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

// REFACTOR: Extraído de PortalService. Responsabilidad: Postulaciones, evaluación, entrevistas, seguimiento, cancelación, finalización y abandono.
public class AdopcionWorkflowService(IPortalRepository repository, TimeProvider timeProvider) : ServiceBase(repository, timeProvider), IAdopcionWorkflowService
{
    private static void Own(Actor a, SolicitudDeAdopcion s)
    {
        if (a.Rol == "Adoptante" && a.AdoptanteId != s.AdoptanteId)
            throw new AccessException("La solicitud pertenece a otro adoptante.");
    }

    public async Task<List<SolicitudResponseDTO>> RequestsAsync(Actor a)
    {
        if (a.Rol == "Adoptante")
        {
            if (a.AdoptanteId is null)
                return [];
            return (await repo.ListAsync<SolicitudDeAdopcion>(x => x.AdoptanteId == a.AdoptanteId!.Value))
                .OrderByDescending(x => x.FechaSolicitud).Select(x => x.ToDto()).ToList();
        }

        return (await repo.ListAsync<SolicitudDeAdopcion>())
            .OrderByDescending(x => x.FechaSolicitud).Select(x => x.ToDto()).ToList();
    }

    public async Task<SolicitudResponseDTO> RequestAsync(Actor a, Guid id)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Own(a, s);
        return s.ToDto();
    }

    public async Task<SolicitudResponseDTO> PostulateAsync(Actor a, PostulacionDTO d)
    {
        Require(a, "Adoptante", "Administrador");
        var profile = a.Rol == "Adoptante" ? a.AdoptanteId : d.AdoptanteId;
        if (!profile.HasValue)
            throw new ValidationException("Se requiere adoptante.");
        var p = await Find<Adoptante>(profile.Value);
        var m = await Find<Mascota>(d.MascotaId);
        var user = await repo.FirstAsync<Usuario>(x => x.AdoptanteId == p.Id);
        // ACA: 403 Forbidden confirmado para usuarios en blacklist (ver CU-01/CU-02).
        // Semánticamente adecuado en REST para acceso denegado por sanción vigente; ExceptionHandlingMiddleware mapea AccessException a 403.
        if (await Blocked(p.Id))
            throw new AccessException(ErrorMessages.PostulacionBloqueada);
        if (p.Baja || user is null || user.Estado != "Activa")
            throw new ConflictException(ErrorMessages.PerfilNoHabilitado);
        if (p.Edad < BusinessRules.MinEdadAdoptante || string.IsNullOrWhiteSpace(p.DatosLaborales) || string.IsNullOrWhiteSpace(p.EstadoCivil))
            throw new ValidationException("Completá tu perfil.");
        if (m.Estado != EstadoMascotaEnum.Disponible)
            throw new ConflictException(ErrorMessages.MascotaNoDisponible);
        if (await repo.AnyAsync<SolicitudDeAdopcion>(x => EstadosActivos.Contains(x.Estado) && (x.MascotaId == m.Id || x.AdoptanteId == p.Id)))
            throw new ConflictException(ErrorMessages.ProcesoActivo);
        var s = new SolicitudDeAdopcion
        {
            MascotaId = m.Id,
            AdoptanteId = p.Id,
            FechaSolicitud = Now,
            UltimaActividad = Now,
            MotivoAdopcion = Text(d.MotivoAdopcion, "Motivo de adopción"),
            CodigoConfirmacion = Convert.ToHexString(RandomNumberGenerator.GetBytes(8))
        };
        m.Estado = EstadoMascotaEnum.Reservada;
        await Save(a, "CU-02/CU-05 Postulación", s.Id, "Proceso creado", new Cambio(s, true), new Cambio(m), Notice(s.Id, "Nueva postulación", "Administrador"), Notice(s.Id, "Confirmación: " + s.CodigoConfirmacion, user: user.Id), Notice(s.Id, "Confirmación: " + s.CodigoConfirmacion, user: user.Id, channel: "CorreoBccPendiente", destination: p.Email));
        return s.ToDto();
    }

    public async Task<SolicitudResponseDTO> EvaluateAsync(Actor a, Guid id, EvaluacionDTO d)
    {
        Require(a, "Administrador", "Voluntario");
        var s = await Find<SolicitudDeAdopcion>(id);
        if (s.Estado is not (EstadoSolicitudEnum.Pendiente or EstadoSolicitudEnum.EnObservacion))
            throw new ConflictException("No se puede evaluar en este estado.");
        if (d.Estado is not (EstadoSolicitudEnum.Aprobada or EstadoSolicitudEnum.Rechazada or EstadoSolicitudEnum.EnObservacion))
            throw new ValidationException("Estado de evaluación inválido.");
        if (d.Estado == EstadoSolicitudEnum.Aprobada && Now < s.FechaSolicitud.AddDays(BusinessRules.MinDiasAntesAprobacion))
            throw new ConflictException($"Deben transcurrir al menos {BusinessRules.MinDiasAntesAprobacion} días.");
        var m = await Find<Mascota>(s.MascotaId);
        var changes = new List<Cambio>();
        var bloqueado = await Blocked(s.AdoptanteId);
        if (bloqueado || m.Estado != EstadoMascotaEnum.Reservada)
        {
            s.Estado = bloqueado ? EstadoSolicitudEnum.Rechazada : EstadoSolicitudEnum.Cancelada;
            s.Motivo = "Persona sancionada o mascota no disponible";
            s.UltimaActividad = Now;
            if (m.Estado == EstadoMascotaEnum.Reservada)
            {
                m.Estado = EstadoMascotaEnum.Disponible;
                changes.Add(new Cambio(m));
            }

            changes.Add(new Cambio(s));
            await Save(a, "CU-04 Bloqueo", s.Id, s.Motivo, changes.ToArray());
            throw new ConflictException(s.Motivo);
        }

        s.Motivo = d.Estado == EstadoSolicitudEnum.Aprobada ? d.Motivo?.Trim() : Text(d.Motivo, "Motivo/observaciones");
        s.EntidadAutorizante = Text(d.EntidadAutorizante, "Entidad autorizante", BusinessRules.MaxLongitudEntidadAutorizante);
        s.EvaluadorId = a.Id;
        s.FechaEvaluacion = Now;
        s.UltimaActividad = Now;
        s.Estado = d.Estado;
        if (d.Estado == EstadoSolicitudEnum.Rechazada)
            m.Estado = EstadoMascotaEnum.Disponible;
        await Save(a, "CU-04 Evaluación", s.Id, s.Motivo ?? "Aprobada", new Cambio(s), new Cambio(m));
        return s.ToDto();
    }

    public async Task<SolicitudResponseDTO> CancelAsync(Actor a, Guid id, string reason)
    {
        Require(a, "Administrador", "Voluntario", "Adoptante");
        var s = await Find<SolicitudDeAdopcion>(id);
        Own(a, s);
        if (!Activa(s.Estado))
            throw new ConflictException("El proceso ya terminó.");
        reason = Text(reason, "Motivo");
        var m = await Find<Mascota>(s.MascotaId);
        var changes = new List<Cambio>();
        if (s.Estado == EstadoSolicitudEnum.EnPeriodoDePrueba)
            changes.Add(new(new SancionBlacklist { AdoptanteId = s.AdoptanteId, Motivo = "Cancelación durante prueba: " + reason, Desde = Now, Hasta = Now.AddYears(BusinessRules.AniosSancionCancelacionPrueba) }, true));
        s.Estado = EstadoSolicitudEnum.Cancelada;
        s.Motivo = reason;
        s.UltimaActividad = Now;
        m.Estado = EstadoMascotaEnum.Disponible;
        changes.Add(new Cambio(s));
        changes.Add(new Cambio(m));
        // SECURITY: Decrementar contador de procesos activos del voluntario asignado al cancelar la solicitud.
        if (s.VoluntarioId.HasValue)
        {
            var volunteer = await Find<Usuario>(s.VoluntarioId.Value);
            if (volunteer.ProcesosActivos > 0)
            {
                volunteer.ProcesosActivos--;
                changes.Add(new Cambio(volunteer));
            }
        }
        await Save(a, "CU-13 Cancelación", id, reason, changes.ToArray());
        return s.ToDto();
    }

    public async Task<EntrevistaResponseDTO> ScheduleAsync(Actor a, Guid id, EntrevistaDTO d)
    {
        Require(a, "Administrador", "Voluntario");
        var s = await Find<SolicitudDeAdopcion>(id);
        if (s.Estado != EstadoSolicitudEnum.Aprobada)
            throw new ConflictException("La solicitud debe estar aprobada.");
        var volunteer = await Find<Usuario>(d.VoluntarioId);
        if (volunteer.Rol != "Voluntario" || volunteer.Estado != "Activa")
            throw new ValidationException("Voluntario no habilitado.");
        if (d.FechaProgramada.ToUniversalTime() < Now)
            throw new ValidationException("La entrevista no puede programarse en el pasado.");

        // SECURITY: RN-16 - Control de concurrencia optimista para límite de procesos activos por voluntario.
        // Se valida ProcesosActivos y se incrementa bajo el token de concurrencia Version (heredado de Registro): en SQLite RowVersion no se genera, por lo que no protege.
        if (volunteer.ProcesosActivos >= BusinessRules.MaxProcesosActivosPorVoluntario)
            throw new ConflictException("El voluntario ya tiene dos procesos activos asignados.");

        volunteer.ProcesosActivos++;

        var e = new Entrevista
        {
            SolicitudId = id,
            VoluntarioId = d.VoluntarioId,
            FechaProgramada = d.FechaProgramada.ToUniversalTime()
        };
        s.Estado = EstadoSolicitudEnum.EnEntrevista;
        s.VoluntarioId = d.VoluntarioId;
        s.UltimaActividad = Now;
        var m = await Find<Mascota>(s.MascotaId);
        m.Estado = EstadoMascotaEnum.EnEntrevista;

        try
        {
            await Save(a, "CU-11 Programar", id, "Entrevista programada",
                new Cambio(e, true),
                new Cambio(s),
                new Cambio(m),
                new Cambio(volunteer),
                Notice(id, "Entrevista asignada", user: d.VoluntarioId));
        }
        catch (ConflictException)
        {
            // SECURITY: RN-16 - El repositorio ya tradujo DbUpdateConcurrencyException (token Version) a ConflictException.
            // Se relee el voluntario: si otro proceso ocupó el último cupo el error es RN-16; si el conflicto fue en otra
            // entidad (solicitud, mascota) se propaga el conflicto original en lugar de enmascararlo como RN-16.
            var current = await repo.FindAsync<Usuario>(d.VoluntarioId);
            if (current is not null && current.ProcesosActivos >= BusinessRules.MaxProcesosActivosPorVoluntario)
                throw new ConflictException("El voluntario ya tiene dos procesos activos asignados.");
            throw;
        }

        return e.ToDto();
    }

    public async Task<List<EntrevistaResponseDTO>> InterviewsAsync(Actor a, Guid id)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Own(a, s);
        return (await repo.ListAsync<Entrevista>(x => x.SolicitudId == id)).Select(x => x.ToDto()).ToList();
    }

    public async Task<SolicitudResponseDTO> InterviewResultAsync(Actor a, Guid id, ResultadoEntrevistaDTO d)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Assigned(a, s);
        if (s.Estado != EstadoSolicitudEnum.EnEntrevista)
            throw new ConflictException("No está en entrevista.");
        if (d.Resultado is not ("Favorable" or "Desfavorable"))
            throw new ValidationException("Resultado inválido.");
        var e = await repo.FirstAsync<Entrevista>(x => x.SolicitudId == id) ?? throw new NotFoundException("Entrevista inexistente.");
        if (Now < e.FechaProgramada)
            throw new ConflictException("La entrevista todavía no llegó a su fecha programada.");
        e.Resultado = d.Resultado;
        e.Observaciones = Text(d.Observaciones, "Observaciones");
        e.FechaRealizada = Now;
        s.UltimaActividad = Now;
        var m = await Find<Mascota>(s.MascotaId);
        var changes = new List<Cambio> { new(e), new(s), new(m) };
        if (d.Resultado == "Favorable")
        {
            s.Estado = EstadoSolicitudEnum.EnPeriodoDePrueba;
            s.InicioPrueba = Now;
            m.Estado = EstadoMascotaEnum.EnPeriodoDePrueba;
        }
        else
        {
            s.Estado = EstadoSolicitudEnum.Rechazada;
            s.Motivo = e.Observaciones;
            m.Estado = EstadoMascotaEnum.Disponible;
            // SECURITY: Si la entrevista no es favorable, se libera el proceso activo asignado al voluntario.
            if (s.VoluntarioId.HasValue)
            {
                var volunteer = await Find<Usuario>(s.VoluntarioId.Value);
                if (volunteer.ProcesosActivos > 0)
                {
                    volunteer.ProcesosActivos--;
                    changes.Add(new Cambio(volunteer));
                }
            }
        }

        await Save(a, "CU-11 Resultado", id, e.Observaciones, changes.ToArray());
        return s.ToDto();
    }

    public async Task<List<SeguimientoResponseDTO>> FollowupsAsync(Actor a, Guid id)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Own(a, s);
        return (await repo.ListAsync<Seguimiento>(x => x.SolicitudId == id)).OrderBy(x => x.Fecha).Select(x => x.ToDto()).ToList();
    }

    public async Task<SeguimientoResponseDTO> FollowupAsync(Actor a, Guid id, SeguimientoDTO d)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Assigned(a, s);
        if (s.Estado != EstadoSolicitudEnum.EnPeriodoDePrueba)
            throw new ConflictException("No está en período de prueba.");
        var entry = new Seguimiento
        {
            SolicitudId = id,
            VoluntarioId = a.Id,
            Fecha = Now.Date,
            Animo = Text(d.Animo, "Ánimo"),
            Alimentacion = Text(d.Alimentacion, "Alimentación"),
            Tareas = Text(d.Tareas, "Tareas"),
            Medicacion = Text(d.Medicacion, "Medicación"),
            Limpieza = Text(d.Limpieza, "Limpieza"),
            Alerta = d.Alerta
        };
        s.UltimaActividad = Now;
        if (d.Alerta)
        {
            s.Alerta = true;
            s.ValidacionVeterinaria = false;
            s.VeterinarioValidadorId = null;
        }

        var changes = new List<Cambio>
        {
            new Cambio(entry, true),
            new Cambio(s)
        };
        if (d.Alerta)
            changes.Add(Notice(id, "Alerta en seguimiento", "Veterinario"));
        await Save(a, "CU-12 Seguimiento", id, d.Alerta ? "Alerta" : "Registro diario", changes.ToArray());
        return entry.ToDto();
    }

    public async Task<SolicitudResponseDTO> FinalizeAsync(Actor a, Guid id, string reason)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Assigned(a, s);
        if (s.Estado != EstadoSolicitudEnum.EnPeriodoDePrueba || s.InicioPrueba is null)
            throw new ConflictException("No está en prueba.");
        if (Now < s.InicioPrueba.Value.AddDays(BusinessRules.MinDiasPeriodoPrueba))
            throw new ConflictException($"El período de prueba debe durar al menos {BusinessRules.MinDiasPeriodoPrueba} días.");
        if (s.Alerta && !s.ValidacionVeterinaria)
            throw new ConflictException(ErrorMessages.AlertaRequiereValidacion);
        var history = await repo.FirstAsync<HistorialClinico>(x => x.MascotaId == s.MascotaId)
            ?? throw new ConflictException(ErrorMessages.HistorialRequerido);
        if (await Blocked(s.AdoptanteId))
            throw new ConflictException("Adoptante sancionado.");
        s.Estado = EstadoSolicitudEnum.Adoptada;
        s.FechaFinalizacion = Now;
        s.UltimaActividad = Now;
        var m = await Find<Mascota>(s.MascotaId);
        m.Estado = EstadoMascotaEnum.Adoptada;
        var owner = await repo.FirstAsync<Usuario>(x => x.AdoptanteId == s.AdoptanteId) ?? throw new NotFoundException(ErrorMessages.RegistroInexistente);
        var clone = new HistorialClinico
        {
            MascotaId = null,
            AdoptanteId = s.AdoptanteId,
            OrigenHistorialId = history.Id,
            OrigenMascotaId = s.MascotaId,
            Fecha = history.Fecha,
            VeterinarioId = history.VeterinarioId,
            Peso = history.Peso,
            EstadoGeneral = history.EstadoGeneral,
            Vacunas = history.Vacunas,
            Antecedentes = history.Antecedentes,
            Esterilizada = history.Esterilizada
        };
        var cloneEvents = (await repo.ListAsync<EventoMedico>(e => e.HistorialClinicoId == history.Id))
            .Select(e => new EventoMedico
            {
                HistorialClinicoId = clone.Id,
                VeterinarioId = e.VeterinarioId,
                Fecha = e.Fecha,
                Tipo = e.Tipo,
                Descripcion = e.Descripcion,
                Tratamiento = e.Tratamiento
            })
            .ToList();
        history.MascotaId = null;
        var changes = new List<Cambio>
        {
            new Cambio(s),
            new Cambio(m),
            new Cambio(history),
            new Cambio(clone, true)
        };
        // SECURITY: Al completar la adopción, se decremente de forma consistente el contador de procesos activos del voluntario.
        if (s.VoluntarioId.HasValue)
        {
            var volunteer = await Find<Usuario>(s.VoluntarioId.Value);
            if (volunteer.ProcesosActivos > 0)
            {
                volunteer.ProcesosActivos--;
                changes.Add(new Cambio(volunteer));
            }
        }
        changes.AddRange(cloneEvents.Select(e => new Cambio(e, true)));
        changes.Add(Notice(id, "Adopción finalizada. El historial clínico fue transferido a tu perfil.", user: owner.Id));
        await Save(a, "CU-15 Finalizar", id, Text(reason, "Motivo"), changes.ToArray());
        return s.ToDto();
    }

    public async Task<int> AbandonAsync()
    {
        var corte = Now.AddDays(-BusinessRules.DiasInactividadAbandono);
        var candidatas = await repo.ListAsync<SolicitudDeAdopcion>(x => x.Estado == EstadoSolicitudEnum.EnEntrevista && x.UltimaActividad < corte);
        var mascotaIds = candidatas.Select(x => x.MascotaId).ToList();
        var pets = (await repo.ListAsync<Mascota>(m => mascotaIds.Contains(m.Id))).ToDictionary(m => m.Id);
        var count = 0;
        foreach (var s in candidatas)
        {
            var m = pets[s.MascotaId];
            s.Estado = EstadoSolicitudEnum.Abandonada;
            s.Motivo = $"Más de {BusinessRules.DiasInactividadAbandono} días sin actividad";
            s.UltimaActividad = Now;
            m.Estado = EstadoMascotaEnum.Disponible;
            var changes = new List<Cambio> { new(s), new(m) };
            // SECURITY: Liberar proceso activo del voluntario en solicitudes abandonadas por inactividad.
            if (s.VoluntarioId.HasValue)
            {
                var volunteer = await Find<Usuario>(s.VoluntarioId.Value);
                if (volunteer.ProcesosActivos > 0)
                {
                    volunteer.ProcesosActivos--;
                    changes.Add(new(volunteer));
                }
            }
            try
            {
                await Save(new(Guid.Empty, "Sistema", null), "CU-14 Abandono", s.Id, s.Motivo, changes.ToArray());
                count++;
            }
            catch (ConflictException)
            { /* Otra ejecución resolvió este proceso. */
            }
        }

        return count;
    }
}
