namespace PortalAdopcion.BusinessLogic.Mapping;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.Shared.DTOs;

public static class PortalMapper
{
    public static UsuarioResponseDTO ToDto(this Usuario x) => new(x.Id, x.Version, x.Nombre, x.Email, x.Dni, x.Rol, x.Estado, x.Matricula, x.AdoptanteId, x.FechaRegistro);
    public static AdoptanteResponseDTO ToDto(this Adoptante x) => new(x.Id, x.Version, x.NombreCompleto, x.Email, x.Dni, x.Telefono, x.Direccion, x.Edad, x.EstadoCivil, x.DatosLaborales, x.FechaRegistro, x.Baja);
    public static MascotaResponseDTO ToDto(this Mascota x) => new(x.Id, x.Version, x.Nombre, x.Especie, x.Raza, x.Edad, x.Sexo, x.FotoUrl, x.Chip, x.Estado, x.FechaRegistro);
    public static SolicitudResponseDTO ToDto(this SolicitudDeAdopcion x) => new(x.Id, x.Version, x.MascotaId, x.AdoptanteId, x.FechaSolicitud, x.UltimaActividad, x.Estado, x.Motivo, x.MotivoAdopcion, x.CodigoConfirmacion, x.EvaluadorId, x.EntidadAutorizante, x.FechaEvaluacion, x.InicioPrueba, x.VoluntarioId, x.Alerta, x.ValidacionVeterinaria, x.VeterinarioValidadorId, x.FechaFinalizacion);
    public static SancionResponseDTO ToDto(this SancionBlacklist x) => new(x.Id, x.Version, x.AdoptanteId, x.Motivo, x.Desde, x.Hasta, x.Revocada);
    public static EntrevistaResponseDTO ToDto(this Entrevista x) => new(x.Id, x.Version, x.SolicitudId, x.VoluntarioId, x.FechaProgramada, x.FechaRealizada, x.Resultado, x.Observaciones);
    public static SeguimientoResponseDTO ToDto(this Seguimiento x) => new(x.Id, x.Version, x.SolicitudId, x.VoluntarioId, x.Fecha, x.Animo, x.Alimentacion, x.Tareas, x.Medicacion, x.Limpieza, x.Alerta);
    public static HistorialResponseDTO ToDto(this HistorialClinico x) => new(x.Id, x.Version, x.MascotaId, x.AdoptanteId, x.OrigenMascotaId, x.Fecha, x.VeterinarioId, x.Peso, x.EstadoGeneral, x.Vacunas, x.Antecedentes, x.Esterilizada);
    public static EventoMedicoResponseDTO ToDto(this EventoMedico x) => new(x.Id, x.Version, x.HistorialClinicoId, x.VeterinarioId, x.Fecha, x.Tipo, x.Descripcion, x.Tratamiento);
    public static IncidenteResponseDTO ToDto(this Incidente x) => new(x.Id, x.Version, x.SolicitudId, x.VoluntarioId, x.Fecha, x.Gravedad, x.Detalle);
    public static AuditoriaResponseDTO ToDto(this EventoAuditoria x) => new(x.Id, x.Version, x.UsuarioId, x.Fecha, x.Accion, x.EntidadId, x.Motivo);
    public static NotificacionResponseDTO ToDto(this Notificacion x) => new(x.Id, x.Version, x.UsuarioId, x.Rol, x.Mensaje, x.Fecha, x.EntidadId, x.Canal, x.Destino);
}
