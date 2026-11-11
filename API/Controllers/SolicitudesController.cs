// REFACTOR: Controlador extraído de PortalControllers.cs — responsabilidad única: flujo de solicitudes de adopción.
namespace PortalAdopcion.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;

[ApiController, Route("api/solicitudes"), Authorize]
public class SolicitudesController(IAdopcionWorkflowService s, IClinicaVeterinariaService clinica) : PortalController
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
    public async Task<IActionResult> Validate(Guid id, ValidacionClinicaDTO d) => Ok(await clinica.ValidateClinicalAsync(Actor, id, d.Motivo));
    [HttpPost("{id:guid}/finalizacion"), Authorize(Roles = "Administrador,Voluntario")]
    public async Task<IActionResult> FinalizeAdoption(Guid id, MotivoDTO d) => Ok(await s.FinalizeAsync(Actor, id, d.Motivo));
}

