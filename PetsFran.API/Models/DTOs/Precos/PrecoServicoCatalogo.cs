namespace PetsFran.API.Models.DTOs.Precos;

public record PrecoServicoCatalogo(string Tipo, string Porte, string Raca, string Nome,
    decimal ValorPadrao, decimal Valor, bool Personalizado);

public record AlterarPrecoServicoRequest(string Tipo, string Porte, string Raca,
    string Nome, decimal Valor);

public record IdentificarPrecoServicoRequest(string Tipo, string Porte, string Raca, string Nome);
