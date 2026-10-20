namespace PetsFran.API.Domain.Ports;

/// <summary>
/// Contrato genérico para Repository Pattern
/// Desacopla a lógica de negócio do ORM
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task SaveChangesAsync();
}

