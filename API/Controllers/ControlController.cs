// REFACTOR: Controlador extraído de PortalControllers.cs — responsabilidad única: control operativo (incidentes, auditoría, notificaciones, demo, reportes).
namespace PortalAdopcion.API.Controllers;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Exceptions;

[ApiController, Authorize, Route("api")]
#pragma warning disable CS9113 // Parámetros utilizados en ResetDemo (activo únicamente en compilaciones DEBUG)
public class ControlController(IControlOperativoService s, IAdopcionWorkflowService adopciones, IWebHostEnvironment env, IConfiguration config, IPortalRepository repo, IOutputCacheStore cache) : PortalController
#pragma warning restore CS9113
{
    [HttpGet("incidentes")]
    public async Task<IActionResult> Incidents() => Ok(await s.IncidentsAsync(Actor));
    [HttpPost("incidentes"), Authorize(Roles = "Administrador,Voluntario")]
    public async Task<IActionResult> Incident(IncidenteDTO d) => StatusCode(201, await s.IncidentAsync(Actor, d));
    [HttpGet("auditoria"), Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Audit(DateTime? desde, DateTime? hasta, string? accion, Guid? usuario) => Ok(await s.AuditAsync(Actor, desde, hasta, accion, usuario));
    [HttpGet("notificaciones")]
    public async Task<IActionResult> Notices() => Ok(await s.NotificationsAsync(Actor));
    [HttpPost("demo/procesar-abandonos"), Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Abandon()
    {
        if (!env.IsDevelopment())
            return NotFound();
        return Ok(new { procesados = await adopciones.AbandonAsync() });
    }

    // Reinicia el entorno demo (borra todos los datos y vuelve a sembrar los ejemplos).
    // Permite repetir la colección Bruno cuantas veces se quiera sin borrar el archivo .db.
    // Solo disponible en Development + modo demo y desde loopback: en cualquier otro caso, 404.
#if DEBUG
    // SECURITY: Endpoint destructivo de reinicio demo confinado exclusivamente a compilaciones DEBUG para prevenir su inclusión y exposición en producción (Release).
    [HttpPost("demo/reiniciar"), AllowAnonymous]
    public async Task<IActionResult> ResetDemo()
    {
        var loopback = IPAddress.IsLoopback(HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback);
        if (!env.IsDevelopment() || !config.GetValue<bool>("Demo:Seed") || !loopback)
            return NotFound();
        await repo.ResetDemoDataAsync();
        await DemoSeed.InitializeAsync(HttpContext.RequestServices);
        // El catálogo de mascotas está cacheado: hay que invalidarlo porque los IDs cambiaron.
        await cache.EvictByTagAsync("catalogo", default);
        return Ok(new { reiniciado = true, mensaje = "Datos demo restaurados." });
    }
#endif

    [HttpGet("reportes"), Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Report(string tipo, DateTime desde, DateTime hasta, string formato = "csv")
    {
        if (formato is not ("csv" or "pdf"))
            throw new ValidationException("Formato: csv o pdf.");
        var rows = await s.ReportAsync(Actor, tipo, desde, hasta);
        if (formato == "pdf")
            return File(ReportFiles.Pdf(rows), "application/pdf", tipo + ".pdf");
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(string.Join("\r\n", rows.Select(row => string.Join(";", row.Select(ReportFiles.Csv)))))).ToArray(), "text/csv; charset=utf-8", tipo + ".csv");
    }
}

