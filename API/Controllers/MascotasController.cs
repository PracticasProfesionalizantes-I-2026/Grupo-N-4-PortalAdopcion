namespace PortalAdopcion.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;

[ApiController]
[Route("api/mascotas")]
public class MascotasController : ControllerBase
{
    private readonly IMascotaService _service;

    public MascotasController(IMascotaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MascotaResponseDTO>>> GetAll(
        [FromQuery] EstadoMascotaEnum? estado,
        CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _service.GetAllAsync(estado, cancellationToken);
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
    public async Task<ActionResult<MascotaResponseDTO>> GetById(Guid id, CancellationToken cancellationToken)
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
    public async Task<ActionResult<MascotaResponseDTO>> Create(MascotaCreateDTO dto, CancellationToken cancellationToken)
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

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MascotaResponseDTO>> Update(Guid id, MascotaUpdateDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await _service.UpdateAsync(id, dto, cancellationToken);
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
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteAsync(id, cancellationToken);
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