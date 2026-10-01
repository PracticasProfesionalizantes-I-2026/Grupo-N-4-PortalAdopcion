namespace API.Integration.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortalAdopcion.DataAccess;

public class PortalAdopcionApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public PortalAdopcionApiFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<PortalAdopcionDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<PortalAdopcionDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}