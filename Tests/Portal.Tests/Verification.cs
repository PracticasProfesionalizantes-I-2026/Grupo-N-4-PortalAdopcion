namespace Portal.Tests;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortalAdopcion.BusinessLogic.Services;
using PortalAdopcion.DataAccess;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;
using PortalAdopcion.Shared.Security;
using Xunit;

public class FakeClock(DateTime now) : TimeProvider
{
    public DateTime Now = now;
    public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
}

public class Scenario : IAsyncDisposable
{
    public readonly SqliteConnection Connection = new("Data Source=:memory:");
    public PortalAdopcionDbContext Db = null!;
    public PortalRepository Repo = null!;
    public PortalService Service = null!;
    public FakeClock Clock = new(DateTime.UtcNow);
    public Actor Admin = null!, Volunteer = null!, Vet = null!, Adopter = null!;
    public Mascota Free = null!;
    public SolicitudDeAdopcion Mature = null!, Trial = null!;
    public async Task Initialize()
    {
        Connection.Open();
        Db = new(new DbContextOptionsBuilder<PortalAdopcionDbContext>().UseSqlite(Connection).Options);
        await Db.Database.MigrateAsync();
        Repo = new(Db);
        Service = new(Repo, Clock);
        var services = new ServiceCollection().AddSingleton<IPortalRepository>(Repo).BuildServiceProvider();
        await DemoSeed.InitializeAsync(services);
        Db.ChangeTracker.Clear();
        var us = await Repo.ListAsync<Usuario>();
        Actor ActorOf(string mail)
        {
            var u = us.Single(x => x.Email == mail);
            return new(u.Id, u.Rol, u.AdoptanteId);
        }

        Admin = ActorOf("admin@demo.local");
        Volunteer = ActorOf("voluntario@demo.local");
        Vet = ActorOf("veterinario@demo.local");
        Adopter = ActorOf("adoptante@demo.local");
        Free = (await Repo.ListAsync<Mascota>()).First(x => x.Estado == EstadoMascotaEnum.Disponible);
        var req = await Repo.ListAsync<SolicitudDeAdopcion>();
        Mature = req.Single(x => x.Estado == EstadoSolicitudEnum.Pendiente);
        Trial = req.Single(x => x.Estado == EstadoSolicitudEnum.EnPeriodoDePrueba);
    }

    public async Task<SolicitudResponseDTO> Postulate()
    {
        var result = await Service.PostulateAsync(Adopter, new(Free.Id, "Dar un hogar", null));
        Db.ChangeTracker.Clear();
        return result;
    }

    public async Task Throws<T>(Func<Task> act)
        where T : Exception
    {
        try
        {
            await act();
        }
        catch (T)
        {
            return;
        }

        throw new Exception("No se produjo " + typeof(T).Name);
    }

    public void Check(bool condition, string why)
    {
        if (!condition)
            throw new Exception(why);
    }

    public ValueTask DisposeAsync()
    {
        Db.Dispose();
        Connection.Dispose();
        return ValueTask.CompletedTask;
    }
}

public class PortalFactory : WebApplicationFactory<global::Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public PortalFactory()
    {
        connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder b)
    {
        b.UseEnvironment("Testing");
        b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { { "Jwt:Key", "Testing-PortalAdopcion-Secret-AtLeast32Bytes" }, { "Demo:Seed", "false" } }));
        b.ConfigureServices(services =>
        {
            var d = services.Where(x => x.ServiceType == typeof(DbContextOptions<PortalAdopcionDbContext>) || x.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true).ToList();
            foreach (var x in d)
                services.Remove(x);
            services.AddDbContext<PortalAdopcionDbContext>(o => o.UseSqlite(connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        connection.Dispose();
    }

    public async Task Seed()
    {
        using var scope = Services.CreateScope();
        await DemoSeed.InitializeAsync(scope.ServiceProvider);
    }
}

public static class Cases
{
    public static readonly string[] Names = ["PasswordHash", "RegistroNormalizado", "RegistroDuplicado", "LoginIncorrecto", "PermisosCatalogo", "EnumInvalido", "Reserva", "DuplicadoActivo", "CancelarLibera", "AprobacionPrematura", "AprobacionNoAdopta", "Observacion", "EntrevistaPrueba", "SeguimientoDiarioUnico", "AlertaBloquea", "ValidacionFinalizacion", "BlacklistBloquea", "BlacklistRechazaActivo", "SancionEnPrueba", "HistorialUnico", "HistorialPrivado", "TransferenciaHistorial", "AbandonoIdempotente", "ConcurrenciaVersion", "IndiceReserva", "ReporteVacio", "PdfValido", "CsvSeguro", "HttpAnonimo", "HttpLoginPermisos", "HttpIdentidad", "HttpContratoEstricto", "HttpRegistroSeguro", "PersonalMatricula", "UltimoAdministrador", "EstadoInvalidaToken", "EventoMedicoPersistido", "IncidenteUrgente", "AuditoriaCambio", "CambioMascotaActivo", "CancelacionAjena", "LimiteVoluntario", "DniNoNumericoPersistencia", "EstadoCivilFueraDeListaPersistencia"];
    public static async Task Run(string name)
    {
        if (name.StartsWith("Http"))
        {
            await Http(name);
            return;
        }

        await using var t = new Scenario();
        await t.Initialize();
        var s = t.Service;
        switch (name)
        {
            case "PasswordHash":
                var hash = PasswordSecurity.Hash("DemoPortal2026!");
                t.Check(hash != "DemoPortal2026!" && PasswordSecurity.Verify("DemoPortal2026!", hash) && !PasswordSecurity.Verify("mala", hash), "Hash incorrecto");
                break;
            case "RegistroNormalizado":
                await s.RegisterAsync(new("Nuevo", "  nuevo@example.com  ", "30000001", "ClaveNueva2026!", "3493000", "Casa", 21, "Soltero/a", "Estudiante"));
                t.Db.ChangeTracker.Clear();
                t.Check((await t.Repo.ListAsync<Usuario>()).Any(x => x.Email == "nuevo@example.com" && x.Rol == "Adoptante"), "No normaliza");
                break;
            case "RegistroDuplicado":
                await t.Throws<ConflictException>(() => s.RegisterAsync(new("Duplicado", " ADMIN@demo.local ", "10000001", "ClaveNueva2026!", "3493", "Casa", 21, "Soltero/a", "Estudiante")));
                break;
            case "LoginIncorrecto":
                await t.Throws<AuthenticationException>(() => s.LoginAsync(new("admin@demo.local", "Incorrecta2026!")));
                break;
            case "PermisosCatalogo":
                await t.Throws<AccessException>(() => s.CreatePetAsync(t.Adopter, new("Pepe", EspecieEnum.Perro, null, 1, SexoEnum.Macho, null, null)));
                break;
            case "EnumInvalido":
                await t.Throws<ValidationException>(() => s.CreatePetAsync(t.Admin, new("Pepe", (EspecieEnum)45, null, 1, SexoEnum.Macho, null, null)));
                break;
            case "Reserva":
                var post = await t.Postulate();
                t.Check(post.Estado == EstadoSolicitudEnum.Pendiente && (await t.Repo.FindAsync<Mascota>(t.Free.Id))!.Estado == EstadoMascotaEnum.Reservada, "No reservó");
                t.Check((await t.Repo.ListAsync<Notificacion>()).Any(n => n.Canal == "CorreoBccPendiente" && n.EntidadId == post.Id), "Sin confirmación en bandeja");
                break;
            case "DuplicadoActivo":
                await t.Postulate();
                await t.Throws<ConflictException>(() => s.PostulateAsync(t.Adopter, new(t.Free.Id, "Otro", null)));
                break;
            case "CancelarLibera":
                var c = await t.Postulate();
                await s.CancelAsync(t.Adopter, c.Id, "Cambio de decisión");
                t.Db.ChangeTracker.Clear();
                t.Check((await t.Repo.FindAsync<Mascota>(t.Free.Id))!.Estado == EstadoMascotaEnum.Disponible, "No libera reserva");
                break;
            case "AprobacionPrematura":
                var p = await t.Postulate();
                await t.Throws<ConflictException>(() => s.EvaluateAsync(t.Admin, p.Id, new(EstadoSolicitudEnum.Aprobada, null, "ONG")));
                break;
            case "AprobacionNoAdopta":
                await s.EvaluateAsync(t.Admin, t.Mature.Id, new(EstadoSolicitudEnum.Aprobada, null, "ONG"));
                t.Db.ChangeTracker.Clear();
                t.Check((await t.Repo.FindAsync<Mascota>(t.Mature.MascotaId))!.Estado == EstadoMascotaEnum.Reservada, "Adopción prematura");
                break;
            case "Observacion":
                await s.EvaluateAsync(t.Admin, t.Mature.Id, new(EstadoSolicitudEnum.EnObservacion, "Más información", "ONG"));
                t.Db.ChangeTracker.Clear();
                await s.EvaluateAsync(t.Admin, t.Mature.Id, new(EstadoSolicitudEnum.Aprobada, null, "ONG"));
                break;
            case "EntrevistaPrueba":
                await s.EvaluateAsync(t.Admin, t.Mature.Id, new(EstadoSolicitudEnum.Aprobada, null, "ONG"));
                t.Db.ChangeTracker.Clear();
                await s.ScheduleAsync(t.Admin, t.Mature.Id, new(t.Clock.Now.AddMinutes(1), t.Volunteer.Id));
                t.Db.ChangeTracker.Clear();
                t.Clock.Now = t.Clock.Now.AddMinutes(2);
                var r = await s.InterviewResultAsync(t.Volunteer, t.Mature.Id, new("Favorable", "Bien"));
                t.Check(r.Estado == EstadoSolicitudEnum.EnPeriodoDePrueba, "No inicia prueba");
                break;
            case "SeguimientoDiarioUnico":
                await s.FollowupAsync(t.Volunteer, t.Trial.Id, new("Bien", "Bien", "Paseo", "Ninguna", "Bien", false));
                t.Db.ChangeTracker.Clear();
                await t.Throws<ConflictException>(() => s.FollowupAsync(t.Volunteer, t.Trial.Id, new("Bien", "Bien", "Paseo", "Ninguna", "Bien", false)));
                break;
            case "AlertaBloquea":
                await s.FollowupAsync(t.Volunteer, t.Trial.Id, new("Mal", "Poco", "Control", "Ninguna", "Bien", true));
                t.Db.ChangeTracker.Clear();
                await t.Throws<ConflictException>(() => s.FinalizeAsync(t.Volunteer, t.Trial.Id, "Finalizar"));
                break;
            case "ValidacionFinalizacion":
                await s.FollowupAsync(t.Volunteer, t.Trial.Id, new("Mal", "Poco", "Control", "Ninguna", "Bien", true));
                t.Db.ChangeTracker.Clear();
                await s.ValidateClinicalAsync(t.Vet, t.Trial.Id, "Apto");
                t.Db.ChangeTracker.Clear();
                var f = await s.FinalizeAsync(t.Volunteer, t.Trial.Id, "Éxito");
                t.Check(f.Estado == EstadoSolicitudEnum.Adoptada, "No finaliza");
                break;
            case "BlacklistBloquea":
                await s.SaveBlacklistAsync(t.Admin, null, new(t.Adopter.AdoptanteId!.Value, "Motivo", t.Clock.Now.AddMinutes(-1), t.Clock.Now.AddYears(1)));
                t.Db.ChangeTracker.Clear();
                await t.Throws<AccessException>(() => s.PostulateAsync(t.Adopter, new(t.Free.Id, "Casa", null)));
                break;
            case "BlacklistRechazaActivo":
                var x = await t.Postulate();
                await s.SaveBlacklistAsync(t.Admin, null, new(t.Adopter.AdoptanteId!.Value, "Motivo", t.Clock.Now.AddMinutes(-1), t.Clock.Now.AddYears(1)));
                t.Db.ChangeTracker.Clear();
                t.Check((await t.Repo.FindAsync<SolicitudDeAdopcion>(x.Id))!.Estado == EstadoSolicitudEnum.Rechazada, "Sigue activo");
                break;
            case "SancionEnPrueba":
                await s.CancelAsync(t.Admin, t.Trial.Id, "No continuar");
                t.Db.ChangeTracker.Clear();
                var sanction = (await t.Repo.ListAsync<SancionBlacklist>()).Single();
                t.Check(sanction.Hasta == sanction.Desde.AddYears(1), "Duración incorrecta");
                break;
            case "HistorialUnico":
                await s.CreateHistoryAsync(t.Vet, t.Free.Id, new(5, "Bien", "Al día", "Sin antecedentes", true));
                t.Db.ChangeTracker.Clear();
                await t.Throws<ConflictException>(() => s.CreateHistoryAsync(t.Vet, t.Free.Id, new(5, "Bien", "Al día", "Sin antecedentes", true)));
                break;
            case "HistorialPrivado":
                await t.Throws<AccessException>(() => s.HistoryAsync(t.Adopter, t.Trial.MascotaId));
                break;
            case "TransferenciaHistorial":
                await s.FinalizeAsync(t.Volunteer, t.Trial.Id, "Éxito");
                t.Db.ChangeTracker.Clear();
                var u = (await t.Repo.ListAsync<Usuario>()).Single(x => x.AdoptanteId == t.Trial.AdoptanteId);
                await s.HistoryAsync(new(u.Id, u.Rol, u.AdoptanteId), t.Trial.MascotaId);
                t.Check(!(await t.Repo.ListAsync<HistorialClinico>()).Any(x => x.MascotaId == t.Trial.MascotaId), "No desvinculó el historial del animal");
                t.Check((await t.Repo.ListAsync<HistorialClinico>()).Any(x => x.OrigenMascotaId == t.Trial.MascotaId && x.AdoptanteId == t.Trial.AdoptanteId), "No clonó el historial al adoptante");
                break;
            case "AbandonoIdempotente":
                t.Mature.Estado = EstadoSolicitudEnum.Aprobada;
                await t.Repo.SaveAsync([new(t.Mature)], new() { Accion = "Preparar" });
                t.Db.ChangeTracker.Clear();
                await s.ScheduleAsync(t.Admin, t.Mature.Id, new(t.Clock.Now.AddMinutes(1), t.Volunteer.Id));
                t.Db.ChangeTracker.Clear();
                t.Clock.Now = t.Clock.Now.AddDays(7);
                t.Check(await s.AbandonAsync() == 0, "Abandona exactamente a 7 días");
                t.Clock.Now = t.Clock.Now.AddSeconds(1);
                t.Check(await s.AbandonAsync() == 1, "No abandona");
                t.Db.ChangeTracker.Clear();
                t.Check(await s.AbandonAsync() == 0, "No es idempotente");
                break;
            case "ConcurrenciaVersion":
                var a = await t.Repo.FindAsync<Mascota>(t.Free.Id);
                var b = await t.Repo.FindAsync<Mascota>(t.Free.Id);
                a!.Nombre = "Cambio A";
                await t.Repo.SaveAsync([new(a)], new() { Accion = "Cambio A" });
                t.Db.ChangeTracker.Clear();
                b!.Nombre = "Cambio B";
                await t.Throws<ConflictException>(() => t.Repo.SaveAsync([new(b)], new() { Accion = "Cambio B" }));
                break;
            case "IndiceReserva":
                var one = await t.Postulate();
                var other = new SolicitudDeAdopcion
                {
                    MascotaId = one.MascotaId,
                    AdoptanteId = t.Mature.AdoptanteId,
                    Estado = EstadoSolicitudEnum.Pendiente,
                    FechaSolicitud = t.Clock.Now
                };
                await t.Throws<ConflictException>(() => t.Repo.SaveAsync([new(other, true)], new() { Accion = "Duplicado" }));
                break;
            case "ReporteVacio":
                var rows = await s.ReportAsync(t.Admin, "adopciones", t.Clock.Now.AddYears(-1), t.Clock.Now);
                t.Check(rows.Count == 1, "Reporte vacío inválido");
                break;
            case "PdfValido":
                var pdf = ReportFiles.Pdf([["Columna"], ["Texto (prueba)"]]);
                t.Check(Encoding.Latin1.GetString(pdf).StartsWith("%PDF-1.4") && Encoding.Latin1.GetString(pdf).Contains("startxref"), "PDF inválido");
                break;
            case "PersonalMatricula":
                await s.CreateStaffAsync(t.Admin, new("Nuevo vet", "vet2@demo.local", "90000001", "ClavePrueba2026!", "Veterinario", "DEMO-002"));
                t.Db.ChangeTracker.Clear();
                await t.Throws<ConflictException>(() => s.CreateStaffAsync(t.Admin, new("Otro", "vet3@demo.local", "90000002", "ClavePrueba2026!", "Veterinario", "DEMO-002")));
                break;
            case "UltimoAdministrador":
                await t.Throws<ConflictException>(() => s.ChangeUserAsync(t.Admin, t.Admin.Id, new("Voluntario", "Activa", null)));
                break;
            case "EstadoInvalidaToken":
                await s.ChangeUserAsync(t.Admin, t.Volunteer.Id, new("Voluntario", "Suspendida", null));
                t.Db.ChangeTracker.Clear();
                t.Check((await t.Repo.FindAsync<Usuario>(t.Volunteer.Id))!.Version == 1, "No cambia versión");
                await t.Throws<AuthenticationException>(() => s.LoginAsync(new("voluntario@demo.local", "DemoPortal2026!")));
                break;
            case "EventoMedicoPersistido":
                await s.MedicalEventAsync(t.Vet, t.Trial.MascotaId, new("Vacunación", "Vacuna aplicada", "Control posterior"));
                t.Db.ChangeTracker.Clear();
                t.Check((await t.Repo.ListAsync<EventoMedico>()).Any(e => e.Tipo == "Vacunación" && e.VeterinarioId == t.Vet.Id), "Evento no persistido");
                break;
            case "IncidenteUrgente":
                await s.IncidentAsync(t.Volunteer, new(t.Trial.Id, "Urgente", "Consulta médica"));
                t.Db.ChangeTracker.Clear();
                t.Check((await t.Repo.ListAsync<Incidente>()).Single().Gravedad == "Urgente" && (await t.Repo.ListAsync<Notificacion>()).Any(n => n.Rol == "Administrador" && n.Mensaje.Contains("Urgente")), "Sin prioridad/notificación");
                break;
            case "AuditoriaCambio":
                await s.CreatePetAsync(t.Admin, new("Prueba auditada", EspecieEnum.Perro, null, 2, SexoEnum.Macho, null, null));
                t.Db.ChangeTracker.Clear();
                var events = await s.AuditAsync(t.Admin, t.Clock.Now.AddMinutes(-1), t.Clock.Now.AddMinutes(1), "CU-09", t.Admin.Id);
                t.Check(events.Count == 1 && events[0].Accion == "CU-09 Alta", "Filtro/auditoría incorrecto");
                await t.Throws<AccessException>(() => s.AuditAsync(t.Adopter, null, null, null, null));
                break;
            case "CambioMascotaActivo":
                await t.Throws<ConflictException>(() => s.ChangePetAsync(t.Admin, t.Mature.MascotaId, new(EstadoMascotaEnum.Disponible, "Intento inválido")));
                break;
            case "CancelacionAjena":
                await t.Throws<AccessException>(() => s.CancelAsync(t.Adopter, t.Mature.Id, "Otro perfil"));
                break;
            case "LimiteVoluntario":
                await s.EvaluateAsync(t.Admin, t.Mature.Id, new(EstadoSolicitudEnum.Aprobada, null, "ONG"));
                t.Db.ChangeTracker.Clear();
                await s.ScheduleAsync(t.Admin, t.Mature.Id, new(t.Clock.Now.AddMinutes(1), t.Volunteer.Id));
                t.Db.ChangeTracker.Clear();
                var extra = await t.Postulate();
                t.Clock.Now = t.Clock.Now.AddDays(16);
                await s.EvaluateAsync(t.Admin, extra.Id, new(EstadoSolicitudEnum.Aprobada, null, "ONG"));
                t.Db.ChangeTracker.Clear();
                await t.Throws<ConflictException>(() => s.ScheduleAsync(t.Admin, extra.Id, new(t.Clock.Now.AddMinutes(1), t.Volunteer.Id)));
                break;
            case "CsvSeguro":
                t.Check(ReportFiles.Csv("=1+1").StartsWith("\"'="), "Fórmula sin neutralizar");
                break;
            case "DniNoNumericoPersistencia":
                await t.Throws<ConflictException>(() => t.Repo.SaveAsync([new(new Adoptante { NombreCompleto = "Prueba DNI", Email = "dni.check@demo.local", Dni = "12AB34", Telefono = "3493000000", Direccion = "Casa", Edad = 30, EstadoCivil = "Soltero/a", DatosLaborales = "Empleado" }, true)], new() { Accion = "Prueba CHECK DNI" }));
                break;
            case "EstadoCivilFueraDeListaPersistencia":
                await t.Throws<ConflictException>(() => t.Repo.SaveAsync([new(new Adoptante { NombreCompleto = "Prueba EC", Email = "ec.check@demo.local", Dni = "30111222", Telefono = "3493000000", Direccion = "Casa", Edad = 30, EstadoCivil = "Viudo", DatosLaborales = "Empleado" }, true)], new() { Accion = "Prueba CHECK EstadoCivil" }));
                break;
            default:
                throw new Exception("Caso desconocido");
        }
    }

    private static async Task Http(string name)
    {
        using var factory = new PortalFactory();
        using var client = factory.CreateClient();
        await factory.Seed();
        void Check(bool v, string message)
        {
            if (!v)
                throw new Exception(message);
        }

        if (name == "HttpAnonimo")
        {
            Check((await client.GetAsync("/api/mascotas")).StatusCode == HttpStatusCode.OK, "Catálogo público");
            Check((await client.GetAsync("/api/usuarios")).StatusCode == HttpStatusCode.Unauthorized, "Falta 401");
            return;
        }

        if (name == "HttpRegistroSeguro")
        {
            var content = new StringContent("{\"nombre\":\"Nuevo\",\"email\":\"nuevo@demo.local\",\"dni\":\"88888888\",\"password\":\"ClaveDemo2026!\",\"telefono\":\"1\",\"direccion\":\"Casa\",\"edad\":21,\"estadoCivil\":\"Soltero/a\",\"datosLaborales\":\"Estudiante\",\"rol\":\"Administrador\"}", Encoding.UTF8, "application/json");
            Check((await client.PostAsync("/api/auth/registro", content)).StatusCode == HttpStatusCode.BadRequest, "Registro permite agregar rol");
            return;
        }

        var email = name == "HttpContratoEstricto" ? "admin@demo.local" : "adoptante@demo.local";
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDTO(email, "DemoPortal2026!"));
        Check(login.IsSuccessStatusCode, "Login falló");
        var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new("Bearer", json.RootElement.GetProperty("token").GetString());
        if (name == "HttpLoginPermisos")
        {
            Check((await client.GetAsync("/api/usuarios")).StatusCode == HttpStatusCode.Forbidden, "Falta 403");
            Check(!(await client.GetStringAsync("/api/auth/me")).Contains("passwordHash"), "Expone hash");
        }

        if (name == "HttpIdentidad")
        {
            using var scope = factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPortalRepository>();
            var other = (await repo.ListAsync<SolicitudDeAdopcion>()).First();
            Check((await client.GetAsync("/api/solicitudes/" + other.Id)).StatusCode == HttpStatusCode.Forbidden, "Consulta de tercero");
        }

        if (name == "HttpContratoEstricto")
        {
            var payload = new StringContent("{\"nombre\":\"P\",\"especie\":\"Perro\",\"raza\":null,\"edad\":2,\"sexo\":\"Macho\",\"fotoUrl\":null,\"chip\":null,\"estado\":\"Adoptada\"}", Encoding.UTF8, "application/json");
            Check((await client.PostAsync("/api/mascotas", payload)).StatusCode == HttpStatusCode.BadRequest, "Acepta estado inyectado");
        }
    }
}

public class Tests
{
    public static IEnumerable<object[]> CasesData => Cases.Names.Select(x => new object[] { x });

    [Theory, MemberData(nameof(CasesData))]
    public Task Caso(string name) => Cases.Run(name);
}

public class Runner
{
    public static async Task<int> Main()
    {
        var failures = 0;
        foreach (var name in Cases.Names)
        {
            try
            {
                await Cases.Run(name);
                Console.WriteLine("PASS " + name);
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine("FAIL " + name + ": " + ex);
            }
        }

        Console.WriteLine($"Resultado: {Cases.Names.Length - failures}/{Cases.Names.Length} aprobados");
        return failures == 0 ? 0 : 1;
    }
}
