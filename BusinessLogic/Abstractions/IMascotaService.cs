namespace PortalAdopcion.BusinessLogic.Abstractions;

using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;

public interface IMascotaService
{
    Task<IEnumerable<MascotaResponseDTO>> GetAllAsync(EstadoMascotaEnum? estado, CancellationToken cancellationToken);

    Task<MascotaResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<MascotaResponseDTO> CreateAsync(MascotaCreateDTO dto, CancellationToken cancellationToken);

    Task<MascotaResponseDTO> UpdateAsync(Guid id, MascotaUpdateDTO dto, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}