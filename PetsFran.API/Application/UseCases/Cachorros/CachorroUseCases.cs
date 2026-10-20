using AutoMapper;
using PetsFran.API.CrossCutting.Results;
using PetsFran.API.Domain.Ports;
using PetsFran.API.Domain.Services;
using PetsFran.API.Models.DTOs.Cachorros;
using PetsFran.API.Models.Entities;

namespace PetsFran.API.Application.UseCases.Cachorros.Implementations;

public interface IGetCachorresByPetShopUseCase
{
    Task<Result<List<CachorroResponseDto>>> ExecuteAsync(int petShopId);
}

public interface IGetCachorroByIdUseCase
{
    Task<Result<CachorroResponseDto>> ExecuteAsync(int id, int petShopId);
}

public interface ICreateCachorroUseCase
{
    Task<Result<CachorroResponseDto>> ExecuteAsync(CreateCachorroDto dto, int petShopId);
}

public interface IUpdateCachorroUseCase
{
    Task<Result<CachorroResponseDto>> ExecuteAsync(int id, UpdateCachorroDto dto, int petShopId);
}

public interface IDeleteCachorroUseCase
{
    Task<Result> ExecuteAsync(int id, int petShopId);
}

public class GetCachorresByPetShopUseCase : IGetCachorresByPetShopUseCase
{
    private readonly ICachorroRepository _cachorroRepository;
    private readonly IMapper _mapper;

    public GetCachorresByPetShopUseCase(ICachorroRepository cachorroRepository, IMapper mapper)
    {
        _cachorroRepository = cachorroRepository;
        _mapper = mapper;
    }

    public async Task<Result<List<CachorroResponseDto>>> ExecuteAsync(int petShopId)
    {
        var cachorros = await _cachorroRepository.GetByPetShopIdAsync(petShopId);
        var dtos = _mapper.Map<List<CachorroResponseDto>>(cachorros);
        return Result<List<CachorroResponseDto>>.Success(dtos);
    }
}

public class GetCachorroByIdUseCase : IGetCachorroByIdUseCase
{
    private readonly ICachorroRepository _cachorroRepository;
    private readonly IMapper _mapper;

    public GetCachorroByIdUseCase(ICachorroRepository cachorroRepository, IMapper mapper)
    {
        _cachorroRepository = cachorroRepository;
        _mapper = mapper;
    }

    public async Task<Result<CachorroResponseDto>> ExecuteAsync(int id, int petShopId)
    {
        var cachorro = await _cachorroRepository.GetByIdAndPetShopIdAsync(id, petShopId);
        if (cachorro == null)
            return Result<CachorroResponseDto>.Failure("Cachorro não encontrado ou você não tem permissão");

        return Result<CachorroResponseDto>.Success(_mapper.Map<CachorroResponseDto>(cachorro));
    }
}

public class CreateCachorroUseCase : ICreateCachorroUseCase
{
    private readonly ICachorroRepository _cachorroRepository;
    private readonly IPetShopRepository _petShopRepository;
    private readonly IValidacaoCachorroService _validacaoService;
    private readonly IMapper _mapper;

    public CreateCachorroUseCase(
        ICachorroRepository cachorroRepository,
        IPetShopRepository petShopRepository,
        IValidacaoCachorroService validacaoService,
        IMapper mapper)
    {
        _cachorroRepository = cachorroRepository;
        _petShopRepository = petShopRepository;
        _validacaoService = validacaoService;
        _mapper = mapper;
    }

    public async Task<Result<CachorroResponseDto>> ExecuteAsync(CreateCachorroDto dto, int petShopId)
    {
        // Validações
        var errors = new List<string>();

        if (!_validacaoService.RacaValida(dto.Raca))
            errors.Add($"Raça '{dto.Raca}' não é válida");

        if (!_validacaoService.PorteValido(dto.Porte))
            errors.Add($"Porte '{dto.Porte}' não é válido");

        if (!_validacaoService.RacaCompativelComPorte(dto.Raca, dto.Porte))
            errors.Add($"Raça '{dto.Raca}' não está disponível para porte '{dto.Porte}'");

        if (errors.Any())
            return Result<CachorroResponseDto>.Failure(errors);

        // Validar PetShop
        var petShop = await _petShopRepository.GetByIdAsync(petShopId);
        if (petShop == null)
            return Result<CachorroResponseDto>.Failure("PetShop não encontrado");

        // Criar e persistir
        var cachorro = _mapper.Map<Cachorro>(dto);
        cachorro.PetShopId = petShopId;

        var created = await _cachorroRepository.AddAsync(cachorro);
        await _cachorroRepository.SaveChangesAsync();

        return Result<CachorroResponseDto>.Success(
            _mapper.Map<CachorroResponseDto>(created),
            "Cachorro criado com sucesso");
    }
}

public class UpdateCachorroUseCase : IUpdateCachorroUseCase
{
    private readonly ICachorroRepository _cachorroRepository;
    private readonly IValidacaoCachorroService _validacaoService;
    private readonly IMapper _mapper;

    public UpdateCachorroUseCase(
        ICachorroRepository cachorroRepository,
        IValidacaoCachorroService validacaoService,
        IMapper mapper)
    {
        _cachorroRepository = cachorroRepository;
        _validacaoService = validacaoService;
        _mapper = mapper;
    }

    public async Task<Result<CachorroResponseDto>> ExecuteAsync(int id, UpdateCachorroDto dto, int petShopId)
    {
        var cachorro = await _cachorroRepository.GetByIdAndPetShopIdAsync(id, petShopId);
        if (cachorro == null)
            return Result<CachorroResponseDto>.Failure("Cachorro não encontrado ou sem permissão");

        // Validações
        var errors = new List<string>();

        if (dto.Raca != null && !_validacaoService.RacaValida(dto.Raca))
            errors.Add($"Raça '{dto.Raca}' não é válida");

        if (dto.Porte != null && !_validacaoService.PorteValido(dto.Porte))
            errors.Add($"Porte '{dto.Porte}' não é válido");

        if (dto.Raca != null && dto.Porte != null &&
            !_validacaoService.RacaCompativelComPorte(dto.Raca, dto.Porte))
            errors.Add($"Raça '{dto.Raca}' não está disponível para porte '{dto.Porte}'");

        if (errors.Any())
            return Result<CachorroResponseDto>.Failure(errors);

        _mapper.Map(dto, cachorro);
        await _cachorroRepository.UpdateAsync(cachorro);
        await _cachorroRepository.SaveChangesAsync();

        return Result<CachorroResponseDto>.Success(
            _mapper.Map<CachorroResponseDto>(cachorro),
            "Cachorro atualizado com sucesso");
    }
}

public class DeleteCachorroUseCase : IDeleteCachorroUseCase
{
    private readonly ICachorroRepository _cachorroRepository;
    private readonly IHorarioRepository _horarioRepository;

    public DeleteCachorroUseCase(ICachorroRepository cachorroRepository, IHorarioRepository horarioRepository)
    {
        _cachorroRepository = cachorroRepository;
        _horarioRepository = horarioRepository;
    }

    public async Task<Result> ExecuteAsync(int id, int petShopId)
    {
        var cachorro = await _cachorroRepository.GetByIdAndPetShopIdAsync(id, petShopId);
        if (cachorro == null)
            return Result.Failure("Cachorro não encontrado ou sem permissão");

        if (await _horarioRepository.HasFutureSlotsAsync(id, petShopId))
            return Result.Failure("Cachorro tem horários agendados futuros");

        await _cachorroRepository.DeleteAsync(id);
        await _cachorroRepository.SaveChangesAsync();

        return Result.Success("Cachorro deletado com sucesso");
    }
}

