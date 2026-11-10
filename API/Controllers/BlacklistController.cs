// REFACTOR: Controlador extraído de PortalControllers.cs — responsabilidad única: gestión de blacklist/sanciones.
// ACA: 403 Forbidden confirmado para adoptantes en blacklist que intentan postularse (ver CU-01/CU-02).
namespace PortalAdopcion.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;

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

