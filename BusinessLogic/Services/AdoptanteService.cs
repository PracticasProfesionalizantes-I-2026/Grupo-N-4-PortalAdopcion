namespace PortalAdopcion.BusinessLogic.Services;

using System.Text.RegularExpressions;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Exceptions;

public partial class AdoptanteService : IAdoptanteService
{
    private const int MaxNombreCompleto = 150;
    private const int MaxEmail = 150;
    private const int MaxDni = 20;
    private const int MaxTelefono = 30;
    private const int MaxDireccion = 200;

    private readonly IAdoptanteRepository _adoptanteRepository;

    public AdoptanteService(IAdoptanteRepository adoptanteRepository)
    {
        _adoptanteRepository = adoptanteRepository;
    }

    public async Task<IEnumerable<AdoptanteResponseDTO>> GetAllAsync(CancellationToken cancellationToken)
    {
        var adoptantes = await _adoptanteRepository.GetAllAsync(cancellationToken);
        return adoptantes.Select(a => MapToResponseDTO(a)).ToList();
    }

    public async Task<AdoptanteResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var adoptante = await _adoptanteRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el adoptante con id '{id}'.");

        return MapToResponseDTO(adoptante);
    }

    public async Task<AdoptanteResponseDTO> CreateAsync(AdoptanteCreateDTO dto, CancellationToken cancellationToken)
    {
        ValidarDatos(dto.NombreCompleto, dto.Email, dto.Dni, dto.Telefono, dto.Direccion);

        if (await _adoptanteRepository.EmailExisteAsync(dto.Email, cancellationToken))
        {
            throw new ConflictException($"Ya existe un adoptante registrado con el email '{dto.Email}'.");
        }

        if (await _adoptanteRepository.DniExisteAsync(dto.Dni, cancellationToken))
        {
            throw new ConflictException($"Ya existe un adoptante registrado con el DNI '{dto.Dni}'.");
        }

        var adoptante = new Adoptante
        {
            NombreCompleto = dto.NombreCompleto.Trim(),
            Email = dto.Email.Trim(),
            Dni = dto.Dni.Trim(),
            Telefono = dto.Telefono.Trim(),
            Direccion = dto.Direccion.Trim(),
            FechaRegistro = DateTime.UtcNow
        };

        var creado = await _adoptanteRepository.CreateAsync(adoptante, cancellationToken);
        return MapToResponseDTO(creado);
    }

    public async Task<AdoptanteResponseDTO> UpdateAsync(Guid id, AdoptanteUpdateDTO dto, CancellationToken cancellationToken)
    {
        ValidarDatos(dto.NombreCompleto, dto.Email, dto.Dni, dto.Telefono, dto.Direccion);

        if (await _adoptanteRepository.EmailExisteAsync(dto.Email, id, cancellationToken))
        {
            throw new ConflictException($"Ya existe un adoptante registrado con el email '{dto.Email}'.");
        }

        if (await _adoptanteRepository.DniExisteAsync(dto.Dni, id, cancellationToken))
        {
            throw new ConflictException($"Ya existe un adoptante registrado con el DNI '{dto.Dni}'.");
        }

        var actualizado = await _adoptanteRepository.UpdateAsync(id, new Adoptante
        {
            NombreCompleto = dto.NombreCompleto.Trim(),
            Email = dto.Email.Trim(),
            Dni = dto.Dni.Trim(),
            Telefono = dto.Telefono.Trim(),
            Direccion = dto.Direccion.Trim()
        }, cancellationToken);

        if (actualizado is null)
        {
            throw new NotFoundException($"No se encontró el adoptante con id '{id}'.");
        }

        return MapToResponseDTO(actualizado);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await _adoptanteRepository.ExistsAsync(id, cancellationToken))
        {
            throw new NotFoundException($"No se encontró el adoptante con id '{id}'.");
        }

        if (await _adoptanteRepository.HasSolicitudesAsync(id, cancellationToken))
        {
            throw new ConflictException("No se puede eliminar el adoptante porque tiene solicitudes de adopción asociadas.");
        }

        await _adoptanteRepository.DeleteAsync(id, cancellationToken);
    }

    private static void ValidarDatos(string nombreCompleto, string email, string dni, string telefono, string direccion)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
        {
            throw new ValidationException("El nombre completo es obligatorio.");
        }

        if (nombreCompleto.Length > MaxNombreCompleto)
        {
            throw new ValidationException($"El nombre completo no puede superar los {MaxNombreCompleto} caracteres.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("El email es obligatorio.");
        }

        if (email.Length > MaxEmail)
        {
            throw new ValidationException($"El email no puede superar los {MaxEmail} caracteres.");
        }

        if (!EmailRegex().IsMatch(email.Trim()))
        {
            throw new ValidationException("El email ingresado no tiene un formato válido.");
        }

        if (string.IsNullOrWhiteSpace(dni))
        {
            throw new ValidationException("El DNI es obligatorio.");
        }

        if (dni.Length > MaxDni)
        {
            throw new ValidationException($"El DNI no puede superar los {MaxDni} caracteres.");
        }

        if (string.IsNullOrWhiteSpace(telefono))
        {
            throw new ValidationException("El teléfono es obligatorio.");
        }

        if (telefono.Length > MaxTelefono)
        {
            throw new ValidationException($"El teléfono no puede superar los {MaxTelefono} caracteres.");
        }

        if (string.IsNullOrWhiteSpace(direccion))
        {
            throw new ValidationException("La dirección es obligatoria.");
        }

        if (direccion.Length > MaxDireccion)
        {
            throw new ValidationException($"La dirección no puede superar los {MaxDireccion} caracteres.");
        }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    private static AdoptanteResponseDTO MapToResponseDTO(Adoptante adoptante)
    {
        return new AdoptanteResponseDTO(
            adoptante.Id,
            adoptante.NombreCompleto,
            adoptante.Email,
            adoptante.Dni,
            adoptante.Telefono,
            adoptante.Direccion,
            adoptante.FechaRegistro);
    }
}