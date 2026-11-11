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

// REFACTOR: Extraído de PortalService. Responsabilidad: Blacklist, auditoría, notificaciones, incidentes y reportes.
public class ControlOperativoService(IPortalRepository repository, TimeProvider timeProvider) : ServiceBase(repository, timeProvider), IControlOperativoService
{
    public async Task<List<SancionResponseDTO>> BlacklistAsync(Actor a)
    {
        Require(a, "Administrador");
        return (await repo.ListAsync<SancionBlacklist>()).Select(x => x.ToDto()).ToList();
    }

    public async Task<SancionResponseDTO> SaveBlacklistAsync(Actor a, Guid? id, BlacklistDTO d)
    {
        Require(a, "Administrador");
        await Find<Adoptante>(d.AdoptanteId);
        if (d.Desde >= d.Hasta)
            throw new ValidationException("La fecha final debe superar la inicial.");
        var s = id.HasValue ? await Find<SancionBlacklist>(id.Value) : new SancionBlacklist();
        if (id.HasValue && s.AdoptanteId != d.AdoptanteId)
            throw new ConflictException("No se cambia la persona de una sanción.");
        s.AdoptanteId = d.AdoptanteId;
        s.Motivo = Text(d.Motivo, "Motivo");
        s.Desde = d.Desde.ToUniversalTime();
        s.Hasta = d.Hasta.ToUniversalTime();
        s.Revocada = false;
        var changes = new List<Cambio>
        {
            new(s, !id.HasValue)
        };
        if (s.Desde <= Now && s.Hasta > Now)
        {
            var activas = await repo.ListAsync<SolicitudDeAdopcion>(x => x.AdoptanteId == s.AdoptanteId && EstadosActivos.Contains(x.Estado));
            var mascotaIds = activas.Select(r => r.MascotaId).ToList();
            var pets = (await repo.ListAsync<Mascota>(m => mascotaIds.Contains(m.Id))).ToDictionary(m => m.Id);
            var released = new Dictionary<Guid, Usuario>();
            foreach (var request in activas)
            {
                request.Estado = EstadoSolicitudEnum.Rechazada;
                request.Motivo = "Sanción vigente: " + s.Motivo;
                request.UltimaActividad = Now;
                var pet = pets[request.MascotaId];
                pet.Estado = EstadoMascotaEnum.Disponible;
                changes.Add(new Cambio(request));
                changes.Add(new Cambio(pet));
                // SECURITY: RN-16 - un proceso rechazado que ya tenía voluntario (EnEntrevista/EnPeriodoDePrueba) debe liberar su cupo;
                // sin esto ProcesosActivos quedaba inflado y el voluntario se bloqueaba de forma permanente.
                if (request.VoluntarioId is Guid volunteerId)
                {
                    if (!released.TryGetValue(volunteerId, out var assigned))
                        released[volunteerId] = assigned = await Find<Usuario>(volunteerId);
                    if (assigned.ProcesosActivos > 0)
                        assigned.ProcesosActivos--;
                }
            }

            changes.AddRange(released.Values.Select(v => new Cambio(v)));
        }

        await Save(a, "CU-01 Sanción", s.Id, s.Motivo, changes.ToArray());
        return s.ToDto();
    }

    public async Task RevokeBlacklistAsync(Actor a, Guid id, string reason)
    {
        Require(a, "Administrador");
        var s = await Find<SancionBlacklist>(id);
        s.Revocada = true;
        await Save(a, "CU-01 Revocación", id, Text(reason, "Motivo"), new Cambio(s));
    }

    public async Task<IncidenteResponseDTO> IncidentAsync(Actor a, IncidenteDTO d)
    {
        var s = await Find<SolicitudDeAdopcion>(d.SolicitudId);
        Assigned(a, s);
        if (s.Estado != EstadoSolicitudEnum.EnPeriodoDePrueba)
            throw new ConflictException("El incidente requiere un proceso en prueba.");
        if (!Enum.IsDefined(d.Gravedad))
            throw new ValidationException("Gravedad inválida.");
        var i = new Incidente
        {
            SolicitudId = s.Id,
            VoluntarioId = a.Id,
            Fecha = Now,
            Gravedad = d.Gravedad,
            Detalle = Text(d.Detalle, "Detalle")
        };
        s.UltimaActividad = Now;
        await Save(a, "CU-18 Incidente", i.Id, i.Detalle, new Cambio(i, true), new Cambio(s), Notice(i.Id, d.Gravedad + ": " + i.Detalle, "Administrador"));
        return i.ToDto();
    }

    public async Task<List<IncidenteResponseDTO>> IncidentsAsync(Actor a)
    {
        Require(a, "Administrador", "Voluntario");
        var incidents = a.Rol == "Administrador"
            ? await repo.ListAsync<Incidente>()
            : await repo.ListAsync<Incidente>(x => x.VoluntarioId == a.Id);
        return incidents.OrderByDescending(x => x.Gravedad == GravedadIncidenteEnum.Urgente).ThenByDescending(x => x.Fecha).Select(x => x.ToDto()).ToList();
    }

    public async Task<List<AuditoriaResponseDTO>> AuditAsync(Actor a, DateTime? desde, DateTime? hasta, string? accion, Guid? usuario)
    {
        Require(a, "Administrador");
        if (desde > hasta)
            throw new ValidationException("Rango inválido.");
        var eventos = await repo.ListAsync<EventoAuditoria>(x =>
            (desde == null || x.Fecha >= desde) &&
            (hasta == null || x.Fecha <= hasta) &&
            (usuario == null || x.UsuarioId == usuario));
        return eventos
            .Where(x => string.IsNullOrWhiteSpace(accion) || x.Accion.Contains(accion, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Fecha).Select(x => x.ToDto()).ToList();
    }

    public async Task<List<NotificacionResponseDTO>> NotificationsAsync(Actor a) => (await repo.ListAsync<Notificacion>(x => x.Canal == CanalNotificacionEnum.Interna && (x.UsuarioId == a.Id || x.Rol == a.Rol))).OrderByDescending(x => x.Fecha).Select(x => x.ToDto()).ToList();

    public async Task<List<string[]>> ReportAsync(Actor a, string tipo, DateTime desde, DateTime hasta)
    {
        Require(a, "Administrador");
        if (desde > hasta)
            throw new ValidationException("Rango de fechas inválido.");
        var rows = new List<string[]>();
        if (tipo is "solicitudes" or "adopciones")
        {
            rows.Add(["ID", "Estado", "Mascota", "Adoptante", "Fecha"]);
            if (tipo == "adopciones")
            {
                foreach (var s in await repo.ListAsync<SolicitudDeAdopcion>(x => x.Estado == EstadoSolicitudEnum.Adoptada && x.FechaFinalizacion >= desde && x.FechaFinalizacion <= hasta))
                    rows.Add([s.Id.ToString(), s.Estado.ToString(), s.MascotaId.ToString(), s.AdoptanteId.ToString(), s.FechaFinalizacion!.Value.ToString("O")]);
            }
            else
            {
                foreach (var s in await repo.ListAsync<SolicitudDeAdopcion>(x => x.FechaSolicitud >= desde && x.FechaSolicitud <= hasta))
                    rows.Add([s.Id.ToString(), s.Estado.ToString(), s.MascotaId.ToString(), s.AdoptanteId.ToString(), s.FechaSolicitud.ToString("O")]);
            }
        }
        else if (tipo == "mascotas")
        {
            rows.Add(["ID", "Nombre", "Estado", "Especie", "Fecha"]);
            foreach (var m in await repo.ListAsync<Mascota>(x => x.FechaRegistro >= desde && x.FechaRegistro <= hasta))
                rows.Add([m.Id.ToString(), m.Nombre, m.Estado.ToString(), m.Especie.ToString(), m.FechaRegistro.ToString("O")]);
        }
        else
            throw new ValidationException("Tipo: solicitudes, adopciones o mascotas.");
        await Save(a, "CU-16 Reporte", Guid.Empty, tipo);
        return rows;
    }
}
