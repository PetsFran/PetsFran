using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PetsFran.API.Application.UseCases.Auth.Commands;
using PetsFran.API.Application.UseCases.Auth.Interfaces;
using PetsFran.API.CrossCutting.Middleware;
using PetsFran.API.Models.DTOs.Auth;

namespace PetsFran.API.Controllers.V2;

/// <summary>
/// AuthController refatorado com Clean Architecture
/// Responsabilidade única: HTTP/routing, delegação para Use Cases
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
public class AuthControllerV2 : ControllerBase
{
    private readonly ILoginUseCase _loginUseCase;
    private readonly IRefreshTokenUseCase _refreshTokenUseCase;
    private readonly IRevokeTokenUseCase _revokeTokenUseCase;
    private readonly ILogoutUseCase _logoutUseCase;

    public AuthControllerV2(
        ILoginUseCase loginUseCase,
        IRefreshTokenUseCase refreshTokenUseCase,
        IRevokeTokenUseCase revokeTokenUseCase,
        ILogoutUseCase logoutUseCase)
    {
        _loginUseCase = loginUseCase;
        _refreshTokenUseCase = refreshTokenUseCase;
        _revokeTokenUseCase = revokeTokenUseCase;
        _logoutUseCase = logoutUseCase;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var command = new LoginCommand { Email = loginDto.Email, Senha = loginDto.Senha };
        var result = await _loginUseCase.ExecuteAsync(command);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.FromResult(result));

        SetRefreshTokenCookie(result.Data!.RefreshToken);
        return Ok(ApiResponse.FromResult(result));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest(new { Message = "Refresh token é obrigatório" });

        var result = await _refreshTokenUseCase.ExecuteAsync(refreshToken);

        if (!result.IsSuccess)
            return Unauthorized(ApiResponse.FromResult(result));

        SetRefreshTokenCookie(result.Data!.RefreshToken);
        return Ok(ApiResponse.FromResult(result));
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenDto dto)
    {
        var result = await _revokeTokenUseCase.ExecuteAsync(dto.RefreshToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.FromResult(result));

        Response.Cookies.Delete("refreshToken");
        return Ok(ApiResponse.FromResult(result));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var petShopId = User.FindFirst("PetShopId")?.Value;
        var refreshToken = Request.Cookies["refreshToken"];

        var result = await _logoutUseCase.ExecuteAsync(petShopId, refreshToken);

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });

        return result.IsSuccess ? Ok(ApiResponse.FromResult(result)) : BadRequest(ApiResponse.FromResult(result));
    }

    [HttpGet("validate")]
    [Authorize]
    public IActionResult Validate()
    {
        var petShopId = User.FindFirst("PetShopId")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new
        {
            success = true,
            message = "Token válido",
            data = new { petShopId, email }
        });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}

