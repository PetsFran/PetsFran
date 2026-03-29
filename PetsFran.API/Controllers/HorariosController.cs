using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetsFran.API.Data;
using PetsFran.API.Models.DTOs.Horarios;
using PetsFran.API.Models.Entities;
using PetsFran.API.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PetsFran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HorariosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly CalculaPrecosService _calculaPrecosService;
    private readonly int _petShopIdLogado;

    public HorariosController(
        AppDbContext context,
        IMapper mapper,
        CalculaPrecosService calculaPrecosService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _calculaPrecosService = calculaPrecosService;
        _petShopIdLogado = ExtrairPetShopIdDoUsuario(httpContextAccessor?.HttpContext?.User);
    }

    private int ExtrairPetShopIdDoUsuario(ClaimsPrincipal user)
    {
        if (user == null)
            throw new UnauthorizedAccessException("Usuário não autenticado");

        var petShopIdClaim = user.FindFirst("PetShopId")?.Value;
        if (string.IsNullOrEmpty(petShopIdClaim) || !int.TryParse(petShopIdClaim, out int petShopId))
            throw new UnauthorizedAccessException("PetShopId não encontrado no token");

        return petShopId;
    }

    // paginação não faz sentido, é contraproducente    
    // GET: api/horarios
    [HttpGet]
    public async Task<ActionResult<List<HorarioResponseDto>>> GetAll()
    {
        var horarios = await _context.Horarios
            .Where(h => h.PetShopId == _petShopIdLogado)
            .Include(h => h.Cachorro)
            .Include(h => h.PetShop)
            .OrderBy(h => h.Data)
            .ToListAsync();

        return Ok(_mapper.Map<List<HorarioResponseDto>>(horarios));
    }

    // GET: api/horarios/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<HorarioResponseDto>> GetById(int id)
    {
        var horario = await _context.Horarios
            .Where(h => h.PetShopId == _petShopIdLogado && h.Id == id)
            .Include(h => h.Cachorro)
            .Include(h => h.PetShop)
            .FirstOrDefaultAsync();

        if (horario == null)
            return NotFound("Horário não encontrado ou você não tem permissão para acessá-lo");

        return Ok(_mapper.Map<HorarioResponseDto>(horario));
    }

    // POST: api/horarios - SÓ PODE CRIAR NO MEU PETSHOP
    [HttpPost]
    public async Task<ActionResult<HorarioResponseDto>> Create(CreateHorarioDto dto)
    {
        if (dto == null)
            return BadRequest("Dados do horário são obrigatórios");

        try
        {
            // Converte a string para DateTime (horário local - Brasil)
            DateTime dataLocal = DateTimeConverter.ConverterParaDateTime(dto.Data);

            // 🔥 CONVERTE PARA UTC ANTES DE SALVAR!
            DateTime dataUtc = dataLocal.ToUniversalTime();

            var cachorro = await _context.Cachorros
                .FirstOrDefaultAsync(c => c.Id == dto.CachorroId);

            if (cachorro == null)
                return NotFound($"Cachorro com ID {dto.CachorroId} não encontrado");

            // O cachorro pertence ao MEU petshop?
            if (cachorro.PetShopId != _petShopIdLogado)
                return BadRequest("Este cachorro não pertence ao seu petshop");

            // Normaliza os dados do cachorro
            cachorro.Raca = cachorro.Raca?.Trim();
            cachorro.Porte = cachorro.Porte?.Trim();

            // Valida serviços disponíveis
            var servicosPermitidos = _calculaPrecosService.GetServicosPorRacaPorte(
                cachorro.Raca ?? "",
                cachorro.Porte ?? ""
            );

            if (!servicosPermitidos.Any())
            {
                var racasDisponiveis = _calculaPrecosService.GetRacasPorPorte(cachorro.Porte ?? "");
                var mensagem = $"Não foram encontrados serviços para '{cachorro.Raca}' de porte '{cachorro.Porte}'. ";

                if (racasDisponiveis.Any())
                    mensagem += $"Raças disponíveis para porte {cachorro.Porte}: {string.Join(", ", racasDisponiveis)}";
                else
                    mensagem += "Nenhuma raça cadastrada para este porte.";

                return BadRequest(mensagem);
            }

            // Verifica se o serviço solicitado está disponível
            if (!servicosPermitidos.Contains(dto.ServicoBaseSelecionado))
            {
                var servicosFormatados = string.Join(", ", servicosPermitidos);
                return BadRequest(
                    $"O serviço '{dto.ServicoBaseSelecionado}' não está disponível " +
                    $"para '{cachorro.Raca}' de porte '{cachorro.Porte}'. " +
                    $"Serviços disponíveis: {servicosFormatados}"
                );
            }

            var horario = new Horario
            {
                CachorroId = dto.CachorroId,
                PetShopId = _petShopIdLogado,
                Data = dataUtc,
                ServicoBaseSelecionado = dto.ServicoBaseSelecionado,
                Adicionais = dto.Adicionais ?? new List<string>(),
                Cachorro = cachorro
            };

            horario.ValorTotal = _calculaPrecosService.GetPrecoTotalHorario(horario);
            _context.Horarios.Add(horario);
            await _context.SaveChangesAsync();

            var horarioCompleto = await _context.Horarios
                .Where(h => h.PetShopId == _petShopIdLogado && h.Id == horario.Id)
                .Include(h => h.Cachorro)
                .Include(h => h.PetShop)
                .FirstOrDefaultAsync();

            return CreatedAtAction(nameof(GetById),
                new { id = horario.Id },
                _mapper.Map<HorarioResponseDto>(horarioCompleto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erro interno: {ex.Message}");
        }
    }

    // PUT: api/horarios/{id} 
    [HttpPut("{id}")]
    public async Task<ActionResult<HorarioResponseDto>> Update(int id, UpdateHorarioDto dto)
    {
        // Verifica se existe e se á do petshop logado
        var horario = await _context.Horarios
            .Where(h => h.PetShopId == _petShopIdLogado && h.Id == id)
            .Include(h => h.Cachorro)
            .FirstOrDefaultAsync();

        if (horario == null)
            return NotFound("Horário não encontrado ou você não tem permissão para editá-lo");

        // Se foi enviada uma nova data, converte
        if (!string.IsNullOrEmpty(dto.Data))
        {
            try
            {
                var novaData = DateTimeConverter.ConverterParaDateTime(dto.Data);
                // Pode ter 50 cachorros no mesmo horário, se dane kkkkk

                horario.Data = novaData;
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Atualiza outros campos
        if (!string.IsNullOrEmpty(dto.ServicoBaseSelecionado))
        {
            // Valida se o serviço é válido para o cachorro
            var servicosPermitidos = _calculaPrecosService.GetServicosPorRacaPorte(
                horario.Cachorro?.Raca ?? "",
                horario.Cachorro?.Porte ?? ""
            );

            if (!servicosPermitidos.Contains(dto.ServicoBaseSelecionado))
            {
                var servicosFormatados = string.Join(", ", servicosPermitidos);
                return BadRequest(
                    $"O serviço '{dto.ServicoBaseSelecionado}' não está disponível " +
                    $"para este cachorro. Serviços disponíveis: {servicosFormatados}"
                );
            }

            horario.ServicoBaseSelecionado = dto.ServicoBaseSelecionado;
        }

        if (dto.Adicionais != null)
            horario.Adicionais = dto.Adicionais;

        // Recalcula valor se necessário
        horario.ValorTotal = _calculaPrecosService.GetPrecoTotalHorario(horario);

        await _context.SaveChangesAsync();

        var horarioAtualizado = await _context.Horarios
            .Where(h => h.PetShopId == _petShopIdLogado && h.Id == id)
            .Include(h => h.Cachorro)
            .Include(h => h.PetShop)
            .FirstOrDefaultAsync();

        return Ok(_mapper.Map<HorarioResponseDto>(horarioAtualizado));
    }

    // DELETE: api/horarios/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var horario = await _context.Horarios
            .Where(h => h.PetShopId == _petShopIdLogado && h.Id == id)
            .FirstOrDefaultAsync();

        if (horario == null)
            return NotFound("Horário não encontrado ou você não tem permissão para excluí-lo");

        _context.Horarios.Remove(horario);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}