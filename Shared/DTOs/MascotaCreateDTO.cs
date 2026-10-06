namespace PortalAdopcion.Shared.DTOs;

using PortalAdopcion.Shared.Enums;

public record MascotaCreateDTO(
    string Nombre,
    EspecieEnum Especie,
    string? Raza,
    int Edad,
    SexoEnum Sexo);