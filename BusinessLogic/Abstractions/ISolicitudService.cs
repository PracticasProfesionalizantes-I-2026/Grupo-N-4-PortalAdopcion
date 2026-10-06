namespace PortalAdopcion.BusinessLogic.Abstractions;

using PortalAdopcion.Shared.DTOs;

public interface ISolicitudService
{
    Task<IEnumerable<SolicitudResponseDTO>> GetAllAsync(CancellationToken cancellationToken);

    Task<SolicitudResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SolicitudResponseDTO> CreateAsync(SolicitudCreateDTO dto, CancellationToken cancellationToken);

    Task<SolicitudResponseDTO> UpdateEstadoAsync(Guid id, SolicitudUpdateEstadoDTO dto, CancellationToken cancellationToken);
}