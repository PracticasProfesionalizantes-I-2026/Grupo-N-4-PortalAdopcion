namespace API.Integration.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PortalAdopcion.DataAccess;
using PortalAdopcion.Shared.DTOs;

public class MascotasApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static HttpClient CreateClient()
    {
        var client = new PortalAdopcionApiFactory().CreateClient();
        return client;
    }

    [Fact]
    public async Task GetMascotas_Devuelve200ConDatosSeed()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/mascotas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = (await response.Content.ReadFromJsonAsync<List<MascotaResponseDTO>>(JsonOptions))!;
        Assert.NotNull(lista);
        Assert.True(lista.Count >= 2);
    }

    [Fact]
    public async Task GetMascotaPorId_Existente_Devuelve200()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/mascotas/{DbInitializer.MascotaTomId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mascota = (await response.Content.ReadFromJsonAsync<MascotaResponseDTO>(JsonOptions))!;
        Assert.Equal("Tom", mascota.Nombre);
    }

    [Fact]
    public async Task GetMascotaPorId_Inexistente_Devuelve404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/mascotas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMascotas_PorEstado_FiltraResultados()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/mascotas?estado=Disponible");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = (await response.Content.ReadFromJsonAsync<List<MascotaResponseDTO>>(JsonOptions))!;
        Assert.All(lista, m => Assert.Equal("Disponible", m.Estado.ToString()));
    }

    [Fact]
    public async Task PostMascota_Valida_Devuelve201()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/mascotas", new
        {
            nombre = "Rex",
            especie = "Perro",
            raza = "Doberman",
            edad = 3,
            sexo = "Macho"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var mascota = (await response.Content.ReadFromJsonAsync<MascotaResponseDTO>(JsonOptions))!;
        Assert.Equal("Rex", mascota.Nombre);
        Assert.Equal("Disponible", mascota.Estado.ToString());
    }

    [Fact]
    public async Task PostMascota_NombreVacio_Devuelve400()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/mascotas", new
        {
            nombre = "  ",
            especie = "Perro",
            edad = 3,
            sexo = "Macho"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMascota_EdadInvalida_Devuelve400()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/mascotas", new
        {
            nombre = "Rex",
            especie = "Perro",
            edad = 45,
            sexo = "Macho"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMascota_EspecieInvalida_Devuelve400()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/mascotas", new
        {
            nombre = "Rex",
            especie = "Pez",
            edad = 3,
            sexo = "Macho"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutMascota_Existente_Devuelve200Actualizado()
    {
        using var client = CreateClient();

        var response = await client.PutAsJsonAsync($"/api/mascotas/{DbInitializer.MascotaTomId}", new
        {
            nombre = "Tommy",
            especie = "Gato",
            raza = "Siames",
            edad = 4,
            sexo = "Macho",
            estado = "Disponible"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mascota = (await response.Content.ReadFromJsonAsync<MascotaResponseDTO>(JsonOptions))!;
        Assert.Equal("Tommy", mascota.Nombre);
        Assert.Equal("Siames", mascota.Raza);
    }

    [Fact]
    public async Task PutMascota_Inexistente_Devuelve404()
    {
        using var client = CreateClient();

        var response = await client.PutAsJsonAsync($"/api/mascotas/{Guid.NewGuid()}", new
        {
            nombre = "Rex",
            especie = "Perro",
            edad = 3,
            sexo = "Macho",
            estado = "Disponible"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMascota_ConSolicitudAsociada_Devuelve409()
    {
        using var client = CreateClient();

        var crearSolicitud = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            mascotaId = DbInitializer.MascotaTomId,
            adoptanteId = DbInitializer.AdoptanteAnaId
        });
        Assert.Equal(HttpStatusCode.Created, crearSolicitud.StatusCode);

        var response = await client.DeleteAsync($"/api/mascotas/{DbInitializer.MascotaTomId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMascota_SinSolicitudes_Devuelve204()
    {
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/mascotas/{DbInitializer.MascotaLunaId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMascota_Inexistente_Devuelve404()
    {
        using var client = CreateClient();

        var response = await client.DeleteAsync($"/api/mascotas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}