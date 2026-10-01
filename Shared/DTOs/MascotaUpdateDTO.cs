namespace PortalAdopcion.Shared.DTOs;

using PortalAdopcion.Shared.Enums;

public record MascotaUpdateDTO(
    string Nombre,
    EspecieEnum Especie,
    string? Raza,
    int Edad,
    SexoEnum Sexo,
    EstadoMascotaEnum Estado);