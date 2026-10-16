namespace PortalAdopcion.DataAccess.Repositories;
using System.Linq.Expressions;
using PortalAdopcion.DataAccess.Entities;

public record Cambio(Registro Entidad, bool Nuevo = false);

/// <summary>Resultado paginado genérico (limit/offset) para listados.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public bool HasNext => Page * PageSize < Total;
}

public interface IPortalRepository
{
    /// <summary>Lista completa (sin filtro). Usar solo cuando se necesita todo el conjunto.</summary>
    Task<List<T>> ListAsync<T>(CancellationToken ct = default)
        where T : Registro;

    /// <summary>Lista filtrada en el motor de base de datos (evita traer toda la tabla).</summary>
    Task<List<T>> ListAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro;

    /// <summary>Primer elemento que cumple el predicado, evaluado por la base de datos.</summary>
    Task<T?> FirstAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro;

    /// <summary>¿Existe al menos un elemento que cumple el predicado? Se resuelve con EXISTS.</summary>
    Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro;

    /// <summary>Cuenta elementos que cumplen el predicado, sin materializar entidades.</summary>
    Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro;

    /// <summary>Página de elementos con ordenamiento estable por Id. Aplica LIMIT/OFFSET en SQL.</summary>
    Task<PagedResult<T>> PageAsync<T>(Expression<Func<T, bool>> predicate, int page, int pageSize, CancellationToken ct = default)
        where T : Registro;

    Task<T?> FindAsync<T>(Guid id, CancellationToken ct = default)
        where T : Registro;
    Task SaveAsync(IEnumerable<Cambio> cambios, EventoAuditoria auditoria, CancellationToken ct = default);

    /// <summary>
    /// Borra TODOS los datos de todas las tablas (en orden de dependencias para no violar
    /// claves foráneas). Pensado para reiniciar el entorno demo y poder repetir las pruebas
    /// sin borrar el archivo .db. No borra el esquema ni el historial de migraciones.
    /// </summary>
    Task ResetDemoDataAsync(CancellationToken ct = default);
}
