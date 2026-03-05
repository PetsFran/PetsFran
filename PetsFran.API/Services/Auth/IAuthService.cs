using System;
using System.Threading.Tasks;
using PetsFran.API.Models.DTOs.Auth;
using PetsFran.API.Models.Entities;

namespace PetsFran.API.Services.Auth;

public interface IAuthService
{
    Task<AuthResponseDto?> AuthenticateAsync(string email, string senha);
    Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
    string GenerateAccessToken(PetShop petShop);
    (string refreshToken, DateTime expiryTime) GenerateRefreshToken();
}