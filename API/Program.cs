using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PortalAdopcion.API.Middleware;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.BusinessLogic.Services;
using PortalAdopcion.DataAccess;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.Exceptions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    o.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});
// Pool/caché de SQLite: evita abrir y cerrar el archivo en cada request.
var csb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=portaladopcion-prueba.db")
{
    Cache = Microsoft.Data.Sqlite.SqliteCacheMode.Shared,
    Pooling = true,
    DefaultTimeout = 5
};
builder.Services.AddDbContext<PortalAdopcionDbContext>(o => o.UseSqlite(csb.ConnectionString));
builder.Services.AddScoped<IPortalRepository, PortalRepository>();
builder.Services.AddScoped<IPortalService, PortalService>();

// --- Compresión de respuestas (Brotli + Gzip) ---
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
    o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json", "application/problem+json"]);
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// --- Caché de salida (server-side) para lecturas públicas y frecuentes ---
builder.Services.AddOutputCache(o =>
{
    o.AddPolicy("Catalogo", b => b.Expire(TimeSpan.FromSeconds(20)).SetVaryByQuery("estado").Tag("catalogo"));
    o.AddPolicy("Publico", b => b.Expire(TimeSpan.FromSeconds(60)));
});

// --- CORS (permite desarrollo del frontend desde otro origen) ---
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5104"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? (builder.Environment.IsEnvironment("Testing") 
        ? "Testing-PortalAdopcion-Secret-AtLeast32Bytes" 
        : "SoloParaDemoLocal-PortalAdopcion2026-CambiarClave");
if (string.IsNullOrEmpty(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Configurá Jwt__Key con al menos 32 bytes.");
builder.Services.AddSingleton(new TokenIssuer(jwtKey));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "PortalAdopcion",
        ValidateAudience = true,
        ValidAudience = "PortalAdopcion",
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
        {
            var id = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var repo = ctx.HttpContext.RequestServices.GetRequiredService<IPortalRepository>();
            var u = Guid.TryParse(id, out var uid) ? await repo.FindAsync<Usuario>(uid) : null;
            if (u is null || u.Estado == "Baja" || (u.Estado == "Suspendida" && u.Rol != "Adoptante") || u.Version.ToString() != ctx.Principal?.FindFirstValue("version"))
                ctx.Fail("Cuenta o sesión no habilitada.");
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
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseResponseCompression();
app.UseDefaultFiles(new DefaultFilesOptions());
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Assets versionables con caché corta; el HTML no se cachea.
        var path = ctx.File.Name;
        ctx.Context.Response.Headers.CacheControl = path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
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
    var db = scope.ServiceProvider.GetRequiredService<PortalAdopcionDbContext>();
    await CleanupStaleMigrationLockAsync(db, app.Logger);
    await db.Database.MigrateAsync();
    // WAL mejora la concurrencia lectura/escritura de SQLite y reduce bloqueos.
    if (db.Database.IsSqlite())
    {
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");
    }
    if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Demo:Seed"))
        await DemoSeed.InitializeAsync(scope.ServiceProvider);
}

// ---------------------------------------------------------------------------
// Seguridad de arranque: libera un lock de migración huérfano.
//
// Si una migración se interrumpe a mitad de camino (matando el proceso o, como
// pasó acá, OneDrive bloqueando el archivo .db/.db-wal), queda una fila en
// "__EFMigrationsLock" que nadie libera. EF Core 10 la interpreta como "otra
// instancia está migrando" y reintenta en un bucle SIN TIMEOUT
// (SqliteHistoryRepository.AcquireDatabaseLockAsync), así que MigrateAsync()
// nunca vuelve y la API jamás abre el puerto. El umbral es corto porque una
// migración completa de este proyecto tarda segundos.
// ---------------------------------------------------------------------------
static async Task CleanupStaleMigrationLockAsync(PortalAdopcionDbContext db, ILogger logger)
{
    List<string> timestamps;
    try
    {
        timestamps = await db.Database
            .SqlQueryRaw<string>("SELECT \"Timestamp\" AS \"Value\" FROM \"__EFMigrationsLock\"")
            .ToListAsync();
    }
    catch (Microsoft.Data.Sqlite.SqliteException)
    {
        return; // La tabla todavía no existe: no hay nada que limpiar.
    }

    var stale = timestamps.Any(t =>
        !DateTimeOffset.TryParse(t, out var ts) ||
        DateTimeOffset.UtcNow - ts > TimeSpan.FromSeconds(15));
    if (!stale) return;

    await db.Database.ExecuteSqlRawAsync("DELETE FROM \"__EFMigrationsLock\";");
    logger.LogWarning("Se liberó un lock de migración huérfano (migración interrumpida); se reanuda el arranque.");
}

app.Run();
public partial class Program;
