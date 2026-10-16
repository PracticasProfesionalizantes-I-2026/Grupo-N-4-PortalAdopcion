namespace PortalAdopcion.DataAccess;
using Microsoft.EntityFrameworkCore;
using PortalAdopcion.DataAccess.Entities;

public class PortalAdopcionDbContext(DbContextOptions<PortalAdopcionDbContext> options) : DbContext(options)
{
    public DbSet<Mascota> Mascotas => Set<Mascota>();
    public DbSet<Adoptante> Adoptantes => Set<Adoptante>();
    public DbSet<SolicitudDeAdopcion> Solicitudes => Set<SolicitudDeAdopcion>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Mascota>().ToTable("Mascotas");
        b.Entity<Adoptante>().ToTable("Adoptantes", tb =>
        {
            tb.HasCheckConstraint("CK_Adoptantes_Dni_Numerico", "trim(\"Dni\") <> '' AND \"Dni\" NOT GLOB '*[^0-9]*'");
            tb.HasCheckConstraint("CK_Adoptantes_Telefono_Numerico", "trim(\"Telefono\") <> '' AND \"Telefono\" NOT GLOB '*[^0-9]*'");
            tb.HasCheckConstraint("CK_Adoptantes_EstadoCivil_Valido", "\"EstadoCivil\" IN ('Soltero/a','Casado/a','Divorciado/a','Viudo/a','Conviviente/Unión de hecho','Separado/a')");
        });
        b.Entity<SolicitudDeAdopcion>().ToTable("Solicitudes");
        b.Entity<Usuario>().ToTable("Usuario", tb =>
            tb.HasCheckConstraint("CK_Usuario_Dni_Numerico", "trim(\"Dni\") <> '' AND \"Dni\" NOT GLOB '*[^0-9]*'"));
        b.Entity<SancionBlacklist>();
        b.Entity<HistorialClinico>();
        b.Entity<EventoMedico>();
        b.Entity<Entrevista>();
        b.Entity<Seguimiento>();
        b.Entity<Incidente>();
        b.Entity<EventoAuditoria>();
        b.Entity<Notificacion>();
        foreach (var e in b.Model.GetEntityTypes())
        {
            b.Entity(e.ClrType).HasKey("Id");
            b.Entity(e.ClrType).Property("Version").IsConcurrencyToken();
        }

        b.Entity<Mascota>().Property(x => x.Estado).HasConversion<string>();
        b.Entity<Mascota>().Property(x => x.Especie).HasConversion<string>();
        b.Entity<Mascota>().Property(x => x.Sexo).HasConversion<string>();
        b.Entity<SolicitudDeAdopcion>().Property(x => x.Estado).HasConversion<string>();
        b.Entity<Adoptante>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Adoptante>().HasIndex(x => x.Dni).IsUnique();
        b.Entity<Usuario>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Usuario>().HasIndex(x => x.Dni).IsUnique();
        b.Entity<Usuario>().HasIndex(x => x.Matricula).IsUnique();
        b.Entity<Mascota>().HasIndex(x => x.Chip).IsUnique();
        b.Entity<Usuario>().HasOne<Adoptante>().WithMany().HasForeignKey(x => x.AdoptanteId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SolicitudDeAdopcion>().HasOne<Mascota>().WithMany().HasForeignKey(x => x.MascotaId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SolicitudDeAdopcion>().HasOne<Adoptante>().WithMany().HasForeignKey(x => x.AdoptanteId).OnDelete(DeleteBehavior.Restrict);
        const string active = "Estado IN ('Pendiente','EnObservacion','Aprobada','EnEntrevista','EnPeriodoDePrueba')";
        b.Entity<SolicitudDeAdopcion>().HasIndex(x => x.MascotaId).IsUnique().HasFilter(active);
        b.Entity<SolicitudDeAdopcion>().HasIndex(x => x.AdoptanteId).IsUnique().HasFilter(active);
        b.Entity<SancionBlacklist>().HasOne<Adoptante>().WithMany().HasForeignKey(x => x.AdoptanteId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HistorialClinico>().HasIndex(x => x.MascotaId).IsUnique().HasFilter("MascotaId IS NOT NULL");
        b.Entity<HistorialClinico>().HasOne<Mascota>().WithMany().HasForeignKey(x => x.MascotaId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HistorialClinico>().HasIndex(x => x.AdoptanteId);
        b.Entity<HistorialClinico>().HasIndex(x => x.OrigenMascotaId);
        b.Entity<EventoMedico>().HasOne<HistorialClinico>().WithMany().HasForeignKey(x => x.HistorialClinicoId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Entrevista>().HasIndex(x => x.SolicitudId).IsUnique();
        b.Entity<Entrevista>().HasOne<SolicitudDeAdopcion>().WithMany().HasForeignKey(x => x.SolicitudId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Seguimiento>().HasIndex(x => new { x.SolicitudId, x.Fecha }).IsUnique();
        b.Entity<Seguimiento>().HasOne<SolicitudDeAdopcion>().WithMany().HasForeignKey(x => x.SolicitudId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Incidente>().HasOne<SolicitudDeAdopcion>().WithMany().HasForeignKey(x => x.SolicitudId).OnDelete(DeleteBehavior.Restrict);

        // --- Índices de rendimiento (filtros/ordenamientos frecuentes) ---
        // Catálogo: WHERE Estado != 'Inactiva' AND Estado = @estado ORDER BY Nombre
        b.Entity<Mascota>().HasIndex(x => x.Estado);
        b.Entity<Mascota>().HasIndex(x => x.Nombre);
        // Procesos: filtros por Estado activo, por Voluntario y caducidad por inactividad.
        b.Entity<SolicitudDeAdopcion>().HasIndex(x => x.Estado);
        b.Entity<SolicitudDeAdopcion>().HasIndex(x => x.VoluntarioId);
        b.Entity<SolicitudDeAdopcion>().HasIndex(x => new { x.Estado, x.UltimaActividad });
        // Blacklist: verificación de sanción vigente por adoptante y rango de fechas.
        b.Entity<SancionBlacklist>().HasIndex(x => new { x.AdoptanteId, x.Revocada, x.Desde, x.Hasta });
        // Auditoría: rango de fechas y filtro por usuario ordenado por fecha.
        b.Entity<EventoAuditoria>().HasIndex(x => x.Fecha);
        b.Entity<EventoAuditoria>().HasIndex(x => new { x.UsuarioId, x.Fecha });
        // Notificaciones: bandeja interna por usuario o rol.
        b.Entity<Notificacion>().HasIndex(x => new { x.Canal, x.UsuarioId });
        b.Entity<Notificacion>().HasIndex(x => new { x.Canal, x.Rol });
        // Incidentes: filtro por voluntario ordenado por fecha.
        b.Entity<Incidente>().HasIndex(x => new { x.VoluntarioId, x.Fecha });
    }
}
