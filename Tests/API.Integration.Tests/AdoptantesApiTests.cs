namespace API.Integration.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PortalAdopcion.DataAccess;
using PortalAdopcion.Shared.DTOs;

public class AdoptantesApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static HttpClient CreateClient() => new PortalAdopcionApiFactory().CreateClient();

    [Fact]
    public async Task GetAdoptantes_Devuelve200ConDatosSeed()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/adoptantes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = (await response.Content.ReadFromJsonAsync<List<AdoptanteResponseDTO>>(JsonOptions))!;
        Assert.NotNull(lista);
        Assert.True(lista.Count >= 2);
    }

    [Fact]
    public async Task GetAdoptantePorId_Inexistente_Devuelve404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/adoptantes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostAdoptante_Valido_Devuelve201()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/adoptantes", new
        {
            nombreCompleto = "Maria Lopez",
            email = "maria.lopez@mail.com",
            dni = "40111222",
            telefono = "1122334455",
            direccion = "Calle 1 234"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var adoptante = (await response.Content.ReadFromJsonAsync<AdoptanteResponseDTO>(JsonOptions))!;
        Assert.Equal("maria.lopez@mail.com", adoptante.Email);
    }

    [Fact]
    public async Task PostAdoptante_EmailDuplicado_Devuelve409()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/adoptantes", new
        {
            nombreCompleto = "Clon Garcia",
            email = "ana.garcia@mail.com",
            dni = "50999111",
            telefono = "1199887766",
            direccion = "Av. Siempre Viva 742"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostAdoptante_DniDuplicado_Devuelve409()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/adoptantes", new
        {
            nombreCompleto = "Clon Garcia",
            email = "otro.clon@mail.com",
            dni = "30111222",
            telefono = "1199887766",
            direccion = "Av. Siempre Viva 742"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostAdoptante_EmailInvalido_Devuelve400()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/adoptantes", new
        {
            nombreCompleto = "Maria Lopez",
            email = "no-es-un-email",
            dni = "40111222",
            telefono = "1122334455",
            direccion = "Calle 1 234"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAdoptante_EmailYaUsadoPorOtro_Devuelve409()
    {
        using var client = CreateClient();

        var response = await client.PutAsJsonAsync($"/api/adoptantes/{DbInitializer.AdoptanteAnaId}", new
        {
            nombreCompleto = "Ana Garcia",
            email = "juan.perez@mail.com",
            dni = "30111222",
            telefono = "1144556677",
            direccion = "Av. Corrientes 1234"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PutAdoptante_Inexistente_Devuelve404()
    {
        using var client = CreateClient();

        var response = await client.PutAsJsonAsync($"/api/adoptantes/{Guid.NewGuid()}", new
        {
            nombreCompleto = "Maria Lopez",
            email = "maria.lopez@mail.com",
            dni = "40111222",
            telefono = "1122334455",
            direccion = "Calle 1 234"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAdoptante_ConSolicitudAsociada_Devuelve409()
    {
        using var client = CreateClient();

        var crearSolicitud = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            mascotaId = DbInitializer.MascotaTomId,
            adoptanteId = DbInitializer.AdoptanteAnaId
        });
        Assert.Equal(HttpStatusCode.Created, crearSolicitud.StatusCode);

        var response = await client.DeleteAsync($"/api/adoptantes/{DbInitializer.AdoptanteAnaId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}