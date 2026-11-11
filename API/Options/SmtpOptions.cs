namespace PortalAdopcion.API.Options;

public class SmtpOptions
{
    public const string SectionName = "Smtp";
    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "no-reply@portaladopcion.local";

    // REFACTOR: parámetros de resiliencia configurables (antes estaban fijos en el código).
    /// <summary>Timeout de red del cliente SMTP, en segundos.</summary>
    public int TimeoutSeconds { get; set; } = 30;
    /// <summary>Intentos de envío por correo dentro de un mismo ciclo (mínimo 1).</summary>
    public int MaxAttempts { get; set; } = 3;
    /// <summary>Espera base del backoff exponencial entre intentos, en milisegundos.</summary>
    public int RetryBaseDelayMs { get; set; } = 500;
    /// <summary>Segundos entre revisiones de la bandeja de correos pendientes.</summary>
    public int PollIntervalSeconds { get; set; } = 60;
}
