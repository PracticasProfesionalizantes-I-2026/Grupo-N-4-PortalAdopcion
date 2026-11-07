namespace Portal.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PortalAdopcion.API.Middleware;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Exceptions;
using Xunit;

public class ValidationHandlingTests
{
    private static void AssertValidationStructure(JsonElement root)
    {
        Assert.True(root.TryGetProperty("status", out var statusProp), "Debe contener propiedad 'status'");
        Assert.Equal(400, statusProp.GetInt32());

        Assert.True(root.TryGetProperty("title", out var titleProp), "Debe contener propiedad 'title'");
        Assert.Equal("Datos inválidos", titleProp.GetString());

        Assert.True(root.TryGetProperty("errors", out var errorsProp), "Debe contener propiedad 'errors'");
        Assert.Equal(JsonValueKind.Object, errorsProp.ValueKind);

        // Si errors tiene más de una clave, ninguna puede ser 'cuerpo'
        if (errorsProp.EnumerateObject().Count() > 1)
        {
            Assert.False(errorsProp.TryGetProperty("cuerpo", out _), "Si hay más de un error, ninguno puede ser 'cuerpo'.");
        }

        // Verificar que ningún mensaje tenga más de 12 palabras ni palabras técnicas en inglés
        foreach (var property in errorsProp.EnumerateObject())
        {
            Assert.False(property.Name.StartsWith("$"), $"El nombre del campo '{property.Name}' no debe contener '$'");
            Assert.True(char.IsLower(property.Name[0]), $"El nombre del campo '{property.Name}' debe comenzar en minúscula (camelCase)");

            var messages = property.Value.EnumerateArray().Select(x => x.GetString()!).ToList();
            Assert.Single(messages); // Un solo mensaje por campo (el más importante)

            var msg = messages[0];
            Assert.False(string.IsNullOrWhiteSpace(msg));

            var words = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.True(words.Length <= 12, $"El mensaje '{msg}' supera las 12 palabras ({words.Length} palabras).");

            Assert.DoesNotContain("JSON", msg, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Path", msg, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("LineNumber", msg, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DTO", msg, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("convertir", msg, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("The ", msg, StringComparison.Ordinal);
            Assert.DoesNotContain("field is required", msg, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Caso1_CampoRequerido_DniFaltante_Retorna400YMensajeEnEspanol()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();

        var payload = "{\"nombre\":\"Carlos\",\"email\":\"carlos@demo.local\",\"password\":\"ClaveDemo2026!\",\"telefono\":\"12345678\",\"direccion\":\"Calle 123\",\"edad\":25,\"estadoCivil\":\"Soltero/a\",\"datosLaborales\":\"Programador\"}";
        var response = await client.PostAsync("/api/auth/registro", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        AssertValidationStructure(json);

        var errors = json.GetProperty("errors");
        Assert.True(errors.TryGetProperty("dni", out var dniErrors));
        Assert.Equal("Ingresá el DNI.", dniErrors[0].GetString());
    }

    [Fact]
    public async Task Caso2_BodyVacio_Retorna400YMensajeCuerpo()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();

        var response = await client.PostAsync("/api/auth/registro", new StringContent("", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        AssertValidationStructure(json);

        var errors = json.GetProperty("errors");
        Assert.Single(errors.EnumerateObject());
        Assert.True(errors.TryGetProperty("cuerpo", out var cuerpoErrors));
        Assert.Equal("No se recibieron datos.", cuerpoErrors[0].GetString());
    }

    [Fact]
    public async Task Caso3_CeroALaIzquierda_Retorna400YSinCerosAlInicio()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();

        var payload = "{\"nombre\":\"Carlos\",\"email\":\"carlos@demo.local\",\"dni\":030111222,\"password\":\"ClaveDemo2026!\",\"telefono\":\"12345678\",\"direccion\":\"Calle 123\",\"edad\":25,\"estadoCivil\":\"Soltero/a\",\"datosLaborales\":\"Programador\"}";
        var response = await client.PostAsync("/api/auth/registro", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        AssertValidationStructure(json);

        var errors = json.GetProperty("errors");
        Assert.False(errors.TryGetProperty("cuerpo", out _), "No debe contener 'cuerpo'");
        Assert.True(errors.TryGetProperty("dni", out var dniErrors));
        Assert.Equal("El DNI debe escribirse sin ceros al inicio.", dniErrors[0].GetString());
    }

    [Fact]
    public async Task Caso4_JsonMalFormado_ComaInesperada_Retorna400YFormatoIncorrecto()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();

        var payload = "{\"nombre\":\"Carlos\",\"email\":\"carlos@demo.local\",\"edad\": ,\"dni\":\"30111222\"}";
        var response = await client.PostAsync("/api/auth/registro", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        AssertValidationStructure(json);

        var errors = json.GetProperty("errors");
        Assert.False(errors.TryGetProperty("cuerpo", out _), "No debe contener 'cuerpo'");
        Assert.True(errors.TryGetProperty("edad", out var edadErrors));
        Assert.Equal("El campo edad tiene un formato incorrecto.", edadErrors[0].GetString());
    }

    [Fact]
    public async Task Caso5_EnumEspecieInvalido_Retorna400YElegiOpcionValida()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO("admin@demo.local", "DemoPortal2026!"));
        var token = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var payload = "{\"nombre\":\"Firulais\",\"especie\":\"Pajaro\",\"edad\":3,\"sexo\":\"Macho\"}";
        var response = await client.PostAsync("/api/mascotas", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        AssertValidationStructure(json);

        var errors = json.GetProperty("errors");
        Assert.False(errors.TryGetProperty("cuerpo", out _), "No debe contener 'cuerpo'");
        Assert.True(errors.TryGetProperty("especie", out var especieErrors));
        Assert.Equal("Elegí una opción válida para especie.", especieErrors[0].GetString());
    }

    [Fact]
    public async Task Caso6_EnumSexoInvalido_Retorna400YElegiOpcionValida()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO("admin@demo.local", "DemoPortal2026!"));
        var token = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var payload = "{\"nombre\":\"Firulais\",\"especie\":\"Perro\",\"edad\":3,\"sexo\":\"Indefinido\"}";
        var response = await client.PostAsync("/api/mascotas", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        AssertValidationStructure(json);

        var errors = json.GetProperty("errors");
        Assert.True(errors.TryGetProperty("sexo", out var sexoErrors));
        Assert.Equal("Elegí una opción válida para sexo.", sexoErrors[0].GetString());
    }

    [Fact]
    public async Task Caso7_PesoInvalido_Retorna400YMayorACero()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO("veterinario@demo.local", "DemoPortal2026!"));
        var token = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPortalRepository>();
        var pet = (await repo.ListAsync<Mascota>()).First();

        var payload = "{\"peso\":0,\"estadoGeneral\":\"Saludable\",\"vacunas\":\"Completas\",\"antecedentes\":\"Ninguno\",\"esterilizada\":true}";
        var response = await client.PostAsync($"/api/mascotas/{pet.Id}/historial", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        AssertValidationStructure(json);

        var errors = json.GetProperty("errors");
        Assert.True(errors.TryGetProperty("peso", out var pesoErrors));
        Assert.Equal("El peso debe ser mayor a 0 kg.", pesoErrors[0].GetString());
    }

    [Fact]
    public async Task CasoAdicional_DatosSinValidar_RechazaEstadoGeneralInvalidoYVacunasVacias()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO("veterinario@demo.local", "DemoPortal2026!"));
        var token = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPortalRepository>();
        var pet = (await repo.ListAsync<Mascota>()).First();

        var payload = "{\"peso\":50,\"estadoGeneral\":\"e\",\"vacunas\":\"\",\"antecedentes\":\"Ninguno\",\"esterilizada\":true}";
        var response = await client.PostAsync($"/api/mascotas/{pet.Id}/historial", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        AssertValidationStructure(json);

        var errors = json.GetProperty("errors");
        Assert.True(errors.TryGetProperty("estadoGeneral", out var estadoErrors));
        Assert.Equal("Elegí un estado general válido.", estadoErrors[0].GetString());

        Assert.True(errors.TryGetProperty("vacunas", out var vacunasErrors));
        Assert.Equal("Ingresá las vacunas.", vacunasErrors[0].GetString());
    }

    [Fact]
    public void Translator_ValidationException_MapeaCorrectamente()
    {
        var ex = new ValidationException("Peso inválido.");
        var response = ValidationErrorTranslator.FromValidationException(ex);

        Assert.Equal(400, response.Status);
        Assert.Equal("Datos inválidos", response.Title);
        Assert.True(response.Errors.ContainsKey("peso"));
        Assert.Equal("El peso debe ser mayor a 0 kg.", response.Errors["peso"][0]);
    }
}

