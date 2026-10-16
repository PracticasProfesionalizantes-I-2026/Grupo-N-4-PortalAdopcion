namespace PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.Shared.Enums;

public class SolicitudDeAdopcion : Registro
{
    public Guid MascotaId { get; set; }
    public Guid AdoptanteId { get; set; }
    public DateTime FechaSolicitud { get; set; }
    public DateTime UltimaActividad { get; set; }
    public EstadoSolicitudEnum Estado { get; set; } = EstadoSolicitudEnum.Pendiente;
    public string? Motivo { get; set; }
    public string MotivoAdopcion { get; set; } = "";
    public string CodigoConfirmacion { get; set; } = "";
    public Guid? EvaluadorId { get; set; }
    public string? EntidadAutorizante { get; set; }
    public DateTime? FechaEvaluacion { get; set; }
    public DateTime? InicioPrueba { get; set; }
    public Guid? VoluntarioId { get; set; }
    public bool Alerta { get; set; }
    public bool ValidacionVeterinaria { get; set; }
    public Guid? VeterinarioValidadorId { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
}
