namespace Portal.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using PortalAdopcion.API.Middleware;
using PortalAdopcion.Shared.DTOs;
using Xunit;

public class MensajesDeErrorTests
{
    [Theory]
    [InlineData("Teléfono: caracteres inválidos: 'abc'. Solo se permiten dígitos (0-9), puntos, guiones y espacios.", "telefono", "El teléfono solo admite números, puntos y guiones.")]
    [InlineData("DNI: máximo 9 dígitos permitidos.", "dni", "El DNI no puede superar los 9 dígitos.")]
    [InlineData("DNI: mínimo 8 dígitos permitidos.", "dni", "El DNI debe tener al menos 8 dígitos.")]
    [InlineData("DNI: no se permiten valores negativos.", "dni", "El DNI no puede ser negativo.")]
    [InlineData("Estado civil: valor obligatorio de hasta 50 caracteres.", "estadoCivil", "Ingresá el estado civil (hasta 50 caracteres).")]
    [InlineData("Dirección: valor obligatorio de hasta 20 caracteres.", "direccion", "Ingresá la dirección (hasta 20 caracteres).")]
    [InlineData("Estado civil inválido. Opciones: Soltero/a, Casado/a.", "estadoCivil", "Elegí un estado civil válido.")]
    [InlineData("Edad: 0 a 20 años.", "edad", "La edad debe estar entre 0 y 20 años.")]
    [InlineData("Edad del adoptante: 18 a 70 años.", "edad", "La edad debe estar entre 18 y 70 años.")]
    [InlineData("Voluntario no habilitado.", "voluntarioId", "El voluntario elegido no está habilitado.")]
    [InlineData("Peso inválido.", "peso", "El peso debe ser mayor a 0 kg.")]
    public void TranslateValidationException_DevuelveUnaOracionSimpleEnEspanol(string original, string campo, string esperado)
    {
        var (field, message) = ValidationErrorTranslator.TranslateValidationException(original);

        Assert.Equal(campo, field);
        Assert.Equal(esperado, message);
        Assert.True(message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 12, "El mensaje supera las 12 palabras.");
    }

    [Fact]
    public void TranslateMessage_CampoEstadoGeneralFaltante_ConservaSuPropioMensaje()
    {
        var message = ValidationErrorTranslator.TranslateMessage("estadoGeneral", "Ingresá el estado general.", null);

        Assert.Equal("Ingresá el estado general.", message);
    }

    [Fact]
    public async Task CrearMascota_SinEstadoGeneral_Retorna201()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();
        await IniciarSesionAdmin(client);

        var payload = "{\"nombre\":\"Loki\",\"especie\":\"Perro\",\"raza\":\"Mestizo\",\"edad\":2,\"sexo\":\"Hembra\",\"fotoUrl\":null,\"chip\":null}";
        var response = await client.PostAsync("/api/mascotas", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CrearMascota_ConEdadNegativa_DevuelveSoloElErrorDeEdad()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();
        await IniciarSesionAdmin(client);

        var payload = "{\"nombre\":\"poli\",\"especie\":\"Perro\",\"raza\":\"chihuaha\",\"edad\":-5,\"sexo\":\"Hembra\",\"fotoUrl\":null,\"chip\":null}";
        var response = await client.PostAsync("/api/mascotas", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("errors");
        Assert.Single(errors.EnumerateObject());
        Assert.Equal("La edad debe estar entre 0 y 20 años.", errors.GetProperty("edad")[0].GetString());
    }

    [Theory]
    [InlineData("desde", "The JSON value could not be converted to System.DateTime. Path: $.desde | LineNumber: 3 | BytePositionInLine: 20.", "El campo desde debe ser una fecha válida.")]
    [InlineData("edad", "The JSON value could not be converted to System.Int32. Path: $.edad | LineNumber: 5 | BytePositionInLine: 15.", "El campo edad debe ser un número.")]
    [InlineData("adoptanteId", "The JSON value could not be converted to System.Guid. Path: $.adoptanteId | LineNumber: 2 | BytePositionInLine: 30.", "El campo adoptanteId no es un identificador válido.")]
    public void TranslateMessage_ErrorDeTipo_DiceQueSeEsperaEnVezDeElegirUnaOpcion(string campo, string original, string esperado)
    {
        var message = ValidationErrorTranslator.TranslateMessage(campo, original, null);

        Assert.Equal(esperado, message);
    }

    [Theory]
    [InlineData("Especie: valor obligatorio.", "especie", "Ingresá la especie.")]
    [InlineData("Sexo: valor obligatorio.", "sexo", "Ingresá el sexo.")]
    [InlineData("Edad: valor obligatorio.", "edad", "Ingresá la edad.")]
    public void TranslateValidationException_CampoDeMascotaObligatorio_DiceQueFalta(string original, string campo, string esperado)
    {
        var (field, message) = ValidationErrorTranslator.TranslateValidationException(original);

        Assert.Equal(campo, field);
        Assert.Equal(esperado, message);
    }

    [Fact]
    public async Task CrearMascota_SinRaza_Retorna400ConMensaje()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();
        await IniciarSesionAdmin(client);

        var payload = "{\"nombre\":\"Perro\",\"especie\":\"Perro\",\"raza\":\"\",\"edad\":2,\"sexo\":\"Hembra\",\"fotoUrl\":null,\"chip\":null}";
        var response = await client.PostAsync("/api/mascotas", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("errors");
        Assert.Equal("Ingresá la raza.", errors.GetProperty("raza")[0].GetString());
    }

    [Fact]
    public async Task CrearMascota_ConVariosErrores_LosDevuelveTodosJuntos()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();
        await IniciarSesionAdmin(client);

        var payload = "{\"nombre\":\"poli\",\"especie\":\"Perro\",\"raza\":\"\",\"edad\":-5,\"fotoUrl\":null,\"chip\":null}";
        var response = await client.PostAsync("/api/mascotas", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("errors");
        Assert.Equal("Ingresá la raza.", errors.GetProperty("raza")[0].GetString());
        Assert.Equal("La edad debe estar entre 0 y 20 años.", errors.GetProperty("edad")[0].GetString());
        Assert.Equal("Ingresá el sexo.", errors.GetProperty("sexo")[0].GetString());
    }

    [Fact]
    public async Task Registro_ConVariosCamposInvalidos_LosDevuelveTodosJuntos()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();

        var payload = "{\"nombre\":\"Carlos\",\"email\":\"carlos@demo.local\",\"dni\":\"12AB34\",\"password\":\"ClaveDemo2026!\",\"telefono\":\"abc123\",\"direccion\":\"Una dirección demasiado larga\",\"edad\":25,\"estadoCivil\":\"Viudo\",\"datosLaborales\":\"Programador\"}";
        var response = await client.PostAsync("/api/auth/registro", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("dni", out _));
        Assert.Equal("El teléfono solo admite números, puntos y guiones.", errors.GetProperty("telefono")[0].GetString());
        Assert.Equal("La dirección no puede superar los 20 caracteres.", errors.GetProperty("direccion")[0].GetString());
        Assert.Equal("Elegí un estado civil válido.", errors.GetProperty("estadoCivil")[0].GetString());
    }

    private static async Task IniciarSesionAdmin(HttpClient client)
    {
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO("admin@demo.local", "DemoPortal2026!"));
        var token = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
    }
}
