using Microsoft.EntityFrameworkCore;
using PetsFran.API.Models.Entities;

namespace PetsFran.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<PetShop> PetShops { get; set; }
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<Cachorro> Cachorros { get; set; }
        public DbSet<PrecoServico> PrecosServicos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PrecoServico>(entity =>
            {
                entity.Property(p => p.Valor).HasPrecision(10, 2);
                entity.HasIndex(p => new { p.PetShopId, p.Tipo, p.Porte, p.Raca, p.Nome }).IsUnique();
                entity.HasOne(p => p.PetShop).WithMany()
                    .HasForeignKey(p => p.PetShopId).OnDelete(DeleteBehavior.Cascade);
            });
            // Configuração do Horario
            modelBuilder.Entity<Horario>(entity =>
            {
                // Relacionamento com Cachorro (1:1)
                entity.HasOne(h => h.Cachorro)
                    .WithMany() // Um cachorro pode ter vários horários
                    .HasForeignKey(h => h.CachorroId)
                    .OnDelete(DeleteBehavior.Restrict);
            
                // Relacionamento com PetShop
                entity.HasOne(h => h.PetShop)
                    .WithMany(p => p.Horarios)
                    .HasForeignKey(h => h.PetShopId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuração do Cachorro
            modelBuilder.Entity<Cachorro>(entity =>
            {
                // Relacionamento com PetShop
                entity.HasOne(c => c.PetShop)
                    .WithMany(p => p.Cachorros)
                    .HasForeignKey(c => c.PetShopId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

    }
}
