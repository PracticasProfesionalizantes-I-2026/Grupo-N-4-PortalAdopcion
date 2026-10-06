namespace PortalAdopcion.BusinessLogic.Abstractions;

using PortalAdopcion.Shared.DTOs;

public interface IAdoptanteService
{
    Task<IEnumerable<AdoptanteResponseDTO>> GetAllAsync(CancellationToken cancellationToken);

    Task<AdoptanteResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<AdoptanteResponseDTO> CreateAsync(AdoptanteCreateDTO dto, CancellationToken cancellationToken);

    Task<AdoptanteResponseDTO> UpdateAsync(Guid id, AdoptanteUpdateDTO dto, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}