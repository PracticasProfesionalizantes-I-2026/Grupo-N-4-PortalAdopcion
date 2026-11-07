namespace Portal.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Security;
using Xunit;

public class SecurityCorrectionsTests
{
    private const string DemoPassword = "DemoPortal2026!";

    // No es la clave real que estuvo expuesta: sirve para comprobar que una firma ajena es rechazada.
    private const string ClaveDemoDePrueba = "ClaveDemoDePrueba-NoEsLaClaveReal-32b";

    private sealed class JwtConfigFactory : WebApplicationFactory<global::Program>
    {
        private readonly string? previousKey;

        public JwtConfigFactory(string? key)
        {
            previousKey = Environment.GetEnvironmentVariable("Jwt__Key");
            Environment.SetEnvironmentVariable("Jwt__Key", key);
        }

        protected override void ConfigureWebHost(IWebHostBuilder b)
        {
            b.UseEnvironment("Testing");
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Demo:Seed"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
            }));
        }

        protected override void Dispose(bool disposing)
        {
            Environment.SetEnvironmentVariable("Jwt__Key", previousKey);
            base.Dispose(disposing);
        }
    }

    private static async Task<HttpClient> LoginAsync(PortalFactory factory, string email)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO(email, DemoPassword));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private static async Task<Guid> AddVolunteerAsync(PortalFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPortalRepository>();
        var user = new Usuario
        {
            Nombre = "Voluntario dos",
            Email = "voluntario2@demo.local",
            Dni = "90000099",
            Rol = "Voluntario",
            Estado = "Activa",
            PasswordHash = PasswordSecurity.Hash(DemoPassword)
        };
        await repo.SaveAsync([new Cambio(user, true)], new EventoAuditoria
        {
            UsuarioId = Guid.Empty,
            Fecha = DateTime.UtcNow,
            Accion = "Prueba",
            EntidadId = user.Id,
            Motivo = "Segundo voluntario"
        });
        return user.Id;
    }

    private static async Task<Guid> PendingRequestAsync(PortalFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPortalRepository>();
        return (await repo.ListAsync<SolicitudDeAdopcion>(x => x.Estado == EstadoSolicitudEnum.Pendiente)).First().Id;
    }

    private static async Task<Guid> TrialRequestAsync(PortalFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPortalRepository>();
        return (await repo.ListAsync<SolicitudDeAdopcion>(x => x.Estado == EstadoSolicitudEnum.EnPeriodoDePrueba)).First().Id;
    }

    private static async Task<Guid> AssignPendingRequestAsync(PortalFactory factory, Guid volunteerId)
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPortalRepository>();
        var request = (await repo.ListAsync<SolicitudDeAdopcion>(x => x.Estado == EstadoSolicitudEnum.Pendiente)).First();
        request.VoluntarioId = volunteerId;
        await repo.SaveAsync([new Cambio(request)], new EventoAuditoria
        {
            UsuarioId = Guid.Empty,
            Fecha = DateTime.UtcNow,
            Accion = "Prueba",
            EntidadId = request.Id,
            Motivo = "Asignación de prueba"
        });
        return request.Id;
    }

    [Fact]
    public void Config_SinJwtKey_NoArranca()
    {
        using var factory = new JwtConfigFactory(null);

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Jwt", ex.ToString());
    }

    [Fact]
    public void Config_JwtKeyCorta_NoArranca()
    {
        using var factory = new JwtConfigFactory("corta");

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Jwt", ex.ToString());
    }

    [Fact]
    public async Task Auth_TokenConClaveDemo_401()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        var token = new TokenIssuer(ClaveDemoDePrueba).Issue(new Usuario
        {
            Nombre = "Demo",
            Rol = "Administrador"
        }).Token;
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Requests_Veterinario_403()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        using var client = await LoginAsync(factory, "veterinario@demo.local");

        var response = await client.GetAsync("/api/solicitudes");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_VoluntarioDeOtro_403()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        var otherVolunteer = await AddVolunteerAsync(factory);
        var requestId = await AssignPendingRequestAsync(factory, otherVolunteer);
        using var client = await LoginAsync(factory, "voluntario@demo.local");

        var response = await client.GetAsync($"/api/solicitudes/{requestId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_VoluntarioNoAsignada_200()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        var requestId = await PendingRequestAsync(factory);
        using var client = await LoginAsync(factory, "voluntario@demo.local");

        var response = await client.GetAsync($"/api/solicitudes/{requestId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Staff_NoVeCodigoConfirmacion()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        using var client = await LoginAsync(factory, "admin@demo.local");

        var response = await client.GetAsync("/api/solicitudes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        foreach (var item in json.EnumerateArray())
        {
            Assert.False(
                item.TryGetProperty("codigoConfirmacion", out var code) && !string.IsNullOrWhiteSpace(code.GetString()),
                "El personal no debe recibir el código de confirmación.");
        }
    }

    [Fact]
    public async Task Request_AdoptantePropio_200()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        var requestId = await PendingRequestAsync(factory);
        using var client = await LoginAsync(factory, "adoptante2@demo.local");

        var response = await client.GetAsync($"/api/solicitudes/{requestId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_AdoptanteAjeno_403()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        var requestId = await PendingRequestAsync(factory);
        using var client = await LoginAsync(factory, "adoptante@demo.local");

        var response = await client.GetAsync($"/api/solicitudes/{requestId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_SinAsignar_Voluntario_200()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        var requestId = await PendingRequestAsync(factory);
        using var client = await LoginAsync(factory, "voluntario@demo.local");

        var response = await client.PatchAsync($"/api/solicitudes/{requestId}/evaluacion",
            new StringContent("{\"estado\":\"Aprobada\",\"entidadAutorizante\":\"ONG\"}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_AsignadoAOtro_403()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        var otherVolunteer = await AddVolunteerAsync(factory);
        var requestId = await AssignPendingRequestAsync(factory, otherVolunteer);
        using var client = await LoginAsync(factory, "voluntario@demo.local");

        var response = await client.PatchAsync($"/api/solicitudes/{requestId}/evaluacion",
            new StringContent("{\"estado\":\"EnObservacion\",\"motivo\":\"Revisar\",\"entidadAutorizante\":\"ONG\"}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_AsignadoAOtro_403()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        var otherVolunteer = await AddVolunteerAsync(factory);
        var requestId = await AssignPendingRequestAsync(factory, otherVolunteer);
        using var client = await LoginAsync(factory, "voluntario@demo.local");

        var response = await client.PostAsJsonAsync($"/api/solicitudes/{requestId}/cancelacion",
            new MotivoDTO("No corresponde"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_Admin_200()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        var requestId = await TrialRequestAsync(factory);
        using var client = await LoginAsync(factory, "admin@demo.local");

        var response = await client.PostAsJsonAsync($"/api/solicitudes/{requestId}/cancelacion",
            new MotivoDTO("Cancelación administrativa"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
