namespace PortalAdopcion.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;

[ApiController]
[Route("api/solicitudes")]
public class SolicitudesController : ControllerBase
{
    private readonly ISolicitudService _service;

    public SolicitudesController(ISolicitudService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SolicitudResponseDTO>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _service.GetAllAsync(cancellationToken);
            return Ok(resultado);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SolicitudResponseDTO>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _service.GetByIdAsync(id, cancellationToken);
            return Ok(resultado);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<SolicitudResponseDTO>> Create(SolicitudCreateDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, resultado);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/estado")]
    public async Task<ActionResult<SolicitudResponseDTO>> UpdateEstado(Guid id, SolicitudUpdateEstadoDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _service.UpdateEstadoAsync(id, dto, cancellationToken);
            return Ok(resultado);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string? motivo, CancellationToken cancellationToken)
    {
        try
        {
            await _service.UpdateEstadoAsync(
                id,
                new SolicitudUpdateEstadoDTO(EstadoSolicitudEnum.Cancelada, motivo),
                cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}