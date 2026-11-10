namespace PortalAdopcion.API;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using PortalAdopcion.API.Options;
using PortalAdopcion.API.Services;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;

// REFACTOR: Migración de SmtpClient obsoleto a MailKit/MimeKit con IOptions<SmtpOptions>, conexión reutilizada y política de reintentos con backoff.
public class CorreoJob(
    IServiceScopeFactory scopes,
    IOptions<SmtpOptions> smtpOptions,
    ISmtpClientFactory clientFactory,
    ILogger<CorreoJob> logger) : BackgroundService
{
    private readonly SmtpOptions _options = smtpOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // SECURITY: El envío de correos permanece deshabilitado por defecto en pruebas y entornos sin configuración SMTP explícita.
        if (!_options.Enabled)
            return;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IPortalRepository>();
                var messages = (await repo.ListAsync<Notificacion>(ct))
                    .Where(x => x.Canal == "CorreoBccPendiente")
                    .Take(20)
                    .ToList();

                if (messages.Count > 0)
                {
                    await ProcessBatchAsync(repo, messages, ct);
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogError(ex, "Error al procesar la bandeja de correos pendientes.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessBatchAsync(IPortalRepository repo, List<Notificacion> messages, CancellationToken ct)
    {
        using var client = clientFactory.CreateClient();
        try
        {
            var secureOption = _options.EnableSsl
                ? (_options.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                : SecureSocketOptions.None;

            await client.ConnectAsync(_options.Host, _options.Port, secureOption, ct);

            if (!string.IsNullOrEmpty(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password ?? "", ct);
            }

            foreach (var entry in messages)
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_options.From));
                message.To.Add(MailboxAddress.Parse(_options.From));
                if (!string.IsNullOrWhiteSpace(entry.Destino))
                {
                    message.Bcc.Add(MailboxAddress.Parse(entry.Destino));
                }
                message.Subject = "Confirmación de postulación - Portal de Adopción";
                message.Body = new TextPart("plain") { Text = entry.Mensaje };

                var sent = false;
                const int maxAttempts = 3;
                for (var attempt = 1; attempt <= maxAttempts && !sent; attempt++)
                {
                    try
                    {
                        await client.SendAsync(message, ct);
                        sent = true;
                    }
                    catch (Exception ex) when (attempt < maxAttempts && !ct.IsCancellationRequested)
                    {
                        var delayMs = (int)Math.Pow(2, attempt) * 500;
                        logger.LogWarning(ex, "Fallo al enviar correo a {Destino} (intento {Intento}/{Max}). Reintentando en {Delay}ms...", entry.Destino, attempt, maxAttempts, delayMs);
                        await Task.Delay(delayMs, ct);
                    }
                }

                if (sent)
                {
                    entry.Canal = "CorreoBccEnviado";
                    await repo.SaveAsync([new(entry)], new EventoAuditoria
                    {
                        Fecha = DateTime.UtcNow,
                        Accion = "Correo BCC enviado",
                        EntidadId = entry.EntidadId,
                        Motivo = "Entrega de confirmación"
                    }, ct);
                    logger.LogInformation("Correo enviado satisfactoriamente a {Destino} para la entidad {EntidadId}.", entry.Destino, entry.EntidadId);
                }
                else
                {
                    logger.LogError("No se pudo enviar el correo a {Destino} tras {Max} intentos.", entry.Destino, maxAttempts);
                }
            }
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, ct);
            }
        }
    }
}
