namespace PortalAdopcion.API.Controllers;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;

public abstract class PortalController : ControllerBase
{
    protected Actor Actor => new(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.FindFirstValue(ClaimTypes.Role)!, Guid.TryParse(User.FindFirstValue("adoptanteId"), out var id) ? id : null);
}

[ApiController, Route("api/auth")]
public class AuthController(IPortalService s, TokenIssuer tokens) : PortalController
{
    [HttpPost("registro"), AllowAnonymous]
    public async Task<IActionResult> Register(RegistroUsuarioDTO d) => StatusCode(201, await s.RegisterAsync(d));
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginDTO d) => Ok(tokens.Issue(await s.LoginAsync(d)));
    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me() => Ok(await s.MeAsync(Actor));
}

[ApiController, Route("api/usuarios"), Authorize(Roles = "Administrador")]
public class UsuariosController(IPortalService s) : PortalController
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await s.UsersAsync(Actor));
    [HttpPost]
    public async Task<IActionResult> Create(PersonalDTO d) => StatusCode(201, await s.CreateStaffAsync(Actor, d));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Change(Guid id, UsuarioCambioDTO d) => Ok(await s.ChangeUserAsync(Actor, id, d));
}

[ApiController, Route("api/adoptantes"), Authorize]
public class AdoptantesController(IPortalService s) : PortalController
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await s.ProfilesAsync(Actor));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await s.ProfileAsync(Actor, id));
    [HttpPost]
    public async Task<IActionResult> Create(PerfilDTO d) => StatusCode(201, await s.CreateProfileAsync(Actor, d));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PerfilDTO d) => Ok(await s.UpdateProfileAsync(Actor, id, d));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await s.DeleteProfileAsync(Actor, id);
        return NoContent();
    }
}

[ApiController, Route("api/mascotas")]
public class MascotasController(IPortalService s) : PortalController
{
    [HttpGet, OutputCache(PolicyName = "Catalogo")]
    public async Task<IActionResult> List(EstadoMascotaEnum? estado) => Ok(await s.PetsAsync(estado));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await s.PetAsync(id));
    [HttpPost, Authorize(Roles = "Administrador,Voluntario")]
    public async Task<IActionResult> Create(MascotaFichaDTO d)
    {
        var m = await s.CreatePetAsync(Actor, d);
        return CreatedAtAction(nameof(Get), new { id = m.Id }, m);
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "Administrador,Voluntario")]
    public async Task<IActionResult> Update(Guid id, MascotaFichaDTO d) => Ok(await s.UpdatePetAsync(Actor, id, d));
    [HttpPatch("{id:guid}/estado"), Authorize(Roles = "Administrador,Voluntario")]
    public async Task<IActionResult> State(Guid id, CambioMascotaDTO d) => Ok(await s.ChangePetAsync(Actor, id, d));
    [HttpDelete("{id:guid}"), Authorize(Roles = "Administrador,Voluntario")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string motivo)
    {
        await s.ChangePetAsync(Actor, id, new(EstadoMascotaEnum.Inactiva, motivo));
        return NoContent();
    }

    [HttpGet("{id:guid}/historial"), Authorize]
    public async Task<IActionResult> History(Guid id) => Ok(await s.HistoryAsync(Actor, id));
    [HttpPost("{id:guid}/historial"), Authorize(Roles = "Veterinario")]
    public async Task<IActionResult> Initial(Guid id, ClinicaDTO d) => StatusCode(201, await s.CreateHistoryAsync(Actor, id, d));
    [HttpPost("{id:guid}/eventos-medicos"), Authorize(Roles = "Veterinario")]
    public async Task<IActionResult> Event(Guid id, EventoMedicoDTO d) => StatusCode(201, await s.MedicalEventAsync(Actor, id, d));
}

[ApiController, Route("api/blacklists"), Authorize(Roles = "Administrador")]
public class BlacklistController(IPortalService s) : PortalController
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await s.BlacklistAsync(Actor));
    [HttpPost]
    public async Task<IActionResult> Add(BlacklistDTO d) => StatusCode(201, await s.SaveBlacklistAsync(Actor, null, d));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, BlacklistDTO d) => Ok(await s.SaveBlacklistAsync(Actor, id, d));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string motivo)
    {
        await s.RevokeBlacklistAsync(Actor, id, motivo);
        return NoContent();
    }
}

[ApiController, Route("api/solicitudes"), Authorize]
public class SolicitudesController(IPortalService s) : PortalController
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await s.RequestsAsync(Actor));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await s.RequestAsync(Actor, id));
    [HttpPost]
    public async Task<IActionResult> Create(PostulacionDTO d)
    {
        var value = await s.PostulateAsync(Actor, d);
        return CreatedAtAction(nameof(Get), new { id = value.Id }, value);
    }

    [HttpPatch("{id:guid}/evaluacion"), Authorize(Roles = "Administrador,Voluntario")]
    public async Task<IActionResult> Evaluate(Guid id, EvaluacionDTO d) => Ok(await s.EvaluateAsync(Actor, id, d));
    [HttpPost("{id:guid}/cancelacion")]
    public async Task<IActionResult> Cancel(Guid id, MotivoDTO d) => Ok(await s.CancelAsync(Actor, id, d.Motivo));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string motivo)
    {
        await s.CancelAsync(Actor, id, motivo);
        return NoContent();
    }

    [HttpGet("{id:guid}/entrevista")]
    public async Task<IActionResult> Interviews(Guid id) => Ok(await s.InterviewsAsync(Actor, id));
    [HttpPost("{id:guid}/entrevista"), Authorize(Roles = "Administrador,Voluntario")]
    public async Task<IActionResult> Schedule(Guid id, EntrevistaDTO d) => StatusCode(201, await s.ScheduleAsync(Actor, id, d));
    [HttpPatch("{id:guid}/entrevista"), Authorize(Roles = "Administrador,Voluntario")]
    public async Task<IActionResult> Result(Guid id, ResultadoEntrevistaDTO d) => Ok(await s.InterviewResultAsync(Actor, id, d));
    [HttpGet("{id:guid}/seguimientos")]
    public async Task<IActionResult> Followups(Guid id) => Ok(await s.FollowupsAsync(Actor, id));
    [HttpPost("{id:guid}/seguimientos"), Authorize(Roles = "Administrador,Voluntario")]
    public async Task<IActionResult> Followup(Guid id, SeguimientoDTO d) => StatusCode(201, await s.FollowupAsync(Actor, id, d));
    [HttpPost("{id:guid}/validacion-veterinaria"), Authorize(Roles = "Veterinario")]
    public async Task<IActionResult> Validate(Guid id, ValidacionClinicaDTO d) => Ok(await s.ValidateClinicalAsync(Actor, id, d.Motivo));
    [HttpPost("{id:guid}/finalizacion"), Authorize(Roles = "Administrador,Voluntario")]
    public async Task<IActionResult> FinalizeAdoption(Guid id, MotivoDTO d) => Ok(await s.FinalizeAsync(Actor, id, d.Motivo));
}

[ApiController, Authorize, Route("api")]
public class ControlController(IPortalService s, IWebHostEnvironment env, IConfiguration config, IPortalRepository repo, IOutputCacheStore cache) : PortalController
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
        return Ok(new { procesados = await s.AbandonAsync() });
    }

    // Reinicia el entorno demo (borra todos los datos y vuelve a sembrar los ejemplos).
    // Permite repetir la colección Bruno cuantas veces se quiera sin borrar el archivo .db.
    // Solo disponible en Development + modo demo y desde loopback: en cualquier otro caso, 404.
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
