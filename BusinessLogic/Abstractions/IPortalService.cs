namespace PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.DataAccess.Entities;


// REFACTOR: Fachada de compatibilidad. Agrupa los cinco servicios de dominio; se conserva solo porque
// los tests existentes construyen PortalService directamente. El código de producción usa las interfaces específicas.
public interface IPortalService : IUsuarioAuthService, IMascotaService, IAdopcionWorkflowService, IClinicaVeterinariaService, IControlOperativoService
{
}
