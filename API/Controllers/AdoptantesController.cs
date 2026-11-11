// REFACTOR: Controlador extraído de PortalControllers.cs — responsabilidad única: perfiles de adoptantes.
namespace PortalAdopcion.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;

[ApiController, Route("api/adoptantes"), Authorize]
public class AdoptantesController(IUsuarioAuthService s) : PortalController
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

