namespace PortalAdopcion.Shared.DTOs;

public record AdoptanteCreateDTO(
    string NombreCompleto,
    string Email,
    string Dni,
    string Telefono,
    string Direccion);