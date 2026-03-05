using System.ComponentModel.DataAnnotations;

namespace PetsFran.API.Models.DTOs.Auth;

public class RefreshTokenDto
{
    [Required(ErrorMessage = "Refresh Token é obrigatório")]
    public string RefreshToken { get; set; } = string.Empty;
}