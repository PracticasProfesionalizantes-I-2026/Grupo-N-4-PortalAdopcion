namespace PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.DataAccess.Entities;

// REFACTOR: Postulaciones, evaluación, entrevistas, seguimiento, cancelación, finalización y abandono.
public interface IAdopcionWorkflowService
{
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
    Task<SolicitudResponseDTO> FinalizeAsync(Actor a, Guid id, string reason);
    Task<int> AbandonAsync();
}
