namespace Portal.Tests;
using System.Linq.Expressions;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Moq;
using PortalAdopcion.API;
using PortalAdopcion.API.Middleware;
using PortalAdopcion.API.Options;
using PortalAdopcion.API.Services;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;
using Xunit;

/// <summary>
/// Repositorio que delega en otro y, tras la PRIMERA lectura del voluntario indicado, ejecuta un competidor.
/// Reproduce de forma determinista la carrera "leer -> otro escribe -> guardar" (TOCTOU) sobre SQLite real.
/// </summary>
internal sealed class RacingRepository(IPortalRepository inner, Guid volunteerId, Func<Task> onFirstVolunteerRead) : IPortalRepository
{
    private Func<Task>? _hook = onFirstVolunteerRead;
    public bool Fired { get; private set; }

    public Task<List<T>> ListAsync<T>(CancellationToken ct = default)
        where T : Registro => inner.ListAsync<T>(ct);

    public Task<List<T>> ListAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro => inner.ListAsync<T>(predicate, ct);

    public Task<T?> FirstAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro => inner.FirstAsync<T>(predicate, ct);

    public Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro => inner.AnyAsync<T>(predicate, ct);

    public Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : Registro => inner.CountAsync<T>(predicate, ct);

    public Task<PagedResult<T>> PageAsync<T>(Expression<Func<T, bool>> predicate, int page, int pageSize, CancellationToken ct = default)
        where T : Registro => inner.PageAsync<T>(predicate, page, pageSize, ct);

    public async Task<T?> FindAsync<T>(Guid id, CancellationToken ct = default)
        where T : Registro
    {
        var result = await inner.FindAsync<T>(id, ct);
        if (_hook is not null && result is Usuario u && u.Id == volunteerId)
        {
            var hook = _hook;
            _hook = null;
            Fired = true;
            await hook();
        }

        return result;
    }

    public Task SaveAsync(IEnumerable<Cambio> cambios, EventoAuditoria auditoria, CancellationToken ct = default) => inner.SaveAsync(cambios, auditoria, ct);

    public Task ResetDemoDataAsync(CancellationToken ct = default) => inner.ResetDemoDataAsync(ct);
}

public class StartupGuardsTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void CanCleanMigrationLock_OnlyInDevelopmentWithSqlite(bool isDevelopment, bool isSqlite, bool expected)
        => Assert.Equal(expected, StartupGuards.CanCleanMigrationLock(isDevelopment, isSqlite));
}

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<int> StatusFor(Exception toThrow)
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw toThrow, NullLogger<ExceptionHandlingMiddleware>.Instance);
        var ctx = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        await middleware.InvokeAsync(ctx);
        return ctx.Response.StatusCode;
    }

    // A.2: decisión acordada - la sanción vigente responde 403 (AccessException), no 409.
    [Fact]
    public async Task AccessException_IsMappedTo403() => Assert.Equal(StatusCodes.Status403Forbidden, await StatusFor(new AccessException("Sancionado")));

    [Fact]
    public async Task ConflictException_IsStillMappedTo409() => Assert.Equal(StatusCodes.Status409Conflict, await StatusFor(new ConflictException("Conflicto")));

    [Fact]
    public async Task ValidationException_IsMappedTo400() => Assert.Equal(StatusCodes.Status400BadRequest, await StatusFor(new ValidationException("Inválido")));

    [Fact]
    public async Task NotFoundException_IsMappedTo404() => Assert.Equal(StatusCodes.Status404NotFound, await StatusFor(new NotFoundException("No existe")));

    [Fact]
    public async Task AuthenticationException_IsMappedTo401() => Assert.Equal(StatusCodes.Status401Unauthorized, await StatusFor(new PortalAdopcion.Shared.Exceptions.AuthenticationException("Credenciales")));
}

public class CorreoJobTests
{
    private sealed class Rig : IAsyncDisposable
    {
        public readonly Scenario Scenario = new();
        public readonly Mock<ISmtpClient> Client = new();
        public bool Connected;
        public int Connects, Sends;
        public CorreoJob Job = null!;

        // fail(mensaje, nroDeEnvio) devuelve la excepción a lanzar en ese envío, o null para que el envío sea correcto.
        public async Task Init(Func<MimeMessage, int, Exception?>? fail = null, int maxAttempts = 3)
        {
            await Scenario.Initialize();
            Client.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    Connected = true;
                    Connects++;
                })
                .Returns(Task.CompletedTask);
            Client.SetupGet(c => c.IsConnected).Returns(() => Connected);
            Client.Setup(c => c.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback(() => Connected = false)
                .Returns(Task.CompletedTask);
            Client.Setup(c => c.SendAsync(It.IsAny<ISendRequest>(), It.IsAny<CancellationToken>()))
                .Returns((ISendRequest request, CancellationToken _) =>
                {
                    Sends++;
                    var ex = fail?.Invoke(request.Message, Sends);
                    if (ex is not null)
                    {
                        // Un error de red real deja el cliente desconectado: el job debe reconectar antes de reintentar.
                        if (ex is IOException)
                            Connected = false;
                        throw ex;
                    }

                    // CorreoJob no necesita inspeccionar el SendResult; el mock solo simula un envío aceptado.
                    return Task.FromResult<SendResult>(null!);
                });
            var factory = new Mock<ISmtpClientFactory>();
            factory.Setup(f => f.CreateClient()).Returns(Client.Object);
            var provider = new ServiceCollection().AddSingleton<IPortalRepository>(Scenario.Repo).BuildServiceProvider();
            var options = new SmtpOptions { Enabled = true, Host = "smtp.test", Port = 587, MaxAttempts = maxAttempts, RetryBaseDelayMs = 1 };
            Job = new CorreoJob(provider.GetRequiredService<IServiceScopeFactory>(), Microsoft.Extensions.Options.Options.Create(options), factory.Object, NullLogger<CorreoJob>.Instance);
        }

        public async Task AddPending(string? destino)
        {
            await Scenario.Repo.SaveAsync(
                new[] { new Cambio(new Notificacion { EntidadId = Guid.NewGuid(), Mensaje = "Confirmación: ABC", Fecha = DateTime.UtcNow, Canal = CanalNotificacionEnum.CorreoBccPendiente, Destino = destino }, true) },
                new EventoAuditoria { Fecha = DateTime.UtcNow, Accion = "Test" });
            Scenario.Db.ChangeTracker.Clear();
        }

        public async Task<CanalNotificacionEnum> CanalDe(string? destino)
        {
            Scenario.Db.ChangeTracker.Clear();
            return (await Scenario.Repo.ListAsync<Notificacion>()).Single(n => n.Destino == destino).Canal;
        }

        public ValueTask DisposeAsync() => Scenario.DisposeAsync();
    }

    [Fact]
    public async Task ProcessPending_WhenSendSucceeds_MarksSentAndWritesAudit()
    {
        await using var rig = new Rig();
        await rig.Init();
        await rig.AddPending("ok@demo.local");

        var sent = await rig.Job.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(1, sent);
        Assert.Equal(1, rig.Sends);
        Assert.Equal(1, rig.Connects);
        Assert.Equal(CanalNotificacionEnum.CorreoBccEnviado, await rig.CanalDe("ok@demo.local"));
        Assert.Contains(await rig.Scenario.Repo.ListAsync<EventoAuditoria>(), a => a.Accion == "Correo BCC enviado");
        Assert.False(rig.Connected); // la conexión se cierra al terminar el lote
    }

    [Fact]
    public async Task ProcessPending_WhenNetworkFailsOnce_ReconnectsAndRetries()
    {
        await using var rig = new Rig();
        await rig.Init(fail: (_, n) => n == 1 ? new IOException("conexión perdida") : null);
        await rig.AddPending("reintento@demo.local");

        var sent = await rig.Job.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(1, sent);
        Assert.Equal(2, rig.Sends);
        Assert.Equal(2, rig.Connects); // reconexión tras el IOException
        Assert.Equal(CanalNotificacionEnum.CorreoBccEnviado, await rig.CanalDe("reintento@demo.local"));
    }

    [Fact]
    public async Task ProcessPending_WhenServerRejectsPermanently_MarksFailedWithoutRetryAndContinues()
    {
        await using var rig = new Rig();
        await rig.Init(fail: (m, _) => m.Bcc.Mailboxes.Any(b => b.Address == "malo@demo.local")
            ? new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "550 buzón inexistente")
            : null);
        await rig.AddPending("malo@demo.local");
        await rig.AddPending("bueno@demo.local");

        var sent = await rig.Job.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(1, sent);
        Assert.Equal(2, rig.Sends); // 5xx no se reintenta
        Assert.Equal(CanalNotificacionEnum.CorreoBccFallido, await rig.CanalDe("malo@demo.local"));
        Assert.Equal(CanalNotificacionEnum.CorreoBccEnviado, await rig.CanalDe("bueno@demo.local"));
    }

    [Fact]
    public async Task ProcessPending_WhenAllAttemptsFail_LeavesPendingAndDoesNotAbortTheBatch()
    {
        await using var rig = new Rig();
        await rig.Init(fail: (m, _) => m.Bcc.Mailboxes.Any(b => b.Address == "caido@demo.local") ? new IOException("timeout") : null, maxAttempts: 2);
        await rig.AddPending("caido@demo.local");
        await rig.AddPending("bueno@demo.local");

        var sent = await rig.Job.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(1, sent);
        Assert.Equal(CanalNotificacionEnum.CorreoBccPendiente, await rig.CanalDe("caido@demo.local")); // se reintentará en el próximo ciclo
        Assert.Equal(CanalNotificacionEnum.CorreoBccEnviado, await rig.CanalDe("bueno@demo.local"));
    }

    [Fact]
    public async Task ProcessPending_WhenDestinationIsInvalid_MarksFailedWithoutSending()
    {
        await using var rig = new Rig();
        await rig.Init();
        await rig.AddPending("esto no es un correo");

        var sent = await rig.Job.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(0, sent);
        Assert.Equal(0, rig.Sends);
        Assert.Equal(CanalNotificacionEnum.CorreoBccFallido, await rig.CanalDe("esto no es un correo"));
    }

    [Fact]
    public async Task ProcessPending_WhenNothingPending_DoesNotOpenAConnection()
    {
        await using var rig = new Rig();
        await rig.Init();

        var sent = await rig.Job.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(0, sent);
        Assert.Equal(0, rig.Connects);
    }
}
