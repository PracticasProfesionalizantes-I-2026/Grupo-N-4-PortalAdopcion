namespace PortalAdopcion.Shared.DTOs;

using PortalAdopcion.Shared.Enums;

public record MascotaResponseDTO(
    Guid Id,
    string Nombre,
    EspecieEnum Especie,
    string? Raza,
    int Edad,
    SexoEnum Sexo,
    EstadoMascotaEnum Estado,
    DateTime FechaRegistro);