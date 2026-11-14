namespace PetsFran.API.Models.DTOs.Precos;

public record PrecoServicoCatalogo(string Tipo, string Porte, string Raca, string Nome,
    decimal Valor);

public record AlterarPrecoServicoRequest(string Tipo, string Porte, string Raca,
    string Nome, decimal Valor);
