using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Security;

public static class DemoSeed
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var repo = services.GetRequiredService<IPortalRepository>();
        if ((await repo.ListAsync<Usuario>()).Count != 0)
            return;
        var now = DateTime.UtcNow;
        var changes = new List<Cambio>();
        var admin = new Usuario
        {
            Nombre = "Administración demo",
            Email = "admin@demo.local",
            Dni = "10000001",
            Rol = "Administrador",
            PasswordHash = PasswordSecurity.Hash("DemoPortal2026!")
        };
        var volunteer = new Usuario
        {
            Nombre = "Voluntario demo",
            Email = "voluntario@demo.local",
            Dni = "10000002",
            Rol = "Voluntario",
            PasswordHash = PasswordSecurity.Hash("DemoPortal2026!")
        };
        var vet = new Usuario
        {
            Nombre = "Veterinario demo",
            Email = "veterinario@demo.local",
            Dni = "10000003",
            Rol = "Veterinario",
            Matricula = "DEMO-001",
            PasswordHash = PasswordSecurity.Hash("DemoPortal2026!")
        };
        changes.AddRange(new[] { new Cambio(admin, true), new Cambio(volunteer, true), new Cambio(vet, true) });
        var names = new[]
        {
            "Luna",
            "Tom",
            "Mora",
            "Rex",
            "Nina",
            "Toby"
        };
        var pets = names.Select((n, i) => new Mascota { Nombre = n, Especie = i % 2 == 0 ? EspecieEnum.Perro : EspecieEnum.Gato, Sexo = SexoEnum.Hembra, Edad = 2, FechaRegistro = now.AddDays(-60) }).ToArray();
        foreach (var p in pets)
            changes.Add(new(p, true));
        for (var i = 0; i < 3; i++)
        {
            var profile = new Adoptante
            {
                NombreCompleto = "Adoptante demo " + (i + 1),
                Email = i == 0 ? "adoptante@demo.local" : $"adoptante{i + 1}@demo.local",
                Dni = "2000000" + (i + 1),
                Telefono = "3493000000",
                Direccion = "Domicilio de prueba",
                Edad = 25,
                EstadoCivil = "Soltero/a",
                DatosLaborales = "Datos de ejemplo"
            };
            var u = new Usuario
            {
                Nombre = profile.NombreCompleto,
                Email = profile.Email,
                Dni = profile.Dni,
                AdoptanteId = profile.Id,
                PasswordHash = PasswordSecurity.Hash("DemoPortal2026!")
            };
            changes.Add(new(profile, true));
            changes.Add(new(u, true));
            // Primera cuenta libre para postular. Otras dos permiten probar evaluación y finalización sin esperar días reales.
            if (i == 0)
                continue;
            var s = new SolicitudDeAdopcion
            {
                MascotaId = pets[i].Id,
                AdoptanteId = profile.Id,
                FechaSolicitud = now.AddDays(-40),
                UltimaActividad = now,
                Estado = i == 1 ? EstadoSolicitudEnum.Pendiente : EstadoSolicitudEnum.EnPeriodoDePrueba,
                MotivoAdopcion = "Solicitud de demostración",
                CodigoConfirmacion = "DEMO-" + (i + 1),
                InicioPrueba = i == 2 ? now.AddDays(-16) : null,
                VoluntarioId = i == 2 ? volunteer.Id : null
            };
            pets[i].Estado = i == 1 ? EstadoMascotaEnum.Reservada : EstadoMascotaEnum.EnPeriodoDePrueba;
            changes.Add(new(s, true));
            if (i == 2)
            {
                var h = new HistorialClinico
                {
                    MascotaId = pets[i].Id,
                    Fecha = now.AddDays(-50),
                    VeterinarioId = vet.Id,
                    Peso = 8,
                    EstadoGeneral = "Saludable",
                    Vacunas = "Al día",
                    Antecedentes = "Sin antecedentes"
                };
                changes.Add(new(h, true));
                changes.Add(new(new EventoMedico { HistorialClinicoId = h.Id, VeterinarioId = vet.Id, Fecha = h.Fecha, Tipo = "Inicial", Descripcion = "Revisión de ingreso", Tratamiento = "Ninguno" }, true));
                changes.Add(new(new Entrevista { SolicitudId = s.Id, VoluntarioId = volunteer.Id, FechaProgramada = now.AddDays(-17), FechaRealizada = now.AddDays(-16), Resultado = "Favorable", Observaciones = "Demo" }, true));
            }
        }

        await repo.SaveAsync(changes, new EventoAuditoria { UsuarioId = admin.Id, Fecha = now, Accion = "Datos demo", Motivo = "Ejemplos sintéticos; no corresponden a personas reales" });
    }
}
