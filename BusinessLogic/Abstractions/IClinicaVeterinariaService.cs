namespace PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.DataAccess.Entities;

// REFACTOR: Historial clínico inicial, eventos médicos y validación veterinaria.
public interface IClinicaVeterinariaService
{
    Task<SolicitudResponseDTO> ValidateClinicalAsync(Actor a, Guid id, string reason);
    Task<HistorialResponseDTO> CreateHistoryAsync(Actor a, Guid pet, ClinicaDTO d);
    Task<HistorialDetalleResponseDTO> HistoryAsync(Actor a, Guid pet);
    Task<EventoMedicoResponseDTO> MedicalEventAsync(Actor a, Guid pet, EventoMedicoDTO d);
}
