namespace API.Integration.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PortalAdopcion.DataAccess;
using PortalAdopcion.Shared.DTOs;

public class SolicitudesApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static HttpClient CreateClient() => new PortalAdopcionApiFactory().CreateClient();

    private static async Task<Guid> CrearSolicitud(HttpClient client, Guid mascotaId, Guid adoptanteId)
    {
        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            mascotaId,
            adoptanteId
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var solicitud = (await response.Content.ReadFromJsonAsync<SolicitudResponseDTO>(JsonOptions))!;
        return solicitud.Id;
    }

    [Fact]
    public async Task GetSolicitudes_Devuelve200()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/solicitudes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSolicitudPorId_Inexistente_Devuelve404()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/api/solicitudes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostSolicitud_Valida_Devuelve201Pendiente()
    {
        using var client = CreateClient();

        var id = await CrearSolicitud(client, DbInitializer.MascotaTomId, DbInitializer.AdoptanteAnaId);

        var response = await client.GetAsync($"/api/solicitudes/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var solicitud = (await response.Content.ReadFromJsonAsync<SolicitudResponseDTO>(JsonOptions))!;
        Assert.Equal("Pendiente", solicitud.Estado.ToString());
        Assert.Equal(DbInitializer.MascotaTomId, solicitud.MascotaId);
        Assert.Equal(DbInitializer.AdoptanteAnaId, solicitud.AdoptanteId);
    }

    [Fact]
    public async Task PostSolicitud_MascotaInexistente_Devuelve404()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            mascotaId = Guid.NewGuid(),
            adoptanteId = DbInitializer.AdoptanteAnaId
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostSolicitud_AdoptanteInexistente_Devuelve404()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            mascotaId = DbInitializer.MascotaTomId,
            adoptanteId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostSolicitud_MascotaNoDisponible_Devuelve409()
    {
        using var client = CreateClient();
        await CrearSolicitud(client, DbInitializer.MascotaTomId, DbInitializer.AdoptanteAnaId);

        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            mascotaId = DbInitializer.MascotaTomId,
            adoptanteId = DbInitializer.AdoptanteJuanId
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostSolicitud_MascotaYaTieneActiva_Devuelve409()
    {
        using var client = CreateClient();
        await CrearSolicitud(client, DbInitializer.MascotaTomId, DbInitializer.AdoptanteAnaId);

        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            mascotaId = DbInitializer.MascotaTomId,
            adoptanteId = DbInitializer.AdoptanteJuanId
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostSolicitud_AdoptanteConActiva_Devuelve409()
    {
        using var client = CreateClient();
        await CrearSolicitud(client, DbInitializer.MascotaTomId, DbInitializer.AdoptanteAnaId);

        var response = await client.PostAsJsonAsync("/api/solicitudes", new
        {
            mascotaId = DbInitializer.MascotaLunaId,
            adoptanteId = DbInitializer.AdoptanteAnaId
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PatchEstado_Aprobar_Devuelve200YAdoptaMascota()
    {
        using var client = CreateClient();
        var solicitudId = await CrearSolicitud(client, DbInitializer.MascotaTomId, DbInitializer.AdoptanteAnaId);

        var response = await client.PatchAsJsonAsync($"/api/solicitudes/{solicitudId}/estado", new
        {
            estado = "Aprobada"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var solicitud = (await response.Content.ReadFromJsonAsync<SolicitudResponseDTO>(JsonOptions))!;
        Assert.Equal("Aprobada", solicitud.Estado.ToString());

        var mascotaResponse = await client.GetAsync($"/api/mascotas/{DbInitializer.MascotaTomId}");
        var mascota = (await mascotaResponse.Content.ReadFromJsonAsync<MascotaResponseDTO>(JsonOptions))!;
        Assert.Equal("Adoptada", mascota.Estado.ToString());
    }

    [Fact]
    public async Task PatchEstado_Rechazar_SinMotivo_Devuelve400()
    {
        using var client = CreateClient();
        var solicitudId = await CrearSolicitud(client, DbInitializer.MascotaTomId, DbInitializer.AdoptanteAnaId);

        var response = await client.PatchAsJsonAsync($"/api/solicitudes/{solicitudId}/estado", new
        {
            estado = "Rechazada"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchEstado_Rechazar_ConMotivo_Devuelve200YMascotaDisponible()
    {
        using var client = CreateClient();
        var solicitudId = await CrearSolicitud(client, DbInitializer.MascotaTomId, DbInitializer.AdoptanteAnaId);

        var response = await client.PatchAsJsonAsync($"/api/solicitudes/{solicitudId}/estado", new
        {
            estado = "Rechazada",
            motivo = "El adoptante no cumplimenta los requisitos"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var mascotaResponse = await client.GetAsync($"/api/mascotas/{DbInitializer.MascotaTomId}");
        var mascota = (await mascotaResponse.Content.ReadFromJsonAsync<MascotaResponseDTO>(JsonOptions))!;
        Assert.Equal("Disponible", mascota.Estado.ToString());
    }

    [Fact]
    public async Task DeleteSolicitud_SinMotivo_Devuelve400()
    {
        using var client = CreateClient();
        var solicitudId = await CrearSolicitud(client, DbInitializer.MascotaTomId, DbInitializer.AdoptanteAnaId);

        var response = await client.DeleteAsync($"/api/solicitudes/{solicitudId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSolicitud_ConMotivo_Devuelve204YMascotaDisponible()
    {
        using var client = CreateClient();
        var solicitudId = await CrearSolicitud(client, DbInitializer.MascotaTomId, DbInitializer.AdoptanteAnaId);

        var response = await client.DeleteAsync($"/api/solicitudes/{solicitudId}?motivo=Desisti%20de%20la%20adopcion");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var mascotaResponse = await client.GetAsync($"/api/mascotas/{DbInitializer.MascotaTomId}");
        var mascota = (await mascotaResponse.Content.ReadFromJsonAsync<MascotaResponseDTO>(JsonOptions))!;
        Assert.Equal("Disponible", mascota.Estado.ToString());
    }

    [Fact]
    public async Task GetSolicitudes_DespuesDeCrear_DevuelveListaConNueva()
    {
        using var client = CreateClient();
        var solicitudId = await CrearSolicitud(client, DbInitializer.MascotaTomId, DbInitializer.AdoptanteAnaId);

        var response = await client.GetAsync("/api/solicitudes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lista = (await response.Content.ReadFromJsonAsync<List<SolicitudResponseDTO>>(JsonOptions))!;
        Assert.Contains(lista, s => s.Id == solicitudId);
    }
}