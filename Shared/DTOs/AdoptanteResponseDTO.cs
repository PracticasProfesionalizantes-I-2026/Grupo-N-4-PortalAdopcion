namespace PortalAdopcion.Shared.DTOs;

public record AdoptanteResponseDTO(
    Guid Id,
    string NombreCompleto,
    string Email,
    string Dni,
    string Telefono,
    string Direccion,
    DateTime FechaRegistro);