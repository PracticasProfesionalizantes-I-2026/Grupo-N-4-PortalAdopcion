namespace PortalAdopcion.BusinessLogic.Services;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.BusinessLogic.Mapping;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.Constants;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;
using PortalAdopcion.Shared.Security;

// REFACTOR: Base común de los servicios de dominio (antes todo vivía en PortalService).
// Concentra repositorio, reloj y validaciones/helpers compartidos; las reglas de negocio no cambiaron.
public abstract class ServiceBase
{
    protected readonly IPortalRepository repo;
    protected readonly TimeProvider clock;

    protected ServiceBase(IPortalRepository repo, TimeProvider clock)
    {
        this.repo = repo;
        this.clock = clock;
    }

    protected DateTime Now => clock.GetUtcNow().UtcDateTime;

    public static bool Activa(EstadoSolicitudEnum e) => e is EstadoSolicitudEnum.Pendiente or EstadoSolicitudEnum.EnObservacion or EstadoSolicitudEnum.Aprobada or EstadoSolicitudEnum.EnEntrevista or EstadoSolicitudEnum.EnPeriodoDePrueba;

    // Se traduce a un IN (...) en SQL en lugar de filtrar en memoria (evita leer toda la tabla).
    protected static readonly EstadoSolicitudEnum[] EstadosActivos = [EstadoSolicitudEnum.Pendiente, EstadoSolicitudEnum.EnObservacion, EstadoSolicitudEnum.Aprobada, EstadoSolicitudEnum.EnEntrevista, EstadoSolicitudEnum.EnPeriodoDePrueba];

    protected static void Require(Actor a, params string[] roles)
    {
        if (!roles.Contains(a.Rol))
            throw new AccessException("Tu rol no permite esta operación.");
    }

    protected static string Text(string? s, string campo, int max = BusinessRules.MaxLongitudTexto)
    {
        if (string.IsNullOrWhiteSpace(s) || s.Trim().Length > max)
            throw new ValidationException($"{campo}: valor obligatorio de hasta {max} caracteres.");
        return s.Trim();
    }

    protected static string Numeric(string? s, string campo, int max)
    {
        var v = Text(s, campo, max);
        // Normalizar separadores comunes que el usuario puede introducir (espacio NBSP, guiones largos, puntos medios).
        v = v.Replace('\u00A0', ' ').Replace('·', '.').Replace('•', '.').Replace('–', '-').Replace('—', '-');
        // Permitir que el usuario pase separadores (puntos, guiones, espacios).
        // Rechazar otros caracteres (letras, símbolos) y normalizar a solo dígitos ASCII.
        var invalidChars = v.Where(c => !((c >= '0' && c <= '9') || c == '.' || c == '-' || char.IsWhiteSpace(c))).Distinct().ToArray();
        if (invalidChars.Length > 0)
            throw new ValidationException($"{campo}: caracteres inválidos: '{new string(invalidChars)}'. Solo se permiten dígitos (0-9), puntos, guiones y espacios.");
        var digits = new string(v.Where(c => c >= '0' && c <= '9').ToArray());
        if (string.IsNullOrEmpty(digits))
            throw new ValidationException($"{campo}: valor obligatorio de hasta {max} dígitos.");
        if (digits.Length > max)
            throw new ValidationException($"{campo}: máximo {max} dígitos permitidos.");
        return digits;
    }

    protected static string Civil(string? s)
    {
        var v = Text(s, "Estado civil", BusinessRules.MaxLongitudEstadoCivil);
        if (!BusinessRules.EstadosCiviles.Contains(v))
            throw new ValidationException("Estado civil inválido. Opciones: " + string.Join(", ", BusinessRules.EstadosCiviles) + ".");
        return v;
    }

    protected static string Email(string s)
    {
        s = Text(s, "Email", BusinessRules.MaxLongitudEmail).ToLowerInvariant();
        if (!MailAddress.TryCreate(s, out var a) || a.Address != s)
            throw new ValidationException("Email inválido.");
        return s;
    }

    protected static void Password(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length < BusinessRules.PasswordMinLength || s.Length > BusinessRules.PasswordMaxLength || !s.Any(char.IsUpper) || !s.Any(char.IsLower) || !s.Any(char.IsDigit))
            throw new ValidationException("Contraseña: " + BusinessRules.ContrasenaRequisitos);
    }

    protected static void EnumValid<T>(T value)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ValidationException("Valor de enumeración inválido.");
    }

    protected async Task<T> Find<T>(Guid id)
        where T : Registro => await repo.FindAsync<T>(id) ?? throw new NotFoundException(ErrorMessages.RegistroInexistente);

    protected Task Save(Actor a, string action, Guid id, string reason, params Cambio[] changes) => repo.SaveAsync(changes, new EventoAuditoria { UsuarioId = a.Id, Fecha = Now, Accion = action, EntidadId = id, Motivo = reason });

    protected Cambio Notice(Guid id, string msg, string? role = null, Guid? user = null, CanalNotificacionEnum channel = CanalNotificacionEnum.Interna, string? destination = null) => new(new Notificacion { EntidadId = id, Mensaje = msg, Rol = role, UsuarioId = user, Fecha = Now, Canal = channel, Destino = destination }, true);

    protected Task<bool> Blocked(Guid profile) => repo.AnyAsync<SancionBlacklist>(x => x.AdoptanteId == profile && !x.Revocada && x.Desde <= Now && x.Hasta > Now);

    protected static void Assigned(Actor a, SolicitudDeAdopcion s)
    {
        Require(a, "Administrador", "Voluntario");
        if (a.Rol == "Voluntario" && s.VoluntarioId != a.Id)
            throw new AccessException("El proceso está asignado a otro voluntario.");
    }
}
