namespace PortalAdopcion.DataAccess;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.Shared.Enums;

public static class DbInitializer
{
    public static readonly Guid MascotaTomId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid MascotaLunaId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid AdoptanteAnaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid AdoptanteJuanId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortalAdopcionDbContext>();

        db.Database.Migrate();

        if (db.Mascotas.Any() || db.Adoptantes.Any())
        {
            return;
        }

        db.Mascotas.AddRange(new[]
        {
            new Mascota
            {
                Id = MascotaTomId,
                Nombre = "Tom",
                Especie = EspecieEnum.Gato,
                Raza = "Comun Europeo",
                Edad = 2,
                Sexo = SexoEnum.Macho,
                Estado = EstadoMascotaEnum.Disponible,
                FechaRegistro = DateTime.UtcNow.AddDays(-30)
            },
            new Mascota
            {
                Id = MascotaLunaId,
                Nombre = "Luna",
                Especie = EspecieEnum.Perro,
                Raza = "Labrador",
                Edad = 1,
                Sexo = SexoEnum.Hembra,
                Estado = EstadoMascotaEnum.Disponible,
                FechaRegistro = DateTime.UtcNow.AddDays(-15)
            }
        });

        db.Adoptantes.AddRange(new[]
        {
            new Adoptante
            {
                Id = AdoptanteAnaId,
                NombreCompleto = "Ana Garcia",
                Email = "ana.garcia@mail.com",
                Dni = "30111222",
                Telefono = "1144556677",
                Direccion = "Av. Corrientes 1234",
                FechaRegistro = DateTime.UtcNow.AddDays(-20)
            },
            new Adoptante
            {
                Id = AdoptanteJuanId,
                NombreCompleto = "Juan Perez",
                Email = "juan.perez@mail.com",
                Dni = "33222333",
                Telefono = "1177889900",
                Direccion = "Calle Falsa 456",
                FechaRegistro = DateTime.UtcNow.AddDays(-10)
            }
        });

        db.SaveChanges();
    }
}