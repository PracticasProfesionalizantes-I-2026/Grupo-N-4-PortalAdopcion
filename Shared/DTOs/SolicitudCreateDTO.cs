namespace PortalAdopcion.Shared.DTOs;

public record SolicitudCreateDTO(
    Guid MascotaId,
    Guid AdoptanteId);