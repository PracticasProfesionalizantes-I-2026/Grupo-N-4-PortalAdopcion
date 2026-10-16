namespace PortalAdopcion.DataAccess.Repositories;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.Shared.Exceptions;

public class PortalRepository(PortalAdopcionDbContext db) : IPortalRepository
{
    public Task<List<T>> ListAsync<T>(CancellationToken ct = default)
        where T : Registro => db.Set<T>().AsNoTracking().ToListAsync(ct);

    public Task<List<T>> ListAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro => db.Set<T>().AsNoTracking().Where(predicate).ToListAsync(ct);

    public Task<T?> FirstAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro => db.Set<T>().AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro => db.Set<T>().AsNoTracking().AnyAsync(predicate, ct);

    public Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro => db.Set<T>().AsNoTracking().CountAsync(predicate, ct);

    public async Task<PagedResult<T>> PageAsync<T>(Expression<Func<T, bool>> predicate, int page, int pageSize, CancellationToken ct = default)
        where T : Registro
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        var query = db.Set<T>().AsNoTracking().Where(predicate);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T>(items, total, page, pageSize);
    }

    public Task<T?> FindAsync<T>(Guid id, CancellationToken ct = default)
        where T : Registro => db.Set<T>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task SaveAsync(IEnumerable<Cambio> cambios, EventoAuditoria audit, CancellationToken ct = default)
    {
        try
        {
            foreach (var change in cambios)
            {
                if (change.Nuevo)
                    db.Add(change.Entidad);
                else
                {
                    var v = change.Entidad.Version;
                    db.Update(change.Entidad);
                    db.Entry(change.Entidad).Property(x => x.Version).OriginalValue = v;
                    change.Entidad.Version = v + 1;
                }
            }

            db.Add(audit);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            throw new ConflictException("Los datos cambiaron. Actualizá la consulta y reintentá.");
        }
        catch (DbUpdateException e)when (e.InnerException is SqliteException s && (s.SqliteErrorCode == 19 || s.SqliteErrorCode == 5))
        {
            db.ChangeTracker.Clear();
            throw new ConflictException("Conflicto de unicidad, integridad o reserva. Actualizá los datos.");
        }
    }

    // Orden hijo -> padre para respetar las FK (DeleteBehavior.Restrict) sin cascade.
    public async Task ResetDemoDataAsync(CancellationToken ct = default)
    {
        db.ChangeTracker.Clear();
        await db.Set<EventoMedico>().ExecuteDeleteAsync(ct);
        await db.Set<Seguimiento>().ExecuteDeleteAsync(ct);
        await db.Set<Incidente>().ExecuteDeleteAsync(ct);
        await db.Set<Entrevista>().ExecuteDeleteAsync(ct);
        await db.Set<HistorialClinico>().ExecuteDeleteAsync(ct);
        await db.Set<SolicitudDeAdopcion>().ExecuteDeleteAsync(ct);
        await db.Set<SancionBlacklist>().ExecuteDeleteAsync(ct);
        await db.Set<Notificacion>().ExecuteDeleteAsync(ct);
        await db.Set<EventoAuditoria>().ExecuteDeleteAsync(ct);
        await db.Set<Usuario>().ExecuteDeleteAsync(ct);
        await db.Set<Adoptante>().ExecuteDeleteAsync(ct);
        await db.Set<Mascota>().ExecuteDeleteAsync(ct);
    }
}
