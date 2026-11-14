using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PetsFran.API.Data;

// Permite criar migrações sem iniciar o host, que tenta conectar ao banco na inicialização.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=petsfran;Username=petsfran;Password=petsfran_local";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection).Options;
        return new AppDbContext(options);
    }
}
