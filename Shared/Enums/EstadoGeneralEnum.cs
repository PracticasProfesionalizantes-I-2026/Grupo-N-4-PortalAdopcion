namespace PortalAdopcion.Shared.Enums;

/// <summary>
/// Estado de salud general de la mascota (historial clínico).
/// Los valores numéricos son explícitos y arrancan en 1: el 0 (valor por defecto de .NET)
/// no es un estado válido, así un campo ausente nunca se confunde con un estado real.
/// </summary>
public enum EstadoGeneralEnum
{
    Saludable = 1,
    Bien = 2,
    Excelente = 3,
    Bueno = 4,
    Regular = 5,
    Malo = 6,
    Critico = 7
}
