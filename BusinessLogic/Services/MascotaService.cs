namespace PortalAdopcion.BusinessLogic.Services;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.BusinessLogic.Mapping;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.Constants;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;
using PortalAdopcion.Shared.Security;

// REFACTOR: Extraído de PortalService. Responsabilidad: Catálogo, altas, fichas y estados de mascotas.
public class MascotaService(IPortalRepository repository, TimeProvider timeProvider) : ServiceBase(repository, timeProvider), IMascotaService
{
    // REFACTOR: Filtro dinámico de catálogo (RF 5.1 / CU-09) compuesto en Expression y evaluado por el motor de BD.
    public async Task<IReadOnlyList<MascotaResponseDTO>> PetsAsync(
        EstadoMascotaEnum? estado = null,
        EspecieEnum? especie = null,
        SexoEnum? sexo = null,
        string? busqueda = null,
        CancellationToken ct = default)
    {
        if (estado.HasValue)
            EnumValid(estado.Value);
        if (especie.HasValue)
            EnumValid(especie.Value);
        if (sexo.HasValue)
            EnumValid(sexo.Value);

        System.Linq.Expressions.Expression<Func<Mascota, bool>> predicate = x => x.Estado != EstadoMascotaEnum.Inactiva;

        if (estado.HasValue)
        {
            var eVal = estado.Value;
            predicate = PortalAdopcion.BusinessLogic.Common.PredicateBuilder.And(predicate, x => x.Estado == eVal);
        }

        if (especie.HasValue)
        {
            var espVal = especie.Value;
            predicate = PortalAdopcion.BusinessLogic.Common.PredicateBuilder.And(predicate, x => x.Especie == espVal);
        }

        if (sexo.HasValue)
        {
            var sVal = sexo.Value;
            predicate = PortalAdopcion.BusinessLogic.Common.PredicateBuilder.And(predicate, x => x.Sexo == sVal);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var term = busqueda.Trim();
            if (term.Length > BusinessRules.MaxLongitudNombre)
                throw new ValidationException($"Búsqueda: hasta {BusinessRules.MaxLongitudNombre} caracteres.");
            // SECURITY: se escapan los comodines LIKE (% _ \) para que el texto del usuario se busque literalmente y no como patrón.
            var pattern = "%" + term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            predicate = PortalAdopcion.BusinessLogic.Common.PredicateBuilder.And(predicate, x =>
                EF.Functions.Like(x.Nombre, pattern, "\\") ||
                (x.Raza != null && EF.Functions.Like(x.Raza, pattern, "\\")));
        }

        var pets = await repo.ListAsync(predicate, ct);
        return pets.OrderBy(x => x.Nombre).Select(x => x.ToDto()).ToList();
    }

    public async Task<MascotaResponseDTO> PetAsync(Guid id) => (await Find<Mascota>(id)).ToDto();

    private static void PetData(Mascota m, MascotaFichaDTO d)
    {
        m.Nombre = Text(d.Nombre, "Nombre", BusinessRules.MaxLongitudNombreMascota);
        EnumValid(d.Especie);
        EnumValid(d.Sexo);
        if (d.Edad < BusinessRules.MinEdadMascota || d.Edad > BusinessRules.MaxEdadMascota)
            throw new ValidationException($"Edad: {BusinessRules.MinEdadMascota} a {BusinessRules.MaxEdadMascota} años.");
        if (d.FotoUrl != null && d.FotoUrl != "" && (!Uri.TryCreate(d.FotoUrl, UriKind.Absolute, out var url) || url.Scheme != "https"))
            throw new ValidationException("Foto: URL HTTPS válida.");
        m.Especie = d.Especie;
        m.Sexo = d.Sexo;
        m.Edad = d.Edad;
        m.Raza = d.Raza?.Trim();
        if (m.Raza?.Length > BusinessRules.MaxLongitudRaza)
            throw new ValidationException("Raza demasiado larga.");
        m.FotoUrl = string.IsNullOrWhiteSpace(d.FotoUrl) ? null : d.FotoUrl.Trim();
        m.Chip = string.IsNullOrWhiteSpace(d.Chip) ? null : Text(d.Chip, "Chip", BusinessRules.MaxLongitudChip);
    }

    public async Task<MascotaResponseDTO> CreatePetAsync(Actor a, MascotaFichaDTO d)
    {
        Require(a, "Administrador", "Voluntario");
        var m = new Mascota
        {
            FechaRegistro = Now
        };
        PetData(m, d);
        await Save(a, "CU-09 Alta", m.Id, "Mascota registrada", new Cambio(m, true));
        return m.ToDto();
    }

    public async Task<MascotaResponseDTO> UpdatePetAsync(Actor a, Guid id, MascotaFichaDTO d)
    {
        Require(a, "Administrador", "Voluntario");
        var m = await Find<Mascota>(id);
        PetData(m, d);
        await Save(a, "CU-09 Edición", id, "Ficha actualizada", new Cambio(m));
        return m.ToDto();
    }

    public async Task<MascotaResponseDTO> ChangePetAsync(Actor a, Guid id, CambioMascotaDTO d)
    {
        Require(a, "Administrador", "Voluntario");
        EnumValid(d.Estado);
        var reason = Text(d.Motivo, "Motivo");
        var m = await Find<Mascota>(id);
        if (m.Estado == EstadoMascotaEnum.Adoptada)
            throw new ConflictException("Una mascota adoptada no cambia de estado administrativamente.");
        if (d.Estado is not (EstadoMascotaEnum.Disponible or EstadoMascotaEnum.EnTratamiento or EstadoMascotaEnum.Inactiva))
            throw new ConflictException("Este estado solo se cambia desde el proceso de adopción.");
        if (await repo.AnyAsync<SolicitudDeAdopcion>(x => x.MascotaId == id && EstadosActivos.Contains(x.Estado)))
            throw new ConflictException("La mascota tiene un proceso activo.");
        m.Estado = d.Estado;
        await Save(a, "CU-09 Estado", id, reason, new Cambio(m));
        return m.ToDto();
    }
}
