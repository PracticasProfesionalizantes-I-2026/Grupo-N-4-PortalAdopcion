namespace PortalAdopcion.BusinessLogic.Services;

using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;

public class MascotaService : IMascotaService
{
    private const int EdadMinima = 0;
    private const int EdadMaxima = 30;
    private const int MaxNombre = 100;
    private const int MaxRaza = 100;

    private readonly IMascotaRepository _mascotaRepository;

    public MascotaService(IMascotaRepository mascotaRepository)
    {
        _mascotaRepository = mascotaRepository;
    }

    public async Task<IEnumerable<MascotaResponseDTO>> GetAllAsync(EstadoMascotaEnum? estado, CancellationToken cancellationToken)
    {
        var mascotas = await _mascotaRepository.GetAllAsync(cancellationToken);

        if (estado.HasValue)
        {
            mascotas = mascotas.Where(m => m.Estado == estado.Value);
        }

        return mascotas.Select(m => MapToResponseDTO(m)).ToList();
    }

    public async Task<MascotaResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var mascota = await _mascotaRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró la mascota con id '{id}'.");

        return MapToResponseDTO(mascota);
    }

    public async Task<MascotaResponseDTO> CreateAsync(MascotaCreateDTO dto, CancellationToken cancellationToken)
    {
        ValidarNombre(dto.Nombre);
        ValidarRaza(dto.Raza);
        ValidarEdad(dto.Edad);

        var mascota = new Mascota
        {
            Nombre = dto.Nombre.Trim(),
            Especie = dto.Especie,
            Raza = dto.Raza?.Trim(),
            Edad = dto.Edad,
            Sexo = dto.Sexo,
            Estado = EstadoMascotaEnum.Disponible,
            FechaRegistro = DateTime.UtcNow
        };

        var creada = await _mascotaRepository.CreateAsync(mascota, cancellationToken);
        return MapToResponseDTO(creada);
    }

    public async Task<MascotaResponseDTO> UpdateAsync(Guid id, MascotaUpdateDTO dto, CancellationToken cancellationToken)
    {
        ValidarNombre(dto.Nombre);
        ValidarRaza(dto.Raza);
        ValidarEdad(dto.Edad);

        var actualizada = await _mascotaRepository.UpdateAsync(id, new Mascota
        {
            Nombre = dto.Nombre.Trim(),
            Especie = dto.Especie,
            Raza = dto.Raza?.Trim(),
            Edad = dto.Edad,
            Sexo = dto.Sexo,
            Estado = dto.Estado
        }, cancellationToken);

        if (actualizada is null)
        {
            throw new NotFoundException($"No se encontró la mascota con id '{id}'.");
        }

        return MapToResponseDTO(actualizada);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await _mascotaRepository.ExistsAsync(id, cancellationToken))
        {
            throw new NotFoundException($"No se encontró la mascota con id '{id}'.");
        }

        if (await _mascotaRepository.HasSolicitudesAsync(id, cancellationToken))
        {
            throw new ConflictException("No se puede eliminar la mascota porque tiene solicitudes de adopción asociadas.");
        }

        await _mascotaRepository.DeleteAsync(id, cancellationToken);
    }

    private static void ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ValidationException("El nombre de la mascota es obligatorio.");
        }

        if (nombre.Length > MaxNombre)
        {
            throw new ValidationException($"El nombre de la mascota no puede superar los {MaxNombre} caracteres.");
        }
    }

    private static void ValidarRaza(string? raza)
    {
        if (raza is not null && raza.Length > MaxRaza)
        {
            throw new ValidationException($"La raza no puede superar los {MaxRaza} caracteres.");
        }
    }

    private static void ValidarEdad(int edad)
    {
        if (edad < EdadMinima || edad > EdadMaxima)
        {
            throw new ValidationException($"La edad debe estar entre {EdadMinima} y {EdadMaxima} años.");
        }
    }

    private static MascotaResponseDTO MapToResponseDTO(Mascota mascota)
    {
        return new MascotaResponseDTO(
            mascota.Id,
            mascota.Nombre,
            mascota.Especie,
            mascota.Raza,
            mascota.Edad,
            mascota.Sexo,
            mascota.Estado,
            mascota.FechaRegistro);
    }
}