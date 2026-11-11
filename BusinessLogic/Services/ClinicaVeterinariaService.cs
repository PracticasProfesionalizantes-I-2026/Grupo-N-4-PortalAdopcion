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

// REFACTOR: Extraído de PortalService. Responsabilidad: Historial clínico inicial, eventos médicos y validación veterinaria.
public class ClinicaVeterinariaService(IPortalRepository repository, TimeProvider timeProvider) : ServiceBase(repository, timeProvider), IClinicaVeterinariaService
{
    public async Task<SolicitudResponseDTO> ValidateClinicalAsync(Actor a, Guid id, string reason)
    {
        Require(a, "Veterinario");
        var s = await Find<SolicitudDeAdopcion>(id);
        if (s.Estado != EstadoSolicitudEnum.EnPeriodoDePrueba)
            throw new ConflictException("No está en prueba.");
        s.ValidacionVeterinaria = true;
        s.VeterinarioValidadorId = a.Id;
        s.UltimaActividad = Now;
        await Save(a, "CU-12 Validación veterinaria", id, Text(reason, "Motivo"), new Cambio(s));
        return s.ToDto();
    }

    public async Task<HistorialResponseDTO> CreateHistoryAsync(Actor a, Guid pet, ClinicaDTO d)
    {
        Require(a, "Veterinario");
        await Find<Mascota>(pet);
        if (d.Peso <= BusinessRules.MinPesoMascota || d.Peso > BusinessRules.MaxPesoMascota)
            throw new ValidationException("Peso inválido.");
        if (await repo.AnyAsync<HistorialClinico>(x => x.MascotaId == pet))
            throw new ConflictException("El historial ya existe.");
        var h = new HistorialClinico
        {
            MascotaId = pet,
            Fecha = Now,
            VeterinarioId = a.Id,
            Peso = d.Peso,
            EstadoGeneral = Text(d.EstadoGeneral, "Estado general"),
            Vacunas = Text(d.Vacunas, "Vacunas"),
            Antecedentes = Text(d.Antecedentes, "Antecedentes"),
            Esterilizada = d.Esterilizada
        };
        var e = new EventoMedico
        {
            HistorialClinicoId = h.Id,
            VeterinarioId = a.Id,
            Fecha = Now,
            Tipo = "Inicial",
            Descripcion = h.EstadoGeneral,
            Tratamiento = h.Antecedentes
        };
        await Save(a, "CU-03 Historial", h.Id, "Registro inicial", new Cambio(h, true), new Cambio(e, true));
        return h.ToDto();
    }

    private async Task ClinicalAccess(Actor a, Guid pet)
    {
        if (a.Rol is "Administrador" or "Veterinario")
            return;
        if (a.Rol == "Adoptante" && await repo.AnyAsync<SolicitudDeAdopcion>(x => x.MascotaId == pet && x.AdoptanteId == a.AdoptanteId && x.Estado == EstadoSolicitudEnum.Adoptada))
            return;
        throw new AccessException("El historial es accesible al veterinario, administración y adoptante al finalizar.");
    }

    public async Task<HistorialDetalleResponseDTO> HistoryAsync(Actor a, Guid pet)
    {
        await ClinicalAccess(a, pet);
        var h = await repo.FirstAsync<HistorialClinico>(x => x.MascotaId == pet)
            ?? await repo.FirstAsync<HistorialClinico>(x => x.OrigenMascotaId == pet)
            ?? throw new NotFoundException("Historial inexistente.");
        return new HistorialDetalleResponseDTO(h.ToDto(), (await repo.ListAsync<EventoMedico>(x => x.HistorialClinicoId == h.Id)).OrderBy(x => x.Fecha).Select(x => x.ToDto()).ToList());
    }

    public async Task<EventoMedicoResponseDTO> MedicalEventAsync(Actor a, Guid pet, EventoMedicoDTO d)
    {
        Require(a, "Veterinario");
        var h = await repo.FirstAsync<HistorialClinico>(x => x.MascotaId == pet) ?? throw new NotFoundException("Creá primero el historial inicial.");
        var e = new EventoMedico
        {
            HistorialClinicoId = h.Id,
            VeterinarioId = a.Id,
            Fecha = Now,
            Tipo = Text(d.Tipo, "Tipo", BusinessRules.MaxLongitudTipoEventoMedico),
            Descripcion = Text(d.Descripcion, "Descripción"),
            Tratamiento = Text(d.Tratamiento, "Tratamiento")
        };
        await Save(a, "CU-10 Evento médico", e.Id, e.Tipo, new Cambio(e, true));
        return e.ToDto();
    }
}
