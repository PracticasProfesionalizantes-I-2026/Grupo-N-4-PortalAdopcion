namespace PortalAdopcion.Shared.DTOs;

using PortalAdopcion.Shared.Enums;

public record SolicitudUpdateEstadoDTO(
    EstadoSolicitudEnum Estado,
    string? Motivo);