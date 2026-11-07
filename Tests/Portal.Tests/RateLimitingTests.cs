namespace Portal.Tests;

using System.Net;
using System.Net.Http.Json;
using PortalAdopcion.Shared.DTOs;
using Xunit;

public class RateLimitingTests
{
    [Fact]
    public async Task Login_ExcedeLimite_429()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var allowed = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO("admin@demo.local", "Incorrecta2026!"));
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var limited = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO("admin@demo.local", "Incorrecta2026!"));

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Registro_ExcedeLimite_429()
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var allowed = await client.PostAsync("/api/auth/registro",
                new StringContent("", System.Text.Encoding.UTF8, "application/json"));
            Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
        }

        var limited = await client.PostAsync("/api/auth/registro",
            new StringContent("", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task RateLimit_NoAfectaCatalogo()
    {
        using var factory = new PortalFactory();
        await factory.Seed();
        using var client = factory.CreateClient();

        for (var i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("/api/mascotas");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
