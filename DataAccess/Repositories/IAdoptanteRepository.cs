namespace PortalAdopcion.DataAccess.Repositories;

using PortalAdopcion.DataAccess.Entities;

public interface IAdoptanteRepository
{
    Task<IEnumerable<Adoptante>> GetAllAsync(CancellationToken cancellationToken);

    Task<Adoptante?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<Adoptante> CreateAsync(Adoptante adoptante, CancellationToken cancellationToken);

    Task<Adoptante?> UpdateAsync(Guid id, Adoptante valores, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasSolicitudesAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> EmailExisteAsync(string email, CancellationToken cancellationToken);

    Task<bool> EmailExisteAsync(string email, Guid excluirId, CancellationToken cancellationToken);

    Task<bool> DniExisteAsync(string dni, CancellationToken cancellationToken);

    Task<bool> DniExisteAsync(string dni, Guid excluirId, CancellationToken cancellationToken);
}