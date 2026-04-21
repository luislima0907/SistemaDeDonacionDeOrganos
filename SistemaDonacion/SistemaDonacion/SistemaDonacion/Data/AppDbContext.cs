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
        public DbSet<Hospital> Hospitales { get; set; }
        public DbSet<Donante> Donantes { get; set; }
        public DbSet<Organo> Organos { get; set; }
        public DbSet<BitacoraAccion> BitacoraAcciones { get; set; }

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

                b.HasIndex(u => u.Nombre).IsUnique();
            });

            modelBuilder.Entity<Hospital>(b =>
            {
                b.ToTable("Hospitales");
                b.HasKey(h => h.Id);

                b.Property(h => h.Nombre)
                    .IsRequired()
                    .HasMaxLength(256);

                b.Property(h => h.Ciudad)
                    .IsRequired()
                    .HasMaxLength(256);

                b.Property(h => h.Pais)
                    .IsRequired()
                    .HasMaxLength(100);

                b.Property(h => h.Telefono)
                    .HasMaxLength(20);

                b.Property(h => h.Email)
                    .HasMaxLength(256);

                b.Property(h => h.Estado)
                    .HasDefaultValue(true);

                b.Property(h => h.FechaRegistro)
                    .HasDefaultValue(DateTime.Now);

                b.HasMany(h => h.Donantes)
                    .WithOne(d => d.Hospital)
                    .HasForeignKey(d => d.HospitalId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(h => h.Nombre).IsUnique();
                b.HasIndex(h => h.Estado);
            });

            modelBuilder.Entity<Donante>(b =>
            {
                b.ToTable("Donantes");
                b.HasKey(d => d.Id);

                b.Property(d => d.Nombre)
                    .IsRequired()
                    .HasMaxLength(256);

                b.Property(d => d.TipoSanguineo)
                    .IsRequired()
                    .HasMaxLength(10);

                b.Property(d => d.Edad).IsRequired();

                b.Property(d => d.FechaRegistro)
                    .HasDefaultValue(DateTime.Now);

                b.Property(d => d.Estado)
                    .HasMaxLength(50)
                    .HasDefaultValue("Disponible");

                b.Property(d => d.HospitalId).IsRequired();

                b.HasOne(d => d.Hospital)
                    .WithMany(h => h.Donantes)
                    .HasForeignKey(d => d.HospitalId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasMany(d => d.Organos)
                    .WithOne(o => o.Donante)
                    .HasForeignKey(o => o.DonanteId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(d => d.Estado);
                b.HasIndex(d => d.HospitalId);
            });

            modelBuilder.Entity<Organo>(b =>
            {
                b.ToTable("Organos");
                b.HasKey(o => o.Id);

                b.Property(o => o.DonanteId).IsRequired();

                b.Property(o => o.TipoOrgano)
                    .IsRequired()
                    .HasMaxLength(100);

                b.Property(o => o.Estado)
                    .HasMaxLength(50)
                    .HasDefaultValue("Disponible");

                b.Property(o => o.FechaDisponibilidad)
                    .HasDefaultValue(DateTime.Now);

                b.HasIndex(o => o.DonanteId);
                b.HasIndex(o => o.Estado);
                b.HasIndex(o => o.TipoOrgano);
            });

            modelBuilder.Entity<BitacoraAccion>(b =>
            {
                b.ToTable("BitacoraAcciones");
                b.HasKey(ba => ba.Id);

                b.Property(ba => ba.UsuarioId).IsRequired();

                b.Property(ba => ba.Accion)
                    .IsRequired()
                    .HasMaxLength(256);

                b.Property(ba => ba.Tabla)
                    .IsRequired()
                    .HasMaxLength(100);

                b.Property(ba => ba.RegistroId).IsRequired();

                b.Property(ba => ba.FechaAccion)
                    .HasDefaultValue(DateTime.Now);

                b.HasOne(ba => ba.Usuario)
                    .WithMany()
                    .HasForeignKey(ba => ba.UsuarioId)
                    .OnDelete(DeleteBehavior.NoAction);

                b.HasIndex(ba => ba.UsuarioId);
                b.HasIndex(ba => ba.Tabla);
                b.HasIndex(ba => ba.FechaAccion);
            });
        }
    }
}
