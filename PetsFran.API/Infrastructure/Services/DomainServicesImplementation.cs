using PetsFran.API.Models;
using PetsFran.API.Models.Entities;
using PetsFran.API.Services;
using PetsFran.API.Services.Data;

namespace PetsFran.API.Infrastructure.Services;

/// <summary>
/// Domain Service Implementation
/// Contém lógica pura de negócio, sem dependências de framework
/// </summary>
public class ValidacaoCachorroService : Domain.Services.IValidacaoCachorroService
{
    private readonly ValidaRacaService _validaRacaServiceLegacy;

    public ValidacaoCachorroService(ValidaRacaService validaRacaService)
    {
        _validaRacaServiceLegacy = validaRacaService;
    }

    public bool RacaValida(string raca)
        => _validaRacaServiceLegacy.RacaValida(raca);

    public bool PorteValido(string porte)
        => _validaRacaServiceLegacy.PorteValido(porte);

    public bool RacaCompativelComPorte(string raca, string porte)
        => _validaRacaServiceLegacy.RacaCompativelComPorte(raca, porte);

    public List<string> GetRacasValidas()
        => _validaRacaServiceLegacy.GetRacasValidas();

    public List<string> GetPortesValidos()
        => new List<string> { "Pequeno", "Médio", "Grande" };

    public List<string> GetRacasPorPorte(string porte)
        => _validaRacaServiceLegacy.GetRacasPorPorte(porte);
}

public class CalculoPrecosService : Domain.Services.ICalculoPrecosService
{
    private readonly CalculaPrecosService _calculaPrecosServiceLegacy;

    public CalculoPrecosService(CalculaPrecosService calculaPrecosService)
    {
        _calculaPrecosServiceLegacy = calculaPrecosService;
    }

    public decimal GetPrecoBasePorCachorro(Cachorro cachorro, List<string> servicosBase)
        => _calculaPrecosServiceLegacy.GetPrecoBasePorCachorro(cachorro, servicosBase);

    public decimal GetPrecoAdicionalPorCachorro(Cachorro cachorro, List<string> adicionais)
        => _calculaPrecosServiceLegacy.GetPrecoAdicionalPorCachorro(cachorro, adicionais);

    public decimal GetPrecoTotalHorario(Horario horario)
        => _calculaPrecosServiceLegacy.GetPrecoTotalHorario(horario);

    public List<string> GetServicosPorRacaPorte(string raca, string porte)
        => _calculaPrecosServiceLegacy.GetServicosPorRacaPorte(raca, porte);

    public List<string> GetServicosAdicionais()
        => _calculaPrecosServiceLegacy.GetServicosAdicionais();
}

public class TokenService : Domain.Services.ITokenService
{
    private readonly global::PetsFran.API.Services.Auth.AuthService _authServiceLegacy;

    public TokenService(global::PetsFran.API.Services.Auth.AuthService authService)
    {
        _authServiceLegacy = authService;
    }

    public string GenerateAccessToken(PetShop petShop)
        => _authServiceLegacy.GenerateAccessToken(petShop);

    public (string refreshToken, DateTime expiryTime) GenerateRefreshToken()
        => _authServiceLegacy.GenerateRefreshToken();
}

