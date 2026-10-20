using PetsFran.API.Models.Entities;

namespace PetsFran.API.Domain.Services;

/// <summary>
/// Domain Service - Lógica de negócio pura (sem dependência de Application/Infrastructure)
/// Validações, cálculos e regras especializadas
/// </summary>
public interface IValidacaoCachorroService
{
    bool RacaValida(string raca);
    bool PorteValido(string porte);
    bool RacaCompativelComPorte(string raca, string porte);
    List<string> GetRacasValidas();
    List<string> GetPortesValidos();
    List<string> GetRacasPorPorte(string porte);
}

public interface ICalculoPrecosService
{
    decimal GetPrecoBasePorCachorro(Cachorro cachorro, List<string> servicosBase);
    decimal GetPrecoAdicionalPorCachorro(Cachorro cachorro, List<string> adicionais);
    decimal GetPrecoTotalHorario(Horario horario);
    List<string> GetServicosPorRacaPorte(string raca, string porte);
    List<string> GetServicosAdicionais();
}

public interface ITokenService
{
    string GenerateAccessToken(PetShop petShop);
    (string refreshToken, DateTime expiryTime) GenerateRefreshToken();
}

