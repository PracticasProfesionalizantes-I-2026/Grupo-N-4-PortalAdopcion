// REFACTOR: Controlador extraído de PortalControllers.cs — responsabilidad única: gestión de usuarios de personal.
namespace PortalAdopcion.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;

[ApiController, Route("api/usuarios"), Authorize(Roles = "Administrador")]
public class UsuariosController(IUsuarioAuthService s) : PortalController
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await s.UsersAsync(Actor));
    [HttpPost]
    public async Task<IActionResult> Create(PersonalDTO d) => StatusCode(201, await s.CreateStaffAsync(Actor, d));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Change(Guid id, UsuarioCambioDTO d) => Ok(await s.ChangeUserAsync(Actor, id, d));
}

