namespace PortalAdopcion.DataAccess.Repositories;

using Microsoft.EntityFrameworkCore;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.Shared.Enums;

public class SolicitudRepository : ISolicitudRepository
{
    private readonly PortalAdopcionDbContext _db;

    public SolicitudRepository(PortalAdopcionDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SolicitudDeAdopcion>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _db.Solicitudes
            .AsNoTracking()
            .OrderByDescending(s => s.FechaSolicitud)
            .ToListAsync(cancellationToken);
    }

    public async Task<SolicitudDeAdopcion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Solicitudes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Solicitudes
            .AsNoTracking()
            .AnyAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<SolicitudDeAdopcion?> GetActivaPorMascotaAsync(Guid mascotaId, CancellationToken cancellationToken)
    {
        return await _db.Solicitudes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.MascotaId == mascotaId && s.Estado == EstadoSolicitudEnum.Pendiente,
                cancellationToken);
    }

    public async Task<bool> TieneActivaAsync(Guid adoptanteId, CancellationToken cancellationToken)
    {
        return await _db.Solicitudes
            .AsNoTracking()
            .AnyAsync(
                s => s.AdoptanteId == adoptanteId && s.Estado == EstadoSolicitudEnum.Pendiente,
                cancellationToken);
    }

    public async Task<SolicitudDeAdopcion?> GetActivaPorAdoptanteAsync(Guid adoptanteId, CancellationToken cancellationToken)
    {
        return await _db.Solicitudes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.AdoptanteId == adoptanteId && s.Estado == EstadoSolicitudEnum.Pendiente,
                cancellationToken);
    }

    public async Task<SolicitudDeAdopcion> CreateAsync(
        SolicitudDeAdopcion solicitud,
        EstadoMascotaEnum nuevoEstadoMascota,
        CancellationToken cancellationToken)
    {
        solicitud.Id = Guid.NewGuid();
        solicitud.FechaSolicitud = DateTime.UtcNow;
        solicitud.Estado = EstadoSolicitudEnum.Pendiente;

        var mascota = await _db.Mascotas.FindAsync([solicitud.MascotaId], cancellationToken);
        if (mascota is not null)
        {
            mascota.Estado = nuevoEstadoMascota;
        }

        _db.Solicitudes.Add(solicitud);
        await _db.SaveChangesAsync(cancellationToken);
        return solicitud;
    }

    public async Task<SolicitudDeAdopcion?> UpdateEstadoAsync(
        Guid id,
        EstadoSolicitudEnum nuevoEstado,
        string? motivo,
        EstadoMascotaEnum? nuevoEstadoMascota,
        CancellationToken cancellationToken)
    {
        var solicitud = await _db.Solicitudes
            .Include(s => s.Mascota)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (solicitud is null)
        {
            return null;
        }

        solicitud.Estado = nuevoEstado;
        solicitud.Motivo = motivo;

        if (nuevoEstadoMascota.HasValue)
        {
            solicitud.Mascota.Estado = nuevoEstadoMascota.Value;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return solicitud;
    }
}