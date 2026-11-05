using PortalAdopcion.BusinessLogic.Abstractions;

public class AbandonoJob(IServiceScopeFactory scopes, ILogger<AbandonoJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ejecuta al iniciar y luego una vez al día. Es idempotente y usa la fecha de actividad.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var s = scopes.CreateScope();
                var count = await s.ServiceProvider.GetRequiredService<IPortalService>().AbandonAsync();
                logger.LogInformation("Abandonos procesados: {Count}", count);
            }
            catch (Exception ex)when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "No se pudo ejecutar abandono automático");
            }

            try
            {
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
