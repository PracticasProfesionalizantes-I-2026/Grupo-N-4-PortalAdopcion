namespace PortalAdopcion.API;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PortalAdopcion.API.Options;
using PortalAdopcion.API.Services;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;

// REFACTOR: Migración de SmtpClient obsoleto a MailKit/MimeKit con IOptions<SmtpOptions>. Un único cliente SMTP por lote
// (conexión reutilizada, creado y liberado dentro del lote: no se comparte entre hilos), reconexión automática y
// reintentos con backoff exponencial por correo.
public class CorreoJob(
    IServiceScopeFactory scopes,
    IOptions<SmtpOptions> smtpOptions,
    ISmtpClientFactory clientFactory,
    ILogger<CorreoJob> logger) : BackgroundService
{
    public const string CanalPendiente = "CorreoBccPendiente";
    public const string CanalEnviado = "CorreoBccEnviado";
    public const string CanalFallido = "CorreoBccFallido";
    private const int BatchSize = 20;

    private readonly SmtpOptions _options = smtpOptions.Value;
    private int MaxAttempts => Math.Max(1, _options.MaxAttempts);

    private enum Outcome { Sent, Pending, Rejected }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // SECURITY: El envío de correos permanece deshabilitado por defecto en pruebas y entornos sin configuración SMTP explícita.
        if (!_options.Enabled)
            return;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Falla a nivel de lote (host caído, credenciales, remitente inválido): los correos siguen pendientes y se reintentan en el próximo ciclo.
                logger.LogError(ex, "Error al procesar la bandeja de correos pendientes.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>Procesa un ciclo de la bandeja. Devuelve la cantidad de correos enviados.</summary>
    public async Task<int> ProcessPendingAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPortalRepository>();
        // La selección se resuelve en SQL (WHERE + ORDER BY + LIMIT); antes se leía toda la tabla de notificaciones y se filtraba en memoria.
        var page = await repo.PageAsync<Notificacion>(x => x.Canal == CanalPendiente, 1, BatchSize, ct);
        if (page.Items.Count == 0)
            return 0;
        return await SendBatchAsync(repo, page.Items, ct);
    }

    private async Task<int> SendBatchAsync(IPortalRepository repo, IReadOnlyList<Notificacion> pending, CancellationToken ct)
    {
        // Remitente inválido = error de configuración: aborta el lote SIN marcar correos como fallidos.
        var sender = MailboxAddress.Parse(_options.From);
        using var client = clientFactory.CreateClient();
        client.Timeout = Math.Max(1, _options.TimeoutSeconds) * 1000;
        var sent = 0;
        try
        {
            await EnsureConnectedAsync(client, ct);
            foreach (var entry in pending)
            {
                ct.ThrowIfCancellationRequested();
                var outcome = await TrySendAsync(client, sender, entry, ct);
                if (outcome == Outcome.Pending)
                    continue;

                entry.Canal = outcome == Outcome.Sent ? CanalEnviado : CanalFallido;
                try
                {
                    await repo.SaveAsync(new[] { new Cambio(entry) }, new EventoAuditoria
                    {
                        Fecha = DateTime.UtcNow,
                        Accion = outcome == Outcome.Sent ? "Correo BCC enviado" : "Correo BCC rechazado",
                        EntidadId = entry.EntidadId,
                        Motivo = outcome == Outcome.Sent ? "Entrega de confirmación" : "Destinatario o mensaje rechazado de forma permanente"
                    }, ct);
                }
                catch (PortalAdopcion.Shared.Exceptions.ConflictException ex)
                {
                    // El correo ya salió pero no se pudo registrar (otra instancia modificó la fila): queda pendiente y puede reenviarse (entrega al-menos-una-vez).
                    logger.LogWarning(ex, "No se pudo registrar el resultado del correo para la entidad {EntidadId}.", entry.EntidadId);
                    continue;
                }

                if (outcome == Outcome.Sent)
                {
                    sent++;
                    logger.LogInformation("Correo enviado a {Destino} para la entidad {EntidadId}.", entry.Destino, entry.EntidadId);
                }
            }
        }
        finally
        {
            if (client.IsConnected)
            {
                try
                {
                    await client.DisconnectAsync(true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Error al cerrar la conexión SMTP (se libera igualmente al hacer Dispose).");
                }
            }
        }

        return sent;
    }

    private async Task<Outcome> TrySendAsync(ISmtpClient client, MailboxAddress sender, Notificacion entry, CancellationToken ct)
    {
        MimeMessage message;
        try
        {
            message = BuildMessage(sender, entry);
        }
        catch (FormatException ex)
        {
            // MimeKit.ParseException deriva de FormatException: destinatario vacío o con formato inválido, no tiene sentido reintentar.
            logger.LogError(ex, "Destinatario inválido en la entidad {EntidadId}; se marca como fallido.", entry.EntidadId);
            return Outcome.Rejected;
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await EnsureConnectedAsync(client, ct);
                await client.SendAsync(message, ct);
                return Outcome.Sent;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (SmtpCommandException ex) when ((int)ex.StatusCode >= 500)
            {
                // 5xx: rechazo permanente del servidor (buzón inexistente, mensaje rechazado). Reintentar no cambia el resultado.
                logger.LogError(ex, "El servidor SMTP rechazó de forma permanente el correo a {Destino}.", entry.Destino);
                return Outcome.Rejected;
            }
            catch (Exception ex)
            {
                if (attempt == MaxAttempts)
                {
                    logger.LogError(ex, "No se pudo enviar el correo a {Destino} tras {Max} intentos; queda pendiente.", entry.Destino, MaxAttempts);
                    return Outcome.Pending;
                }

                var delay = TimeSpan.FromMilliseconds(Math.Max(0, _options.RetryBaseDelayMs) * Math.Pow(2, attempt - 1));
                logger.LogWarning(ex, "Fallo al enviar correo a {Destino} (intento {Intento}/{Max}). Reintentando en {Delay}ms...", entry.Destino, attempt, MaxAttempts, (int)delay.TotalMilliseconds);
                await Task.Delay(delay, ct);
            }
        }

        return Outcome.Pending;
    }

    private static MimeMessage BuildMessage(MailboxAddress sender, Notificacion entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Destino))
            throw new FormatException("La notificación no tiene destinatario.");
        var message = new MimeMessage();
        message.From.Add(sender);
        message.To.Add(sender);
        message.Bcc.Add(MailboxAddress.Parse(entry.Destino));
        message.Subject = "Confirmación de postulación - Portal de Adopción";
        message.Body = new TextPart("plain") { Text = entry.Mensaje };
        return message;
    }

    // Reconecta/autentica si la conexión se perdió (MailKit la cierra tras ciertos errores de red) en lugar de fallar todos los intentos restantes.
    private async Task EnsureConnectedAsync(ISmtpClient client, CancellationToken ct)
    {
        if (!client.IsConnected)
        {
            var secure = _options.EnableSsl
                ? (_options.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                : SecureSocketOptions.None;
            await client.ConnectAsync(_options.Host, _options.Port, secure, ct);
        }

        if (!string.IsNullOrEmpty(_options.Username) && !client.IsAuthenticated)
            await client.AuthenticateAsync(_options.Username, _options.Password ?? "", ct);
    }
}
