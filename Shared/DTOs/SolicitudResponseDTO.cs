namespace PortalAdopcion.Shared.DTOs;

using PortalAdopcion.Shared.Enums;

public record SolicitudResponseDTO(
    Guid Id,
    Guid MascotaId,
    Guid AdoptanteId,
    DateTime FechaSolicitud,
    EstadoSolicitudEnum Estado,
    string? Motivo);