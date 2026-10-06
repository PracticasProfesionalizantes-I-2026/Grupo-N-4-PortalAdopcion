using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.BusinessLogic.Services;
using PortalAdopcion.DataAccess;
using PortalAdopcion.DataAccess.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<PortalAdopcionDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IMascotaRepository, MascotaRepository>();
builder.Services.AddScoped<IAdoptanteRepository, AdoptanteRepository>();
builder.Services.AddScoped<ISolicitudRepository, SolicitudRepository>();

builder.Services.AddScoped<IMascotaService, MascotaService>();
builder.Services.AddScoped<IAdoptanteService, AdoptanteService>();
builder.Services.AddScoped<ISolicitudService, SolicitudService>();

builder.Services.AddOpenApi();

var app = builder.Build();

DbInitializer.Initialize(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar/v1");
}

app.MapControllers();

app.Run();

public partial class Program;