
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PortalAdopcion.API;
using PortalAdopcion.API.Middleware;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.BusinessLogic.Services;
using PortalAdopcion.DataAccess;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(allowIntegerValues: false));

    o.JsonSerializerOptions.UnmappedMemberHandling =
        JsonUnmappedMemberHandling.Disallow;
});

// Pool/caché de SQLite: evita abrir y cerrar el archivo en cada request.
var csb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=portaladopcion-prueba.db")
{
    Cache = Microsoft.Data.Sqlite.SqliteCacheMode.Shared,
    Pooling = true,
    DefaultTimeout = 5
};

builder.Services.AddDbContext<PortalAdopcionDbContext>(
    o => o.UseSqlite(csb.ConnectionString));

builder.Services.AddScoped<IPortalRepository, PortalRepository>();

// REFACTOR: PortalService se dividió en servicios de dominio con responsabilidad única (SRP).
builder.Services.AddScoped<IUsuarioAuthService, UsuarioAuthService>();
builder.Services.AddScoped<IMascotaService, MascotaService>();
builder.Services.AddScoped<IAdopcionWorkflowService, AdopcionWorkflowService>();
builder.Services.AddScoped<IClinicaVeterinariaService, ClinicaVeterinariaService>();
builder.Services.AddScoped<IControlOperativoService, ControlOperativoService>();

// --- Configuración de Correo (MailKit / SmtpOptions) ---
builder.Services.Configure<PortalAdopcion.API.Options.SmtpOptions>(
    builder.Configuration.GetSection(
        PortalAdopcion.API.Options.SmtpOptions.SectionName));

builder.Services.AddSingleton<
    PortalAdopcion.API.Services.ISmtpClientFactory,
    PortalAdopcion.API.Services.SmtpClientFactory>();

// --- Compresión de respuestas (Brotli + Gzip) ---
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
    o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/json", "application/problem+json"]);
});

builder.Services.Configure<BrotliCompressionProviderOptions>(
    o => o.Level = CompressionLevel.Fastest);

builder.Services.Configure<GzipCompressionProviderOptions>(
    o => o.Level = CompressionLevel.Fastest);

// --- Caché de salida (server-side) para lecturas públicas y frecuentes ---
builder.Services.AddOutputCache(o =>
{
    o.AddPolicy(
        "Catalogo",
        b => b
            .Expire(TimeSpan.FromSeconds(20))
            .SetVaryByQuery(["estado", "especie", "sexo", "busqueda"])
            .Tag("catalogo"));

    o.AddPolicy(
        "Publico",
        b => b.Expire(TimeSpan.FromSeconds(60)));
});

// --- CORS ---
var corsOrigins =
    builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://localhost:5104"];

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// HARDENING: detectar uso de clave JWT de demostración.
var usingDemoJwtKey =
    !builder.Environment.IsEnvironment("Testing")
    && (
        builder.Configuration["Jwt:Key"] is null
        || builder.Configuration["Jwt:Key"]
            == "SoloParaDemoLocal-PortalAdopcion2026-CambiarClave");

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? (
        builder.Environment.IsEnvironment("Testing")
            ? "Testing-PortalAdopcion-Secret-AtLeast32Bytes"
            : "SoloParaDemoLocal-PortalAdopcion2026-CambiarClave");

if (string.IsNullOrEmpty(jwtKey)
    || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Configurá Jwt__Key con al menos 32 bytes.");
}

builder.Services.AddSingleton(new TokenIssuer(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "PortalAdopcion",
            ValidateAudience = true,
            ValidAudience = "PortalAdopcion",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var id = ctx.Principal?
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                var repo = ctx.HttpContext.RequestServices
                    .GetRequiredService<IPortalRepository>();

                var u = Guid.TryParse(id, out var uid)
                    ? await repo.FindAsync<Usuario>(uid)
                    : null;

                if (u is null
                    || u.Estado == EstadoUsuarioEnum.Baja
                    || (
                        u.Estado == EstadoUsuarioEnum.Suspendida
                        && u.Rol != "Adoptante")
                    || u.Version.ToString()
                        != ctx.Principal?.FindFirstValue("version"))
                {
                    ctx.Fail("Cuenta o sesión no habilitada.");
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<AbandonoJob>();
    builder.Services.AddHostedService<CorreoJob>();
}

var app = builder.Build();

if (usingDemoJwtKey)
{
    app.Logger.LogWarning(
        "SECURITY: se está usando la clave JWT de demostración por defecto. " +
        "Configurá Jwt__Key con un secreto propio antes de cualquier despliegue.");
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseResponseCompression();
app.UseDefaultFiles(new DefaultFilesOptions());

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Assets versionables con caché corta; el HTML no se cachea.
        var path = ctx.File.Name;

        ctx.Context.Response.Headers.CacheControl =
            path.EndsWith(
                ".html",
                StringComparison.OrdinalIgnoreCase)
                ? "no-cache"
                : "public,max-age=3600";
    }
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar/v1");
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<PortalAdopcionDbContext>();

    // SECURITY:
    // La limpieza automática del lock solo está permitida en
    // Development + SQLite.
    //
    // En producción, staging o cualquier otro proveedor de BD,
    // nunca se elimina automáticamente "__EFMigrationsLock".
    if (StartupGuards.CanCleanMigrationLock(
        app.Environment.IsDevelopment(),
        db.Database.IsSqlite()))
    {
        await CleanupStaleMigrationLockAsync(db, app.Logger);
    }
    else
    {
        // CRITICAL: se detectó un entorno donde la limpieza automática
        // del lock está deliberadamente deshabilitada por seguridad.
        app.Logger.LogCritical(
            "SECURITY: limpieza automática de locks de migración " +
            "deshabilitada fuera de Development + SQLite. " +
            "EnvironmentName: {EnvironmentName}, ProviderName: {ProviderName}",
            app.Environment.EnvironmentName,
            db.Database.ProviderName);
    }

    await db.Database.MigrateAsync();

    // WAL mejora la concurrencia lectura/escritura de SQLite.
    if (db.Database.IsSqlite())
    {
        await db.Database.ExecuteSqlRawAsync(
            "PRAGMA journal_mode=WAL;");

        await db.Database.ExecuteSqlRawAsync(
            "PRAGMA synchronous=NORMAL;");

        await db.Database.ExecuteSqlRawAsync(
            "PRAGMA busy_timeout=5000;");
    }

    if (app.Environment.IsDevelopment()
        && app.Configuration.GetValue<bool>("Demo:Seed"))
    {
        await DemoSeed.InitializeAsync(scope.ServiceProvider);
    }
}

// ---------------------------------------------------------------------------
// Seguridad de arranque: libera un lock de migración huérfano.
//
// Solo se invoca desde Development + SQLite.
// ---------------------------------------------------------------------------
static async Task CleanupStaleMigrationLockAsync(
    PortalAdopcionDbContext db,
    ILogger logger)
{
    List<string> timestamps;

    try
    {
        timestamps = await db.Database
            .SqlQueryRaw<string>(
                "SELECT \"Timestamp\" AS \"Value\" " +
                "FROM \"__EFMigrationsLock\"")
            .ToListAsync();
    }
    catch (Microsoft.Data.Sqlite.SqliteException)
    {
        return; // La tabla todavía no existe.
    }

    var stale = timestamps.Any(t =>
        !DateTimeOffset.TryParse(t, out var ts)
        || DateTimeOffset.UtcNow - ts
            > TimeSpan.FromSeconds(15));

    if (!stale)
        return;

    await db.Database.ExecuteSqlRawAsync(
        "DELETE FROM \"__EFMigrationsLock\";");

    logger.LogWarning(
        "Se liberó un lock de migración huérfano " +
        "(migración interrumpida); se reanuda el arranque.");
}

app.Run();

public partial class Program;
