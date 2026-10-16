namespace PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.DataAccess.Entities;

public interface IPortalService
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
    Task<List<MascotaResponseDTO>> PetsAsync(EstadoMascotaEnum? estado);
    Task<MascotaResponseDTO> PetAsync(Guid id);
    Task<MascotaResponseDTO> CreatePetAsync(Actor a, MascotaFichaDTO d);
    Task<MascotaResponseDTO> UpdatePetAsync(Actor a, Guid id, MascotaFichaDTO d);
    Task<MascotaResponseDTO> ChangePetAsync(Actor a, Guid id, CambioMascotaDTO d);
    Task<List<SancionResponseDTO>> BlacklistAsync(Actor a);
    Task<SancionResponseDTO> SaveBlacklistAsync(Actor a, Guid? id, BlacklistDTO d);
    Task RevokeBlacklistAsync(Actor a, Guid id, string reason);
    Task<List<SolicitudResponseDTO>> RequestsAsync(Actor a);
    Task<SolicitudResponseDTO> RequestAsync(Actor a, Guid id);
    Task<SolicitudResponseDTO> PostulateAsync(Actor a, PostulacionDTO d);
    Task<SolicitudResponseDTO> EvaluateAsync(Actor a, Guid id, EvaluacionDTO d);
    Task<SolicitudResponseDTO> CancelAsync(Actor a, Guid id, string reason);
    Task<EntrevistaResponseDTO> ScheduleAsync(Actor a, Guid id, EntrevistaDTO d);
    Task<List<EntrevistaResponseDTO>> InterviewsAsync(Actor a, Guid id);
    Task<SolicitudResponseDTO> InterviewResultAsync(Actor a, Guid id, ResultadoEntrevistaDTO d);
    Task<List<SeguimientoResponseDTO>> FollowupsAsync(Actor a, Guid id);
    Task<SeguimientoResponseDTO> FollowupAsync(Actor a, Guid id, SeguimientoDTO d);
    Task<SolicitudResponseDTO> ValidateClinicalAsync(Actor a, Guid id, string reason);
    Task<SolicitudResponseDTO> FinalizeAsync(Actor a, Guid id, string reason);
    Task<int> AbandonAsync();
    Task<HistorialResponseDTO> CreateHistoryAsync(Actor a, Guid pet, ClinicaDTO d);
    Task<HistorialDetalleResponseDTO> HistoryAsync(Actor a, Guid pet);
    Task<EventoMedicoResponseDTO> MedicalEventAsync(Actor a, Guid pet, EventoMedicoDTO d);
    Task<IncidenteResponseDTO> IncidentAsync(Actor a, IncidenteDTO d);
    Task<List<IncidenteResponseDTO>> IncidentsAsync(Actor a);
    Task<List<AuditoriaResponseDTO>> AuditAsync(Actor a, DateTime? desde, DateTime? hasta, string? accion, Guid? usuario);
    Task<List<NotificacionResponseDTO>> NotificationsAsync(Actor a);
    Task<List<string[]>> ReportAsync(Actor a, string tipo, DateTime desde, DateTime hasta);
}
