namespace PortalAdopcion.DataAccess.Entities;

using PortalAdopcion.Shared.Enums;

public class Mascota
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public EspecieEnum Especie { get; set; }

    public string? Raza { get; set; }

    public int Edad { get; set; }

    public SexoEnum Sexo { get; set; }

    public EstadoMascotaEnum Estado { get; set; } = EstadoMascotaEnum.Disponible;

    public DateTime FechaRegistro { get; set; }

    public ICollection<SolicitudDeAdopcion> Solicitudes { get; set; } = new List<SolicitudDeAdopcion>();
}