namespace PortalAdopcion.DataAccess.Repositories;

using Microsoft.EntityFrameworkCore;
using PortalAdopcion.DataAccess.Entities;

public class AdoptanteRepository : IAdoptanteRepository
{
    private readonly PortalAdopcionDbContext _db;

    public AdoptanteRepository(PortalAdopcionDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Adoptante>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _db.Adoptantes
            .AsNoTracking()
            .OrderBy(a => a.NombreCompleto)
            .ToListAsync(cancellationToken);
    }

    public async Task<Adoptante?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Adoptantes
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Adoptantes
            .AsNoTracking()
            .AnyAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Adoptante> CreateAsync(Adoptante adoptante, CancellationToken cancellationToken)
    {
        adoptante.Id = Guid.NewGuid();
        _db.Adoptantes.Add(adoptante);
        await _db.SaveChangesAsync(cancellationToken);
        return adoptante;
    }

    public async Task<Adoptante?> UpdateAsync(Guid id, Adoptante valores, CancellationToken cancellationToken)
    {
        var actual = await _db.Adoptantes.FindAsync([id], cancellationToken);
        if (actual is null)
        {
            return null;
        }

        actual.NombreCompleto = valores.NombreCompleto;
        actual.Email = valores.Email;
        actual.Dni = valores.Dni;
        actual.Telefono = valores.Telefono;
        actual.Direccion = valores.Direccion;

        await _db.SaveChangesAsync(cancellationToken);
        return actual;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var actual = await _db.Adoptantes.FindAsync([id], cancellationToken);
        if (actual is null)
        {
            return false;
        }

        _db.Adoptantes.Remove(actual);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HasSolicitudesAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Solicitudes
            .AsNoTracking()
            .AnyAsync(s => s.AdoptanteId == id, cancellationToken);
    }

    public async Task<bool> EmailExisteAsync(string email, CancellationToken cancellationToken)
    {
        return await _db.Adoptantes
            .AsNoTracking()
            .AnyAsync(a => a.Email == email, cancellationToken);
    }

    public async Task<bool> EmailExisteAsync(string email, Guid excluirId, CancellationToken cancellationToken)
    {
        return await _db.Adoptantes
            .AsNoTracking()
            .AnyAsync(a => a.Email == email && a.Id != excluirId, cancellationToken);
    }

    public async Task<bool> DniExisteAsync(string dni, CancellationToken cancellationToken)
    {
        return await _db.Adoptantes
            .AsNoTracking()
            .AnyAsync(a => a.Dni == dni, cancellationToken);
    }

    public async Task<bool> DniExisteAsync(string dni, Guid excluirId, CancellationToken cancellationToken)
    {
        return await _db.Adoptantes
            .AsNoTracking()
            .AnyAsync(a => a.Dni == dni && a.Id != excluirId, cancellationToken);
    }
}