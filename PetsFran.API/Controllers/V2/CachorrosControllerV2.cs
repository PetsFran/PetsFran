using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PetsFran.API.Application.UseCases.Cachorros.Implementations;
using PetsFran.API.CrossCutting.Middleware;
using PetsFran.API.Models.DTOs.Cachorros;

namespace PetsFran.API.Controllers.V2;

[ApiController]
[Route("api/v2/[controller]")]
[Authorize]
public class CachorrosControllerV2 : ControllerBase
{
    private readonly IGetCachorresByPetShopUseCase _getCachorresByPetShopUseCase;
    private readonly IGetCachorroByIdUseCase _getCachorroByIdUseCase;
    private readonly ICreateCachorroUseCase _createCachorroUseCase;
    private readonly IUpdateCachorroUseCase _updateCachorroUseCase;
    private readonly IDeleteCachorroUseCase _deleteCachorroUseCase;
    private readonly int _petShopIdLogado;

    public CachorrosControllerV2(
        IGetCachorresByPetShopUseCase getCachorresByPetShopUseCase,
        IGetCachorroByIdUseCase getCachorroByIdUseCase,
        ICreateCachorroUseCase createCachorroUseCase,
        IUpdateCachorroUseCase updateCachorroUseCase,
        IDeleteCachorroUseCase deleteCachorroUseCase,
        IHttpContextAccessor httpContextAccessor)
    {
        _getCachorresByPetShopUseCase = getCachorresByPetShopUseCase;
        _getCachorroByIdUseCase = getCachorroByIdUseCase;
        _createCachorroUseCase = createCachorroUseCase;
        _updateCachorroUseCase = updateCachorroUseCase;
        _deleteCachorroUseCase = deleteCachorroUseCase;
        _petShopIdLogado = ExtrairPetShopIdDoUsuario(httpContextAccessor?.HttpContext?.User);
    }

    private int ExtrairPetShopIdDoUsuario(ClaimsPrincipal? user)
    {
        if (user == null)
            throw new UnauthorizedAccessException("Usuário não autenticado");

        var petShopIdClaim = user.FindFirst("PetShopId")?.Value;
        if (string.IsNullOrEmpty(petShopIdClaim) || !int.TryParse(petShopIdClaim, out int petShopId))
            throw new UnauthorizedAccessException("PetShopId não encontrado no token");

        return petShopId;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _getCachorresByPetShopUseCase.ExecuteAsync(_petShopIdLogado);
        return Ok(ApiResponse.FromResult(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _getCachorroByIdUseCase.ExecuteAsync(id, _petShopIdLogado);

        if (!result.IsSuccess)
            return NotFound(ApiResponse.FromResult(result));

        return Ok(ApiResponse.FromResult(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCachorroDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _createCachorroUseCase.ExecuteAsync(dto, _petShopIdLogado);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.FromResult(result));

        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, ApiResponse.FromResult(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCachorroDto dto)
    {
        var result = await _updateCachorroUseCase.ExecuteAsync(id, dto, _petShopIdLogado);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.FromResult(result));

        return Ok(ApiResponse.FromResult(result));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _deleteCachorroUseCase.ExecuteAsync(id, _petShopIdLogado);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.FromResult(result));

        return NoContent();
    }
}

