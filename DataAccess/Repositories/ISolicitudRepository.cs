namespace PortalAdopcion.DataAccess.Repositories;

using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.Shared.Enums;

public interface ISolicitudRepository
{
    Task<IEnumerable<SolicitudDeAdopcion>> GetAllAsync(CancellationToken cancellationToken);

    Task<SolicitudDeAdopcion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<SolicitudDeAdopcion?> GetActivaPorMascotaAsync(Guid mascotaId, CancellationToken cancellationToken);

    Task<bool> TieneActivaAsync(Guid adoptanteId, CancellationToken cancellationToken);

    Task<SolicitudDeAdopcion?> GetActivaPorAdoptanteAsync(Guid adoptanteId, CancellationToken cancellationToken);

    Task<SolicitudDeAdopcion> CreateAsync(SolicitudDeAdopcion solicitud, EstadoMascotaEnum nuevoEstadoMascota, CancellationToken cancellationToken);

    Task<SolicitudDeAdopcion?> UpdateEstadoAsync(
        Guid id,
        EstadoSolicitudEnum nuevoEstado,
        string? motivo,
        EstadoMascotaEnum? nuevoEstadoMascota,
        CancellationToken cancellationToken);
}