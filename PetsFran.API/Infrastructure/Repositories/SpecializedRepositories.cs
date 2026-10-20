using Microsoft.EntityFrameworkCore;
using PetsFran.API.Data;
using PetsFran.API.Domain.Ports;
using PetsFran.API.Models.Entities;

namespace PetsFran.API.Infrastructure.Repositories;

public class CachorroRepository : GenericRepository<Cachorro>, ICachorroRepository
{
    public CachorroRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Cachorro>> GetByPetShopIdAsync(int petShopId)
        => await _dbSet
            .Where(c => c.PetShopId == petShopId)
            .OrderBy(c => c.NomeCachorro)
            .ThenBy(c => c.NomeTutor)
            .ToListAsync();

    public async Task<Cachorro?> GetByIdAndPetShopIdAsync(int id, int petShopId)
        => await _dbSet
            .FirstOrDefaultAsync(c => c.Id == id && c.PetShopId == petShopId);
}

public class HorarioRepository : GenericRepository<Horario>, IHorarioRepository
{
    public HorarioRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Horario>> GetByPetShopIdAsync(int petShopId)
        => await _dbSet
            .Where(h => h.PetShopId == petShopId)
            .ToListAsync();

    public async Task<Horario?> GetByIdAndPetShopIdAsync(int id, int petShopId)
        => await _dbSet
            .FirstOrDefaultAsync(h => h.Id == id && h.PetShopId == petShopId);

    public async Task<bool> HasFutureSlotsAsync(int cachorroId, int petShopId)
        => await _dbSet
            .AnyAsync(h => h.CachorroId == cachorroId && 
                          h.PetShopId == petShopId && 
                          h.Data >= DateTime.UtcNow);
}

public class PetShopRepository : GenericRepository<PetShop>, IPetShopRepository
{
    public PetShopRepository(AppDbContext context) : base(context) { }

    public async Task<PetShop?> GetByEmailAsync(string email)
        => await _dbSet
            .FirstOrDefaultAsync(p => p.Email == email);
}

