using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetsFran.API.Data;
using PetsFran.API.Models.DTOs.PetShops;
using PetsFran.API.Models.Entities;
using PetsFran.API.Services.Auth;

namespace PetsFran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PetShopsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly int _petShopIdLogado;

    public PetShopsController(AppDbContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;

        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var claim = user.FindFirst("PetShopId")?.Value;
            if (int.TryParse(claim, out int id))
            {
                _petShopIdLogado = id;
            }
        }
    }


    // GET: api/petshops/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<PetShopResponseDto>> GetById(int id)
    {
        if (id != _petShopIdLogado)
        {
            return Forbid("Você não tem permissão para visualizar dados de outro PetShop.");
        }

        var petShop = await _context.PetShops
            .AsNoTracking() //apenas lê, não rastreia mudanças
            .Include(p => p.Horarios) //carrega o relacionamento entre petshop e horários do petshop
            .ThenInclude(h => h.Cachorro) //carrega o relacionamento entre cachorros e horários do petshop
            .FirstOrDefaultAsync(p => p.Id == _petShopIdLogado);

        if (petShop == null) return NotFound();

        return Ok(_mapper.Map<PetShopResponseDto>(petShop));
    }

    /* Para esse cliente não precisa de POST, já que é um petshop único.
    // POST: api/petshops/
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<PetShopResponseDto>> Create(CreatePetShopDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await _context.PetShops.AnyAsync(p => p.Email == dto.Email))
            return BadRequest("Email já cadastrado");

        var petShop = _mapper.Map<PetShop>(dto);//Objeto mapeado pelo Automapper
        petShop.Senha = BCrypt.Net.BCrypt.HashPassword(dto.Senha);

        _context.PetShops.Add(petShop);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = petShop.Id },
            _mapper.Map<PetShopResponseDto>(petShop));
    }
    */

    // PUT: api/petshops/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<PetShopResponseDto>> Update(int id, UpdatePetShopDto dto)
    {
        // Verificar token
        var petShopIdClaim = User.FindFirst("PetShopId")?.Value;
        if (petShopIdClaim == null || !int.TryParse(petShopIdClaim, out int currentPetShopId))
            return Unauthorized(new { Message = "Token inválido" });

        // Verificar se está editando seu próprio perfil
        if (id != currentPetShopId)
            return Forbid("Você não pode editar outro petshop");

        var petShop = await _context.PetShops.FindAsync(id);
        if (petShop == null) return NotFound();

        // VALIDAÇÃO DA SENHA ATUAL (SEMPRE obrigatória!)
        bool senhaAtualValida = PasswordHashVerifier.Verify(dto.SenhaAtual, petShop.Senha);
        if (!senhaAtualValida)
            return BadRequest(new { Message = "Senha atual incorreta" });

        // Se está trocando a senha...
        if (!string.IsNullOrEmpty(dto.NovaSenha))
        {
            // Validar confirmação
            if (string.IsNullOrEmpty(dto.ConfirmarNovaSenha))
                return BadRequest(new { Message = "Confirmação de senha é obrigatória" });

            if (dto.NovaSenha != dto.ConfirmarNovaSenha)
                return BadRequest(new { Message = "As senhas não conferem" });

            // Hash da nova senha
            petShop.Senha = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
        }

        // Atualizar endereço (se fornecido)
        if (!string.IsNullOrEmpty(dto.EnderecoPetShop))
            petShop.EnderecoPetShop = dto.EnderecoPetShop;

        await _context.SaveChangesAsync();
        return Ok(_mapper.Map<PetShopResponseDto>(petShop));
    }

    // DELETE: api/petshops/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var petShop = await _context.PetShops.FindAsync(id);
        if (petShop == null) return NotFound();

        _context.PetShops.Remove(petShop);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // GET: api/petshops/{id}/dashboard
    [HttpGet("{id}/dashboard")]
    public async Task<ActionResult<object>> GetDashboard(int id)
    {
        var petShop = await _context.PetShops
            .Include(p => p.Horarios)
            .ThenInclude(h => h.Cachorro)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (petShop == null) return NotFound();

        var resultado = new
        {
            PetShop = new
            {
                petShop.Id,
                petShop.Email,
                petShop.EnderecoPetShop
            },
            Estatisticas = new
            {
                TotalHorarios = petShop.Horarios.Count,
                HorariosHoje = petShop.Horarios
                    .Count(h => h.Data.Date == DateTime.Today),
                FaturamentoTotal = petShop.Horarios.Sum(h => h.ValorTotal),
                FaturamentoHoje = petShop.Horarios
                    .Where(h => h.Data.Date == DateTime.Today)
                    .Sum(h => h.ValorTotal)
            },
            ProximosHorarios = petShop.Horarios
                .Where(h => h.Data >= DateTime.Now)
                .OrderBy(h => h.Data)
                .Take(10)
                .Select(h => new
                {
                    h.Id,
                    h.Data,
                    h.ValorTotal,
                    h.ServicoBaseSelecionado,
                    CachorroNome = h.Cachorro != null ? h.Cachorro.NomeCachorro : "Desconhecido"
                })
                .ToList()
        };

        return Ok(resultado);
    }
}
