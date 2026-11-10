// REFACTOR: Clase base extraída de PortalControllers.cs — proporciona la propiedad Actor a todos los controladores derivados.
namespace PortalAdopcion.API.Controllers;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PortalAdopcion.Shared.DTOs;

public abstract class PortalController : ControllerBase
{
    protected Actor Actor => new(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.FindFirstValue(ClaimTypes.Role)!, Guid.TryParse(User.FindFirstValue("adoptanteId"), out var id) ? id : null);
}
