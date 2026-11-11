namespace PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.DataAccess.Entities;

// REFACTOR: Registro, login, administración de usuarios/roles y perfiles de adoptantes.
public interface IUsuarioAuthService
{
    Task<UsuarioResponseDTO> RegisterAsync(RegistroUsuarioDTO d);
    Task<Usuario> LoginAsync(LoginDTO d);
    Task<UsuarioResponseDTO> MeAsync(Actor a);
    Task<List<UsuarioResponseDTO>> UsersAsync(Actor a);
    Task<UsuarioResponseDTO> CreateStaffAsync(Actor a, PersonalDTO d);
    Task<UsuarioResponseDTO> ChangeUserAsync(Actor a, Guid id, UsuarioCambioDTO d);
    Task<List<AdoptanteResponseDTO>> ProfilesAsync(Actor a);
    Task<AdoptanteResponseDTO> ProfileAsync(Actor a, Guid id);
    Task<AdoptanteResponseDTO> CreateProfileAsync(Actor a, PerfilDTO d);
    Task<AdoptanteResponseDTO> UpdateProfileAsync(Actor a, Guid id, PerfilDTO d);
    Task DeleteProfileAsync(Actor a, Guid id);
}
