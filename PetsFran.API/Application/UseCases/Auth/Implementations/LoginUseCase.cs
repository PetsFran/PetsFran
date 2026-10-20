using AutoMapper;
using Microsoft.Extensions.Configuration;
using PetsFran.API.Application.UseCases.Auth.Commands;
using PetsFran.API.Application.UseCases.Auth.Interfaces;
using PetsFran.API.CrossCutting.Results;
using PetsFran.API.Domain.Ports;
using PetsFran.API.Domain.Services;
using PetsFran.API.Models.DTOs.PetShops;
using PetsFran.API.Services.Auth;

namespace PetsFran.API.Application.UseCases.Auth.Implementations;

/// <summary>
/// Use Case: Login de PetShop
/// Responsabilidade única: orquestrar o fluxo de autenticação
/// </summary>
public class LoginUseCase : ILoginUseCase
{
    private readonly IPetShopRepository _petShopRepository;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public LoginUseCase(
        IPetShopRepository petShopRepository,
        ITokenService tokenService,
        IMapper mapper)
    {
        _petShopRepository = petShopRepository;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    public async Task<Result<LoginResult>> ExecuteAsync(LoginCommand command)
    {
        // 1. Validar entrada
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Senha))
            return Result<LoginResult>.Failure("Email e senha são obrigatórios");

        // 2. Buscar PetShop
        var petShop = await _petShopRepository.GetByEmailAsync(command.Email);
        if (petShop == null)
            return Result<LoginResult>.Failure("Email ou senha incorretos");

        // 3. Validar senha
        if (!PasswordHashVerifier.Verify(command.Senha, petShop.Senha))
            return Result<LoginResult>.Failure("Email ou senha incorretos");

        // 4. Gerar tokens
        var accessToken = _tokenService.GenerateAccessToken(petShop);
        var (refreshToken, refreshTokenExpiry) = _tokenService.GenerateRefreshToken();

        // 5. Persistir refresh token
        petShop.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
        petShop.RefreshTokenExpiryTime = refreshTokenExpiry;
        await _petShopRepository.UpdateAsync(petShop);
        await _petShopRepository.SaveChangesAsync();

        // 6. Retornar resultado
        return Result<LoginResult>.Success(new LoginResult
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            TokenExpiration = DateTime.UtcNow.AddMinutes(15), // Should come from config
            RefreshTokenExpiration = refreshTokenExpiry,
            PetShop = _mapper.Map<PetShopBasicDto>(petShop)
        });
    }
}

