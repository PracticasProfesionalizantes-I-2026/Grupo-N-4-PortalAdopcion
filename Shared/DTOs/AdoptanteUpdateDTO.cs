namespace PortalAdopcion.Shared.DTOs;

public record AdoptanteUpdateDTO(
    string NombreCompleto,
    string Email,
    string Dni,
    string Telefono,
    string Direccion);