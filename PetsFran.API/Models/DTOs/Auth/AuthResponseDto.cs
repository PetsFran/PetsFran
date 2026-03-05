using System;
using PetsFran.API.Models.DTOs.PetShops;

namespace PetsFran.API.Models.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime TokenExpiration { get; set; }
    public DateTime RefreshTokenExpiration { get; set; }
    public PetShopBasicDto PetShop { get; set; } = null!;
}