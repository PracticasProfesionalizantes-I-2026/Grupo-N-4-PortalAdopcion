namespace PortalAdopcion.DataAccess.Entities;
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
    public string Estado { get; set; } = "Activa";
    public string? Matricula { get; set; }
    public Guid? AdoptanteId { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
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
    public string Gravedad { get; set; } = "Normal";
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
    public string Canal { get; set; } = "Interna";
    public string? Destino { get; set; }
}
