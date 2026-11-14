using System.ComponentModel.DataAnnotations;

namespace PetsFran.API.Models.Entities;

// Guarda apenas preços alterados; o catálogo original continua sendo o padrão.
public class PrecoServico
{
    public int Id { get; set; }
    public int PetShopId { get; set; }
    [MaxLength(16)] public string Tipo { get; set; } = string.Empty;
    [MaxLength(40)] public string Porte { get; set; } = string.Empty;
    [MaxLength(120)] public string Raca { get; set; } = string.Empty;
    [MaxLength(120)] public string Nome { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public PetShop PetShop { get; set; } = null!;
}
