# Trazabilidad de la copia de prueba

Todos los métodos indicados, salvo TokenIssuer y los jobs, están en `BusinessLogic/Services/PortalService.cs`. Los controladores están en `API/Controllers/PortalControllers.cs`; los contratos en `Shared/DTOs/PortalContracts.cs`.

| CU | Negocio | HTTP | Verificación |
|---|---|---|---|
| CU-01 | SaveBlacklistAsync / RevokeBlacklistAsync | BlacklistController | BlacklistBloquea, BlacklistRechazaActivo |
| CU-02 | PostulateAsync | SolicitudesController | Reserva, DuplicadoActivo, HttpIdentidad |
| CU-03 | CreateHistoryAsync | MascotasController | HistorialUnico |
| CU-04 | EvaluateAsync | SolicitudesController | AprobacionPrematura, AprobacionNoAdopta, Observacion |
| CU-05 | PostulateAsync (subfunción de CU-02) | SolicitudesController, mismo POST | Reserva |
| CU-06 | RegisterAsync / CreateStaffAsync | AuthController / UsuariosController | RegistroNormalizado, RegistroDuplicado, HttpRegistroSeguro |
| CU-07 | LoginAsync; TokenIssuer; validación JWT en Program | AuthController | LoginIncorrecto, HttpLoginPermisos |
| CU-08 | ChangeUserAsync | UsuariosController | UltimoAdministrador, EstadoInvalidaToken, PersonalMatricula |
| CU-09 | CreatePetAsync / UpdatePetAsync / ChangePetAsync | MascotasController | PermisosCatalogo, EnumInvalido, HttpContratoEstricto |
| CU-10 | MedicalEventAsync | MascotasController | EventoMedicoPersistido |
| CU-11 | ScheduleAsync / InterviewResultAsync | SolicitudesController | EntrevistaPrueba |
| CU-12 | FollowupAsync / ValidateClinicalAsync | SolicitudesController | SeguimientoDiarioUnico, AlertaBloquea, ValidacionFinalizacion |
| CU-13 | CancelAsync | SolicitudesController | CancelarLibera, SancionEnPrueba |
| CU-14 | AbandonAsync / AbandonoJob | ControlController (manual solo demo) | AbandonoIdempotente |
| CU-15 | FinalizeAsync / ClinicalAccess | SolicitudesController | TransferenciaHistorial, ValidacionFinalizacion |
| CU-16 | ReportAsync / ReportFiles | ControlController | ReporteVacio, PdfValido, CsvSeguro |
| CU-17 | AuditAsync / Save (auditoría transversal) | ControlController | AuditoriaCambio |
| CU-18 | IncidentAsync / IncidentsAsync | ControlController | IncidenteUrgente |

La matriz distingue pruebas existentes de recorridos manuales; no afirma que todos los flujos alternativos tengan cobertura exhaustiva. CU-05 comparte la operación de CU-02.
