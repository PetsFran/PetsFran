using PetsFran.API.Models.Entities;

namespace PetsFran.API.Domain.Ports;

public interface ICachorroRepository : IRepository<Cachorro>
{
    Task<IEnumerable<Cachorro>> GetByPetShopIdAsync(int petShopId);
    Task<Cachorro?> GetByIdAndPetShopIdAsync(int id, int petShopId);
}

public interface IHorarioRepository : IRepository<Horario>
{
    Task<IEnumerable<Horario>> GetByPetShopIdAsync(int petShopId);
    Task<Horario?> GetByIdAndPetShopIdAsync(int id, int petShopId);
    Task<bool> HasFutureSlotsAsync(int cachorroId, int petShopId);
}

public interface IPetShopRepository : IRepository<PetShop>
{
    Task<PetShop?> GetByEmailAsync(string email);
}

