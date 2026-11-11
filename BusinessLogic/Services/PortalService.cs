namespace PortalAdopcion.BusinessLogic.Services;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.BusinessLogic.Mapping;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.Constants;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;
using PortalAdopcion.Shared.Security;

// REFACTOR: PortalService quedó como fachada de compatibilidad que delega en los cinco servicios de dominio.
// La lógica de negocio vive en: UsuarioAuthService, MascotaService, AdopcionWorkflowService,
// ClinicaVeterinariaService y ControlOperativoService. Los controladores ya no dependen de esta clase.
public class PortalService : IPortalService
{
    private readonly UsuarioAuthService _usuarioAuth;
    private readonly MascotaService _mascota;
    private readonly AdopcionWorkflowService _adopcionWorkflow;
    private readonly ClinicaVeterinariaService _clinicaVeterinaria;
    private readonly ControlOperativoService _controlOperativo;

    public PortalService(IPortalRepository repo, TimeProvider clock)
    {
        _usuarioAuth = new UsuarioAuthService(repo, clock);
        _mascota = new MascotaService(repo, clock);
        _adopcionWorkflow = new AdopcionWorkflowService(repo, clock);
        _clinicaVeterinaria = new ClinicaVeterinariaService(repo, clock);
        _controlOperativo = new ControlOperativoService(repo, clock);
    }

    public static bool Activa(EstadoSolicitudEnum e) => ServiceBase.Activa(e);

    public Task<UsuarioResponseDTO> RegisterAsync(RegistroUsuarioDTO d) => _usuarioAuth.RegisterAsync(d);
    public Task<Usuario> LoginAsync(LoginDTO d) => _usuarioAuth.LoginAsync(d);
    public Task<UsuarioResponseDTO> MeAsync(Actor a) => _usuarioAuth.MeAsync(a);
    public Task<List<UsuarioResponseDTO>> UsersAsync(Actor a) => _usuarioAuth.UsersAsync(a);
    public Task<UsuarioResponseDTO> CreateStaffAsync(Actor a, PersonalDTO d) => _usuarioAuth.CreateStaffAsync(a, d);
    public Task<UsuarioResponseDTO> ChangeUserAsync(Actor a, Guid id, UsuarioCambioDTO d) => _usuarioAuth.ChangeUserAsync(a, id, d);
    public Task<List<AdoptanteResponseDTO>> ProfilesAsync(Actor a) => _usuarioAuth.ProfilesAsync(a);
    public Task<AdoptanteResponseDTO> ProfileAsync(Actor a, Guid id) => _usuarioAuth.ProfileAsync(a, id);
    public Task<AdoptanteResponseDTO> CreateProfileAsync(Actor a, PerfilDTO d) => _usuarioAuth.CreateProfileAsync(a, d);
    public Task<AdoptanteResponseDTO> UpdateProfileAsync(Actor a, Guid id, PerfilDTO d) => _usuarioAuth.UpdateProfileAsync(a, id, d);
    public Task DeleteProfileAsync(Actor a, Guid id) => _usuarioAuth.DeleteProfileAsync(a, id);
    public Task<IReadOnlyList<MascotaResponseDTO>> PetsAsync(EstadoMascotaEnum? estado = null, EspecieEnum? especie = null, SexoEnum? sexo = null, string? busqueda = null, CancellationToken ct = default) => _mascota.PetsAsync(estado, especie, sexo, busqueda, ct);
    public Task<MascotaResponseDTO> PetAsync(Guid id) => _mascota.PetAsync(id);
    public Task<MascotaResponseDTO> CreatePetAsync(Actor a, MascotaFichaDTO d) => _mascota.CreatePetAsync(a, d);
    public Task<MascotaResponseDTO> UpdatePetAsync(Actor a, Guid id, MascotaFichaDTO d) => _mascota.UpdatePetAsync(a, id, d);
    public Task<MascotaResponseDTO> ChangePetAsync(Actor a, Guid id, CambioMascotaDTO d) => _mascota.ChangePetAsync(a, id, d);
    public Task<List<SolicitudResponseDTO>> RequestsAsync(Actor a) => _adopcionWorkflow.RequestsAsync(a);
    public Task<SolicitudResponseDTO> RequestAsync(Actor a, Guid id) => _adopcionWorkflow.RequestAsync(a, id);
    public Task<SolicitudResponseDTO> PostulateAsync(Actor a, PostulacionDTO d) => _adopcionWorkflow.PostulateAsync(a, d);
    public Task<SolicitudResponseDTO> EvaluateAsync(Actor a, Guid id, EvaluacionDTO d) => _adopcionWorkflow.EvaluateAsync(a, id, d);
    public Task<SolicitudResponseDTO> CancelAsync(Actor a, Guid id, string reason) => _adopcionWorkflow.CancelAsync(a, id, reason);
    public Task<EntrevistaResponseDTO> ScheduleAsync(Actor a, Guid id, EntrevistaDTO d) => _adopcionWorkflow.ScheduleAsync(a, id, d);
    public Task<List<EntrevistaResponseDTO>> InterviewsAsync(Actor a, Guid id) => _adopcionWorkflow.InterviewsAsync(a, id);
    public Task<SolicitudResponseDTO> InterviewResultAsync(Actor a, Guid id, ResultadoEntrevistaDTO d) => _adopcionWorkflow.InterviewResultAsync(a, id, d);
    public Task<List<SeguimientoResponseDTO>> FollowupsAsync(Actor a, Guid id) => _adopcionWorkflow.FollowupsAsync(a, id);
    public Task<SeguimientoResponseDTO> FollowupAsync(Actor a, Guid id, SeguimientoDTO d) => _adopcionWorkflow.FollowupAsync(a, id, d);
    public Task<SolicitudResponseDTO> FinalizeAsync(Actor a, Guid id, string reason) => _adopcionWorkflow.FinalizeAsync(a, id, reason);
    public Task<int> AbandonAsync() => _adopcionWorkflow.AbandonAsync();
    public Task<SolicitudResponseDTO> ValidateClinicalAsync(Actor a, Guid id, string reason) => _clinicaVeterinaria.ValidateClinicalAsync(a, id, reason);
    public Task<HistorialResponseDTO> CreateHistoryAsync(Actor a, Guid pet, ClinicaDTO d) => _clinicaVeterinaria.CreateHistoryAsync(a, pet, d);
    public Task<HistorialDetalleResponseDTO> HistoryAsync(Actor a, Guid pet) => _clinicaVeterinaria.HistoryAsync(a, pet);
    public Task<EventoMedicoResponseDTO> MedicalEventAsync(Actor a, Guid pet, EventoMedicoDTO d) => _clinicaVeterinaria.MedicalEventAsync(a, pet, d);
    public Task<List<SancionResponseDTO>> BlacklistAsync(Actor a) => _controlOperativo.BlacklistAsync(a);
    public Task<SancionResponseDTO> SaveBlacklistAsync(Actor a, Guid? id, BlacklistDTO d) => _controlOperativo.SaveBlacklistAsync(a, id, d);
    public Task RevokeBlacklistAsync(Actor a, Guid id, string reason) => _controlOperativo.RevokeBlacklistAsync(a, id, reason);
    public Task<IncidenteResponseDTO> IncidentAsync(Actor a, IncidenteDTO d) => _controlOperativo.IncidentAsync(a, d);
    public Task<List<IncidenteResponseDTO>> IncidentsAsync(Actor a) => _controlOperativo.IncidentsAsync(a);
    public Task<List<AuditoriaResponseDTO>> AuditAsync(Actor a, DateTime? desde, DateTime? hasta, string? accion, Guid? usuario) => _controlOperativo.AuditAsync(a, desde, hasta, accion, usuario);
    public Task<List<NotificacionResponseDTO>> NotificationsAsync(Actor a) => _controlOperativo.NotificationsAsync(a);
    public Task<List<string[]>> ReportAsync(Actor a, string tipo, DateTime desde, DateTime hasta) => _controlOperativo.ReportAsync(a, tipo, desde, hasta);
}
