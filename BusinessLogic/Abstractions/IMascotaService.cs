namespace PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.DataAccess.Entities;

// REFACTOR: Catálogo, altas, fichas y estados de mascotas.
public interface IMascotaService
{
    // REFACTOR: Firma ampliada con filtros dinámicos (RF 5.1 / CU-09) ejecutados en base de datos.
    Task<IReadOnlyList<MascotaResponseDTO>> PetsAsync(EstadoMascotaEnum? estado = null, EspecieEnum? especie = null, SexoEnum? sexo = null, string? busqueda = null, CancellationToken ct = default);
    Task<MascotaResponseDTO> PetAsync(Guid id);
    Task<MascotaResponseDTO> CreatePetAsync(Actor a, MascotaFichaDTO d);
    Task<MascotaResponseDTO> UpdatePetAsync(Actor a, Guid id, MascotaFichaDTO d);
    Task<MascotaResponseDTO> ChangePetAsync(Actor a, Guid id, CambioMascotaDTO d);
}
