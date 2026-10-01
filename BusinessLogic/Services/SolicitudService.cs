namespace PortalAdopcion.BusinessLogic.Services;

using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;

public class SolicitudService : ISolicitudService
{
    private readonly ISolicitudRepository _solicitudRepository;
    private readonly IMascotaRepository _mascotaRepository;
    private readonly IAdoptanteRepository _adoptanteRepository;

    public SolicitudService(
        ISolicitudRepository solicitudRepository,
        IMascotaRepository mascotaRepository,
        IAdoptanteRepository adoptanteRepository)
    {
        _solicitudRepository = solicitudRepository;
        _mascotaRepository = mascotaRepository;
        _adoptanteRepository = adoptanteRepository;
    }

    public async Task<IEnumerable<SolicitudResponseDTO>> GetAllAsync(CancellationToken cancellationToken)
    {
        var solicitudes = await _solicitudRepository.GetAllAsync(cancellationToken);
        return solicitudes.Select(s => MapToResponseDTO(s)).ToList();
    }

    public async Task<SolicitudResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var solicitud = await _solicitudRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró la solicitud de adopción con id '{id}'.");

        return MapToResponseDTO(solicitud);
    }

    public async Task<SolicitudResponseDTO> CreateAsync(SolicitudCreateDTO dto, CancellationToken cancellationToken)
    {
        if (dto.MascotaId == Guid.Empty)
        {
            throw new ValidationException("El id de la mascota es obligatorio.");
        }

        if (dto.AdoptanteId == Guid.Empty)
        {
            throw new ValidationException("El id del adoptante es obligatorio.");
        }

        var mascota = await _mascotaRepository.GetByIdAsync(dto.MascotaId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró la mascota con id '{dto.MascotaId}'.");

        var adoptante = await _adoptanteRepository.GetByIdAsync(dto.AdoptanteId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el adoptante con id '{dto.AdoptanteId}'.");

        if (mascota.Estado != EstadoMascotaEnum.Disponible)
        {
            throw new ConflictException($"La mascota '{mascota.Nombre}' no está disponible para adopción (estado actual: {mascota.Estado}).");
        }

        var activaPorMascota = await _solicitudRepository.GetActivaPorMascotaAsync(dto.MascotaId, cancellationToken);
        if (activaPorMascota is not null)
        {
            throw new ConflictException($"La mascota '{mascota.Nombre}' ya tiene una solicitud de adopción activa.");
        }

        if (await _solicitudRepository.TieneActivaAsync(dto.AdoptanteId, cancellationToken))
        {
            throw new ConflictException("El adoptante ya tiene una solicitud de adopción activa.");
        }

        var creada = await _solicitudRepository.CreateAsync(new SolicitudDeAdopcion
        {
            MascotaId = dto.MascotaId,
            AdoptanteId = dto.AdoptanteId
        }, EstadoMascotaEnum.Reservada, cancellationToken);

        return MapToResponseDTO(creada);
    }

    public async Task<SolicitudResponseDTO> UpdateEstadoAsync(Guid id, SolicitudUpdateEstadoDTO dto, CancellationToken cancellationToken)
    {
        var solicitud = await _solicitudRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró la solicitud de adopción con id '{id}'.");

        if (solicitud.Estado != EstadoSolicitudEnum.Pendiente)
        {
            throw new ValidationException("Solo las solicitudes en estado 'Pendiente' pueden cambiar de estado.");
        }

        EstadoMascotaEnum? nuevoEstadoMascota = null;
        string? motivo = dto.Motivo;

        switch (dto.Estado)
        {
            case EstadoSolicitudEnum.Aprobada:
                nuevoEstadoMascota = EstadoMascotaEnum.Adoptada;
                break;
            case EstadoSolicitudEnum.Rechazada:
            case EstadoSolicitudEnum.Cancelada:
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    throw new ValidationException("Debe indicar un motivo para rechazar o cancelar la solicitud.");
                }

                nuevoEstadoMascota = EstadoMascotaEnum.Disponible;
                break;
            default:
                throw new ValidationException("El estado solicitado no es válido.");
        }

        var actualizada = await _solicitudRepository.UpdateEstadoAsync(
            id,
            dto.Estado,
            motivo,
            nuevoEstadoMascota,
            cancellationToken);

        if (actualizada is null)
        {
            throw new NotFoundException($"No se encontró la solicitud de adopción con id '{id}'.");
        }

        return MapToResponseDTO(actualizada);
    }

    private static SolicitudResponseDTO MapToResponseDTO(SolicitudDeAdopcion solicitud)
    {
        return new SolicitudResponseDTO(
            solicitud.Id,
            solicitud.MascotaId,
            solicitud.AdoptanteId,
            solicitud.FechaSolicitud,
            solicitud.Estado,
            solicitud.Motivo);
    }
}