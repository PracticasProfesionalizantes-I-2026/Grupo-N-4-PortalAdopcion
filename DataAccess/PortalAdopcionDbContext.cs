namespace PortalAdopcion.DataAccess;

using Microsoft.EntityFrameworkCore;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.Shared.Enums;

public class PortalAdopcionDbContext : DbContext
{
    public PortalAdopcionDbContext(DbContextOptions<PortalAdopcionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Mascota> Mascotas => Set<Mascota>();

    public DbSet<Adoptante> Adoptantes => Set<Adoptante>();

    public DbSet<SolicitudDeAdopcion> Solicitudes => Set<SolicitudDeAdopcion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Mascota>(entity =>
        {
            entity.ToTable("Mascotas");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(m => m.Especie).HasConversion<string>().HasMaxLength(20);
            entity.Property(m => m.Raza).HasMaxLength(100);
            entity.Property(m => m.Sexo).HasConversion<string>().HasMaxLength(20);
            entity.Property(m => m.Estado).HasConversion<string>().HasMaxLength(20);
            entity.Property(m => m.FechaRegistro).IsRequired();
            entity.HasIndex(m => m.Estado);
        });

        modelBuilder.Entity<Adoptante>(entity =>
        {
            entity.ToTable("Adoptantes");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.NombreCompleto).IsRequired().HasMaxLength(150);
            entity.Property(a => a.Email).IsRequired().HasMaxLength(150);
            entity.Property(a => a.Dni).IsRequired().HasMaxLength(20);
            entity.Property(a => a.Telefono).IsRequired().HasMaxLength(30);
            entity.Property(a => a.Direccion).IsRequired().HasMaxLength(200);
            entity.Property(a => a.FechaRegistro).IsRequired();
            entity.HasIndex(a => a.Email).IsUnique();
            entity.HasIndex(a => a.Dni).IsUnique();
        });

        modelBuilder.Entity<SolicitudDeAdopcion>(entity =>
        {
            entity.ToTable("Solicitudes");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.FechaSolicitud).IsRequired();
            entity.Property(s => s.Estado).HasConversion<string>().HasMaxLength(20);
            entity.Property(s => s.Motivo).HasMaxLength(500);

            entity.HasOne(s => s.Mascota)
                .WithMany(m => m.Solicitudes)
                .HasForeignKey(s => s.MascotaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Adoptante)
                .WithMany(a => a.Solicitudes)
                .HasForeignKey(s => s.AdoptanteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => new { s.MascotaId, s.Estado });
            entity.HasIndex(s => new { s.AdoptanteId, s.Estado });
        });
    }
}