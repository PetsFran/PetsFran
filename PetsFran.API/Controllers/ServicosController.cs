using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PetsFran.API.Data;
using PetsFran.API.Models.Entities;
using PetsFran.API.Services;
using PetsFran.API.Models.DTOs.Precos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace PetsFran.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CalculaPrecosService _calculaPrecosService;

    public ServicosController(AppDbContext context, CalculaPrecosService calculaPrecosService)
    {
        _context = context;
        _calculaPrecosService = calculaPrecosService;
    }
    
    [HttpGet("racas/{porte}")]
    public List<string> GetRacas(string porte) => _calculaPrecosService.GetRacasPorPorte(porte);

    [HttpGet("servicos/{porte}/{raca}")]
    public List<string> GetServicos(string porte, string raca) =>
        _calculaPrecosService.GetServicosPorRacaPorte(raca, porte);

    [HttpGet("adicionais")]
    public List<string> GetAdicionais() => _calculaPrecosService.GetServicosAdicionais();
    
    [Authorize]
    [HttpGet("precos")]
    public ActionResult<List<PrecoServicoCatalogo>> GetPrecos()
    {
        if (!TryPetShopId(out var petShopId)) return Unauthorized();
        return Ok(_calculaPrecosService.GetCatalogo(petShopId));
    }

    [Authorize]
    [HttpPut("precos")]
    public async Task<ActionResult<PrecoServicoCatalogo>> AlterarPreco(AlterarPrecoServicoRequest request)
    {
        if (!TryPetShopId(out var petShopId)) return Unauthorized();
        var item = _calculaPrecosService.GetItemCatalogo(request.Tipo, request.Porte,
            request.Raca, request.Nome);
        if (item == null) return BadRequest("Serviço não encontrado no catálogo");
        if (request.Valor < 0 || request.Valor > 99999999.99m ||
            decimal.Round(request.Valor, 2) != request.Valor)
            return BadRequest("Informe um preço entre 0 e 99999999,99 com até duas casas decimais");

        var preco = await _context.PrecosServicos.FirstOrDefaultAsync(p =>
            p.PetShopId == petShopId && p.Tipo == item.Tipo && p.Porte == item.Porte &&
            p.Raca == item.Raca && p.Nome == item.Nome);
        if (preco == null)
        {
            _context.PrecosServicos.Add(new PrecoServico
            {
                PetShopId = petShopId, Tipo = item.Tipo, Porte = item.Porte,
                Raca = item.Raca, Nome = item.Nome, Valor = request.Valor
            });
        }
        else preco.Valor = request.Valor;

        await _context.SaveChangesAsync();
        return Ok(item with { Valor = request.Valor });
    }

    private bool TryPetShopId(out int petShopId) =>
        int.TryParse(User.FindFirst("PetShopId")?.Value, out petShopId) && petShopId > 0;

    [Authorize]
    [HttpPost("calcular-preco")]
    public ActionResult<decimal> CalcularPreco([FromBody] CalculoPrecoRequest request)
    {
        if (!TryPetShopId(out var petShopId)) return Unauthorized();
        // Criar um objeto Horario temporário para cálculo
        var horarioTemp = new Horario
        {
            Cachorro = new Cachorro
            {
                Raca = request.Raca,
                Porte = request.Porte
            },
            ServicoBaseSelecionado = request.ServicoBase,
            Adicionais = request.Adicionais ?? new List<string>(),
            PetShopId = petShopId
        };
    
        if (!_calculaPrecosService.GetServicosPorRacaPorte(request.Raca, request.Porte)
                .Contains(request.ServicoBase))
            return BadRequest("Serviço base não disponível para a raça e porte informados");

        try
        {
            return Ok(_calculaPrecosService.GetPrecoTotalHorario(horarioTemp));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public class CalculoPrecoRequest
    {
        public string Raca { get; set; } = string.Empty;
        public string Porte { get; set; } = string.Empty;
        public string ServicoBase { get; set; } = string.Empty;
        public List<string>? Adicionais { get; set; }
    }
}
