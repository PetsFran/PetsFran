using Microsoft.Extensions.DependencyInjection;
using PetsFran.API.Application.UseCases.Auth.Implementations;
using PetsFran.API.Application.UseCases.Auth.Interfaces;
using PetsFran.API.Application.UseCases.Cachorros.Implementations;
using PetsFran.API.Domain.Ports;
using PetsFran.API.Domain.Services;
using PetsFran.API.Infrastructure.Repositories;
using PetsFran.API.Infrastructure.Services;
using PetsFran.API.Services;
using PetsFran.API.Services.Auth;

namespace PetsFran.API.CrossCutting.DependencyInjection;

/// <summary>
/// Extension para registrar todas as dependências da arquitetura
/// Clean Architecture: Cada camada com suas próprias injeções
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCleanArchitectureServices(this IServiceCollection services)
    {
        services.AddScoped<ValidaRacaService>();
        services.AddScoped<AuthService>();

        // Domain Services (Interfaces)
        services.AddScoped<IValidacaoCachorroService, ValidacaoCachorroService>();
        services.AddScoped<ICalculoPrecosService, CalculoPrecosService>();
        services.AddScoped<ITokenService, TokenService>();

        // Repositories
        services.AddScoped<ICachorroRepository, CachorroRepository>();
        services.AddScoped<IHorarioRepository, HorarioRepository>();
        services.AddScoped<IPetShopRepository, PetShopRepository>();

        // Application Use Cases - Auth
        services.AddScoped<ILoginUseCase, LoginUseCase>();
        services.AddScoped<IRefreshTokenUseCase, RefreshTokenUseCase>();
        services.AddScoped<IRevokeTokenUseCase, RevokeTokenUseCase>();
        services.AddScoped<ILogoutUseCase, LogoutUseCase>();

        // Application Use Cases - Cachorros
        services.AddScoped<IGetCachorresByPetShopUseCase, GetCachorresByPetShopUseCase>();
        services.AddScoped<IGetCachorroByIdUseCase, GetCachorroByIdUseCase>();
        services.AddScoped<ICreateCachorroUseCase, CreateCachorroUseCase>();
        services.AddScoped<IUpdateCachorroUseCase, UpdateCachorroUseCase>();
        services.AddScoped<IDeleteCachorroUseCase, DeleteCachorroUseCase>();

        return services;
    }
}

