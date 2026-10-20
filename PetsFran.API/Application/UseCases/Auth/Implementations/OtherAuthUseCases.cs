using AutoMapper;
using PetsFran.API.Application.UseCases.Auth.Commands;
using PetsFran.API.Application.UseCases.Auth.Interfaces;
using PetsFran.API.CrossCutting.Results;
using PetsFran.API.Domain.Ports;
using PetsFran.API.Domain.Services;
using PetsFran.API.Models.DTOs.PetShops;
using PetsFran.API.Services.Auth;

namespace PetsFran.API.Application.UseCases.Auth.Implementations;

public class RefreshTokenUseCase : IRefreshTokenUseCase
{
    private readonly IPetShopRepository _petShopRepository;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public RefreshTokenUseCase(
        IPetShopRepository petShopRepository,
        ITokenService tokenService,
        IMapper mapper)
    {
        _petShopRepository = petShopRepository;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    public async Task<Result<LoginResult>> ExecuteAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<LoginResult>.Failure("Refresh token é obrigatório");

        var petShops = await _petShopRepository.GetAllAsync();
        var petShop = petShops.FirstOrDefault(p =>
            p.RefreshTokenHash != null &&
            p.RefreshTokenExpiryTime > DateTime.UtcNow &&
            PasswordHashVerifier.Verify(refreshToken, p.RefreshTokenHash)
        );

        if (petShop == null)
            return Result<LoginResult>.Failure("Refresh token inválido ou expirado");

        var newAccessToken = _tokenService.GenerateAccessToken(petShop);
        var (newRefreshToken, newRefreshTokenExpiry) = _tokenService.GenerateRefreshToken();

        petShop.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
        petShop.RefreshTokenExpiryTime = newRefreshTokenExpiry;

        await _petShopRepository.UpdateAsync(petShop);
        await _petShopRepository.SaveChangesAsync();

        return Result<LoginResult>.Success(new LoginResult
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            TokenExpiration = DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiration = newRefreshTokenExpiry,
            PetShop = _mapper.Map<PetShopBasicDto>(petShop)
        });
    }
}

public class RevokeTokenUseCase : IRevokeTokenUseCase
{
    private readonly IPetShopRepository _petShopRepository;

    public RevokeTokenUseCase(IPetShopRepository petShopRepository)
    {
        _petShopRepository = petShopRepository;
    }

    public async Task<Result> ExecuteAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Failure("Refresh token é obrigatório");

        var petShops = await _petShopRepository.GetAllAsync();
        var petShop = petShops.FirstOrDefault(p =>
            p.RefreshTokenHash != null &&
            PasswordHashVerifier.Verify(refreshToken, p.RefreshTokenHash)
        );

        if (petShop == null)
            return Result.Failure("Refresh token inválido");

        petShop.RefreshTokenHash = null;
        petShop.RefreshTokenExpiryTime = null;

        await _petShopRepository.UpdateAsync(petShop);
        await _petShopRepository.SaveChangesAsync();

        return Result.Success("Token revogado com sucesso");
    }
}

public class LogoutUseCase : ILogoutUseCase
{
    private readonly IPetShopRepository _petShopRepository;

    public LogoutUseCase(IPetShopRepository petShopRepository)
    {
        _petShopRepository = petShopRepository;
    }

    public async Task<Result> ExecuteAsync(string? petShopId, string? refreshToken)
    {
        if (!string.IsNullOrEmpty(petShopId) && int.TryParse(petShopId, out int id))
        {
            var petShop = await _petShopRepository.GetByIdAsync(id);
            if (petShop != null)
            {
                petShop.RefreshTokenHash = null;
                petShop.RefreshTokenExpiryTime = null;
                await _petShopRepository.UpdateAsync(petShop);
                await _petShopRepository.SaveChangesAsync();
            }
        }
        else if (!string.IsNullOrEmpty(refreshToken))
        {
            var petShops = await _petShopRepository.GetAllAsync();
            var petShop = petShops.FirstOrDefault(p =>
                p.RefreshTokenHash != null &&
                PasswordHashVerifier.Verify(refreshToken, p.RefreshTokenHash)
            );

            if (petShop != null)
            {
                petShop.RefreshTokenHash = null;
                petShop.RefreshTokenExpiryTime = null;
                await _petShopRepository.UpdateAsync(petShop);
                await _petShopRepository.SaveChangesAsync();
            }
        }

        return Result.Success("Logout realizado com sucesso");
    }
}

