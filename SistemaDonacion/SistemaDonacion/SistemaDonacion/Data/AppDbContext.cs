using Microsoft.EntityFrameworkCore;
using SistemaDonacion.Models;

namespace SistemaDonacion.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationUser> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(b =>
            {
                b.ToTable("Usuarios");
                b.HasKey(u => u.Id);

                b.Property(u => u.Nombre)
                    .IsRequired()
                    .HasMaxLength(256);

                b.Property(u => u.Contrasenia)
                    .IsRequired();

                b.Property(u => u.Estado)
                    .HasDefaultValue(true);

                b.Property(u => u.Rol)
                    .HasMaxLength(50)
                    .HasDefaultValue("Medico");

                // Índice único en Nombre
                b.HasIndex(u => u.Nombre).IsUnique();
            });
        }
    }
}
