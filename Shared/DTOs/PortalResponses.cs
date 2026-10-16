namespace PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;

public record UsuarioResponseDTO(Guid Id, long Version, string Nombre, string Email, string Dni, string Rol, string Estado, string? Matricula, Guid? AdoptanteId, DateTime FechaRegistro);
public record AdoptanteResponseDTO(Guid Id, long Version, string NombreCompleto, string Email, string Dni, string Telefono, string Direccion, int Edad, string EstadoCivil, string DatosLaborales, DateTime FechaRegistro, bool Baja);
public record MascotaResponseDTO(Guid Id, long Version, string Nombre, EspecieEnum Especie, string? Raza, int Edad, SexoEnum Sexo, string? FotoUrl, string? Chip, EstadoMascotaEnum Estado, DateTime FechaRegistro);
public record SolicitudResponseDTO(Guid Id, long Version, Guid MascotaId, Guid AdoptanteId, DateTime FechaSolicitud, DateTime UltimaActividad, EstadoSolicitudEnum Estado, string? Motivo, string MotivoAdopcion, string CodigoConfirmacion, Guid? EvaluadorId, string? EntidadAutorizante, DateTime? FechaEvaluacion, DateTime? InicioPrueba, Guid? VoluntarioId, bool Alerta, bool ValidacionVeterinaria, Guid? VeterinarioValidadorId, DateTime? FechaFinalizacion);
public record SancionResponseDTO(Guid Id, long Version, Guid AdoptanteId, string Motivo, DateTime Desde, DateTime Hasta, bool Revocada);
public record EntrevistaResponseDTO(Guid Id, long Version, Guid SolicitudId, Guid VoluntarioId, DateTime FechaProgramada, DateTime? FechaRealizada, string? Resultado, string? Observaciones);
public record SeguimientoResponseDTO(Guid Id, long Version, Guid SolicitudId, Guid VoluntarioId, DateTime Fecha, string Animo, string Alimentacion, string Tareas, string Medicacion, string Limpieza, bool Alerta);
public record HistorialResponseDTO(Guid Id, long Version, Guid? MascotaId, Guid? AdoptanteId, Guid? OrigenMascotaId, DateTime Fecha, Guid VeterinarioId, decimal Peso, string EstadoGeneral, string Vacunas, string Antecedentes, bool Esterilizada);
public record EventoMedicoResponseDTO(Guid Id, long Version, Guid HistorialClinicoId, Guid VeterinarioId, DateTime Fecha, string Tipo, string Descripcion, string Tratamiento);
public record IncidenteResponseDTO(Guid Id, long Version, Guid SolicitudId, Guid VoluntarioId, DateTime Fecha, string Gravedad, string Detalle);
public record AuditoriaResponseDTO(Guid Id, long Version, Guid UsuarioId, DateTime Fecha, string Accion, Guid EntidadId, string Motivo);
public record NotificacionResponseDTO(Guid Id, long Version, Guid? UsuarioId, string? Rol, string Mensaje, DateTime Fecha, Guid EntidadId, string Canal, string? Destino);
public record HistorialDetalleResponseDTO(HistorialResponseDTO Historial, IReadOnlyList<EventoMedicoResponseDTO> Eventos);
public record SesionResponseDTO(string Token, DateTime Expires, UsuarioResponseDTO Usuario);
