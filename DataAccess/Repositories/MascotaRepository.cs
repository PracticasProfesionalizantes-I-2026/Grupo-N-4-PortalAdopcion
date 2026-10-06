namespace PortalAdopcion.DataAccess.Repositories;

using Microsoft.EntityFrameworkCore;
using PortalAdopcion.DataAccess.Entities;

public class MascotaRepository : IMascotaRepository
{
    private readonly PortalAdopcionDbContext _db;

    public MascotaRepository(PortalAdopcionDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Mascota>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _db.Mascotas
            .AsNoTracking()
            .OrderBy(m => m.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<Mascota?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Mascotas
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Mascotas
            .AsNoTracking()
            .AnyAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<Mascota> CreateAsync(Mascota mascota, CancellationToken cancellationToken)
    {
        mascota.Id = Guid.NewGuid();
        _db.Mascotas.Add(mascota);
        await _db.SaveChangesAsync(cancellationToken);
        return mascota;
    }

    public async Task<Mascota?> UpdateAsync(Guid id, Mascota valores, CancellationToken cancellationToken)
    {
        var actual = await _db.Mascotas.FindAsync([id], cancellationToken);
        if (actual is null)
        {
            return null;
        }

        actual.Nombre = valores.Nombre;
        actual.Especie = valores.Especie;
        actual.Raza = valores.Raza;
        actual.Edad = valores.Edad;
        actual.Sexo = valores.Sexo;
        actual.Estado = valores.Estado;

        await _db.SaveChangesAsync(cancellationToken);
        return actual;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var actual = await _db.Mascotas.FindAsync([id], cancellationToken);
        if (actual is null)
        {
            return false;
        }

        _db.Mascotas.Remove(actual);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HasSolicitudesAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Solicitudes
            .AsNoTracking()
            .AnyAsync(s => s.MascotaId == id, cancellationToken);
    }
}