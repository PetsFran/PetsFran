using PetsFran.API.Application.UseCases.Auth.Commands;
using PetsFran.API.CrossCutting.Results;

namespace PetsFran.API.Application.UseCases.Auth.Interfaces;

public interface ILoginUseCase
{
    Task<Result<LoginResult>> ExecuteAsync(LoginCommand command);
}

public interface IRefreshTokenUseCase
{
    Task<Result<LoginResult>> ExecuteAsync(string refreshToken);
}

public interface IRevokeTokenUseCase
{
    Task<Result> ExecuteAsync(string refreshToken);
}

public interface ILogoutUseCase
{
    Task<Result> ExecuteAsync(string? petShopId, string? refreshToken);
}

