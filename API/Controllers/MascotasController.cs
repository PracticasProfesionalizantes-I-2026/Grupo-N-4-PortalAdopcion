// REFACTOR: Controlador extraído de PortalControllers.cs — responsabilidad única: catálogo de mascotas e historial clínico.
namespace PortalAdopcion.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;

[ApiController, Route("api/mascotas")]
public class MascotasController(IMascotaService s, IClinicaVeterinariaService clinica) : PortalController
{
    [HttpGet, OutputCache(PolicyName = "Catalogo")]
    public async Task<IActionResult> List(
        [FromQuery] EstadoMascotaEnum? estado,
        [FromQuery] EspecieEnum? especie,
        [FromQuery] SexoEnum? sexo,
        [FromQuery] string? busqueda,
        CancellationToken ct = default) => Ok(await s.PetsAsync(estado, especie, sexo, busqueda, ct));
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
    // DOC: DELETE es un alias semántico de la baja lógica: equivale a PATCH /api/mascotas/{id}/estado con estado Inactiva.
    // No borra el registro; la mascota queda Inactiva con el motivo indicado.
    [HttpDelete("{id:guid}"), Authorize(Roles = "Administrador,Voluntario")]
    [EndpointSummary("Baja lógica de una mascota (alias de PATCH /api/mascotas/{id}/estado)")]
    [EndpointDescription("Alias semántico: marca la mascota como Inactiva con el motivo indicado. Equivale a PATCH /api/mascotas/{id}/estado con estado Inactiva. No elimina el registro.")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string motivo)
    {
        await s.ChangePetAsync(Actor, id, new(EstadoMascotaEnum.Inactiva, motivo));
        return NoContent();
    }

    [HttpGet("{id:guid}/historial"), Authorize]
    public async Task<IActionResult> History(Guid id) => Ok(await clinica.HistoryAsync(Actor, id));
    [HttpPost("{id:guid}/historial"), Authorize(Roles = "Veterinario")]
    public async Task<IActionResult> Initial(Guid id, ClinicaDTO d) => StatusCode(201, await clinica.CreateHistoryAsync(Actor, id, d));
    [HttpPost("{id:guid}/eventos-medicos"), Authorize(Roles = "Veterinario")]
    public async Task<IActionResult> Event(Guid id, EventoMedicoDTO d) => StatusCode(201, await clinica.MedicalEventAsync(Actor, id, d));
}

