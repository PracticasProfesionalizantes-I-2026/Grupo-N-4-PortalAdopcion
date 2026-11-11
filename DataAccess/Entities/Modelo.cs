namespace PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.Shared.Enums;
public abstract class Registro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long Version { get; set; }
}

public class Usuario : Registro
{
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string Dni { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Rol { get; set; } = "Adoptante";
    public EstadoUsuarioEnum Estado { get; set; } = EstadoUsuarioEnum.Activa;
    public string? Matricula { get; set; }
    public Guid? AdoptanteId { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    // SECURITY: Control de concurrencia optimista para RN-16 (máximo de procesos activos por voluntario).
    public int ProcesosActivos { get; set; }

    // NOTA: [Timestamp] solo tiene efecto en proveedores que generan rowversion (ej. SQL Server). SQLite NO lo genera:
    // aquí la columna queda siempre NULL y no protege nada. El control de concurrencia real de RN-16 es el campo
    // Version (long, IsConcurrencyToken) heredado de Registro, que PortalRepository.SaveAsync incrementa y comprueba.
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; set; }
}

public class SancionBlacklist : Registro
{
    public Guid AdoptanteId { get; set; }
    public string Motivo { get; set; } = "";
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public bool Revocada { get; set; }
}

public class HistorialClinico : Registro
{
    public Guid? MascotaId { get; set; }
    public Guid? AdoptanteId { get; set; }
    public Guid? OrigenHistorialId { get; set; }
    public Guid? OrigenMascotaId { get; set; }
    public DateTime Fecha { get; set; }
    public Guid VeterinarioId { get; set; }
    public decimal Peso { get; set; }
    public string EstadoGeneral { get; set; } = "";
    public string Vacunas { get; set; } = "";
    public string Antecedentes { get; set; } = "";
    public bool Esterilizada { get; set; }
}

public class EventoMedico : Registro
{
    public Guid HistorialClinicoId { get; set; }
    public Guid VeterinarioId { get; set; }
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Tratamiento { get; set; } = "";
}

public class Entrevista : Registro
{
    public Guid SolicitudId { get; set; }
    public Guid VoluntarioId { get; set; }
    public DateTime FechaProgramada { get; set; }
    public DateTime? FechaRealizada { get; set; }
    public string? Resultado { get; set; }
    public string? Observaciones { get; set; }
}

public class Seguimiento : Registro
{
    public Guid SolicitudId { get; set; }
    public Guid VoluntarioId { get; set; }
    public DateTime Fecha { get; set; }
    public string Animo { get; set; } = "";
    public string Alimentacion { get; set; } = "";
    public string Tareas { get; set; } = "";
    public string Medicacion { get; set; } = "";
    public string Limpieza { get; set; } = "";
    public bool Alerta { get; set; }
}

public class Incidente : Registro
{
    public Guid SolicitudId { get; set; }
    public Guid VoluntarioId { get; set; }
    public DateTime Fecha { get; set; }
    public GravedadIncidenteEnum Gravedad { get; set; } = GravedadIncidenteEnum.Normal;
    public string Detalle { get; set; } = "";
}

public class EventoAuditoria : Registro
{
    public Guid UsuarioId { get; set; }
    public DateTime Fecha { get; set; }
    public string Accion { get; set; } = "";
    public Guid EntidadId { get; set; }
    public string Motivo { get; set; } = "";
}

public class Notificacion : Registro
{
    public Guid? UsuarioId { get; set; }
    public string? Rol { get; set; }
    public string Mensaje { get; set; } = "";
    public DateTime Fecha { get; set; }
    public Guid EntidadId { get; set; }
    public CanalNotificacionEnum Canal { get; set; } = CanalNotificacionEnum.Interna;
    public string? Destino { get; set; }
}
