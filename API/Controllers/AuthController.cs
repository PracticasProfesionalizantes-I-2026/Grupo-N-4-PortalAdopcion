// REFACTOR: Controlador extraído de PortalControllers.cs — responsabilidad única: autenticación y registro.
namespace PortalAdopcion.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;

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

