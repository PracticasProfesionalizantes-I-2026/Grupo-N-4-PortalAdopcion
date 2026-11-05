namespace PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.Shared.Enums;

public class Mascota : Registro
{
    public string Nombre { get; set; } = "";
    public EspecieEnum Especie { get; set; }
    public string? Raza { get; set; }
    public int Edad { get; set; }
    public SexoEnum Sexo { get; set; }
    public string? FotoUrl { get; set; }
    public string? Chip { get; set; }
    public EstadoMascotaEnum Estado { get; set; } = EstadoMascotaEnum.Disponible;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
