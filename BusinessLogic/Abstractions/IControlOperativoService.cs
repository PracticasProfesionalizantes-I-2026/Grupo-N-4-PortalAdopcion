namespace PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.DataAccess.Entities;

// REFACTOR: Blacklist, auditoría, notificaciones, incidentes y reportes.
public interface IControlOperativoService
{
    Task<List<SancionResponseDTO>> BlacklistAsync(Actor a);
    Task<SancionResponseDTO> SaveBlacklistAsync(Actor a, Guid? id, BlacklistDTO d);
    Task RevokeBlacklistAsync(Actor a, Guid id, string reason);
    Task<IncidenteResponseDTO> IncidentAsync(Actor a, IncidenteDTO d);
    Task<List<IncidenteResponseDTO>> IncidentsAsync(Actor a);
    Task<List<AuditoriaResponseDTO>> AuditAsync(Actor a, DateTime? desde, DateTime? hasta, string? accion, Guid? usuario);
    Task<List<NotificacionResponseDTO>> NotificationsAsync(Actor a);
    Task<List<string[]>> ReportAsync(Actor a, string tipo, DateTime desde, DateTime hasta);
}
