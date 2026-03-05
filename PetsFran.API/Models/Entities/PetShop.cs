using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PetsFran.API.Models.Entities;

public class PetShop
{
    public int Id { get; set; }

    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;
    [JsonIgnore] public string Senha { get; set; } = string.Empty;
    public string EnderecoPetShop { get; set; } = string.Empty;
    
    //Refresh Token
    [JsonIgnore] public string? RefreshTokenHash { get; set; }
    [JsonIgnore] public DateTime? RefreshTokenExpiryTime { get; set; }

    // Corrigido: inicialização adequada
    public List<Horario> Horarios { get; set; } = new();
    [JsonIgnore] public List<Cachorro> Cachorros { get; set; } = new();
}