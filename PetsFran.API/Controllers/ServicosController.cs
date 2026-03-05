using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PetsFran.API.Data;
using PetsFran.API.Models.Entities;
using PetsFran.API.Services;

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
    
    [HttpPost("calcular-preco")]
    public ActionResult<decimal> CalcularPreco([FromBody] CalculoPrecoRequest request)
    {
        // Criar um objeto Horario temporário para cálculo
        var horarioTemp = new Horario
        {
            Cachorro = new Cachorro
            {
                Raca = request.Raca,
                Porte = request.Porte
            },
            ServicoBaseSelecionado = request.ServicoBase,
            Adicionais = request.Adicionais ?? new List<string>()
        };
    
        var preco = _calculaPrecosService.GetPrecoTotalHorario(horarioTemp);
        return Ok(preco);
    }

    public class CalculoPrecoRequest
    {
        public string Raca { get; set; } = string.Empty;
        public string Porte { get; set; } = string.Empty;
        public string ServicoBase { get; set; } = string.Empty;
        public List<string>? Adicionais { get; set; }
    }
}