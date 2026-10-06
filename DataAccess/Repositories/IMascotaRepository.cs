namespace PortalAdopcion.DataAccess.Repositories;

using PortalAdopcion.DataAccess.Entities;

public interface IMascotaRepository
{
    Task<IEnumerable<Mascota>> GetAllAsync(CancellationToken cancellationToken);

    Task<Mascota?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<Mascota> CreateAsync(Mascota mascota, CancellationToken cancellationToken);

    Task<Mascota?> UpdateAsync(Guid id, Mascota valores, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasSolicitudesAsync(Guid id, CancellationToken cancellationToken);
}