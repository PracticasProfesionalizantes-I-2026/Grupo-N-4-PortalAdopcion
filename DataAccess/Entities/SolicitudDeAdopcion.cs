namespace PortalAdopcion.DataAccess.Entities;

using PortalAdopcion.Shared.Enums;

public class SolicitudDeAdopcion
{
    public Guid Id { get; set; }

    public Guid MascotaId { get; set; }

    public Guid AdoptanteId { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public EstadoSolicitudEnum Estado { get; set; } = EstadoSolicitudEnum.Pendiente;

    public string? Motivo { get; set; }

    public Mascota Mascota { get; set; } = null!;

    public Adoptante Adoptante { get; set; } = null!;
}