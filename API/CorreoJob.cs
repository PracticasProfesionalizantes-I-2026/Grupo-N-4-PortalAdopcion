using System.Net;
using System.Net.Mail;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;

public class CorreoJob(IServiceScopeFactory scopes, IConfiguration config, ILogger<CorreoJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!config.GetValue<bool>("Smtp:Enabled"))
            return;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IPortalRepository>();
                var messages = (await repo.ListAsync<Notificacion>(ct)).Where(x => x.Canal == "CorreoBccPendiente").Take(20).ToList();
                foreach (var entry in messages)
                {
                    var from = config["Smtp:From"] ?? throw new InvalidOperationException("Falta Smtp:From");
                    using var mail = new MailMessage
                    {
                        From = new MailAddress(from),
                        Subject = "Confirmación de postulación - Portal de Adopción",
                        Body = entry.Mensaje
                    };
                    mail.To.Add(from);
                    mail.Bcc.Add(entry.Destino!);
                    using var smtp = new SmtpClient(config["Smtp:Host"], config.GetValue<int>("Smtp:Port", 587))
                    {
                        EnableSsl = config.GetValue<bool>("Smtp:EnableSsl", true)
                    };
                    if (!string.IsNullOrEmpty(config["Smtp:Username"]))
                        smtp.Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"]);
                    await smtp.SendMailAsync(mail, ct);
                    entry.Canal = "CorreoBccEnviado";
                    await repo.SaveAsync([new(entry)], new EventoAuditoria { Fecha = DateTime.UtcNow, Accion = "Correo BCC enviado", EntidadId = entry.EntidadId, Motivo = "Entrega de confirmación" }, ct);
                }
            }
            catch (Exception ex)when (!ct.IsCancellationRequested)
            {
                logger.LogError(ex, "No se pudo procesar la bandeja de correo");
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
}
