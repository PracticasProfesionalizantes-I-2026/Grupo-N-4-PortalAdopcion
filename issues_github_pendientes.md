# Checklist de Hallazgos y Tareas Pendientes (GitHub Issues)

Este documento reúne todas las acciones correctivas resultantes de la auditoría integral (Diseño Documental, Implementación y Calidad de Código Estático), estructuradas como tareas accionables para ser trasladadas directamente a GitHub Issues o a un Project Board.

---

### 🔴 RIESGO ALTO (Crítico para la ejecución y completitud core)

- [ ] **[Backend / DataAccess] Eliminar el borrado de lock huérfano incondicional en arranque (`Program.cs`)**
  - **Archivo:** `API/Program.cs` (líneas 148-175, función `CleanupStaleMigrationLockAsync`)
  - **Problema:** Si el sistema escala horizontalmente a múltiples réplicas o pods en un entorno productivo con base compartida, el método puede interpretar la ejecución legítima de una migración en un nodo como un "lock huérfano" y ejecutar `DELETE FROM "__EFMigrationsLock"`, provocando migraciones concurrentes y corrupción del esquema de la base de datos.
  - **Acción correctiva:** Envolver la llamada a `CleanupStaleMigrationLockAsync` bajo la condición estricta `if (app.Environment.IsDevelopment() && db.Database.IsSqlite())` y registrar una alerta crítica (`LogLevel.Critical`) si se detecta en cualquier otro entorno.

- [ ] **[Seguridad / API] Exclusión estricta del endpoint de reinicio de base de datos en entornos no-Development (`ControlController.cs`)**
  - **Archivo:** `API/Controllers/PortalControllers.cs` (líneas 173-183, método `ResetDemo`)
  - **Problema:** El endpoint `POST /api/demo/reiniciar` tiene `[AllowAnonymous]` y purga la totalidad de las 12 tablas de base de datos (`repo.ResetDemoDataAsync()`). Aunque evalúa `IPAddress.IsLoopback` y `env.IsDevelopment()`, la presencia de un endpoint anónimo con capacidad de vaciado masivo en controladores principales representa un riesgo crítico de denegación de servicio (DoS) si un error de configuración de variables de entorno activa el perfil Development.
  - **Acción correctiva:** Mover el controlador de utilidades demo a un ensamblado exclusivo de desarrollo/testing o aislarlo con directivas condicionales del compilador `#if DEBUG` para garantizar que no se emita en compilaciones Release/Producción.

- [ ] **[Concurrencia / BusinessLogic] Condición de carrera en asignación de límite de voluntarios RN-16 (`PortalService.cs`)**
  - **Archivo:** `BusinessLogic/Services/PortalService.cs` (líneas 524-526)
  - **Problema:** La validación de que un voluntario no posea más de dos procesos activos simultáneos (`repo.CountAsync<SolicitudDeAdopcion>(x => x.VoluntarioId == voluntarioId && EstadosActivos.Contains(x.Estado))`) se realiza en memoria mediante un `SELECT COUNT(*)`. Dos administradores asignando al mismo voluntario de forma casi simultánea pueden superar el límite legal sin que la base de datos dispare un conflicto de unicidad o versión.
  - **Acción correctiva:** Implementar un bloqueo transaccional a nivel de entidad/fila o incorporar un campo de control de versión / contador atómico en la entidad o tabla de voluntario para que la segunda transacción colisione con `DbUpdateConcurrencyException`.

---

### 🟡 RIESGO MEDIO (Mantenibilidad y Flujos Secundarios)

- [ ] **[Arquitectura / Refactor] Descomponer la clase Dios `PortalService` (Violación de SRP)**
  - **Archivo:** `BusinessLogic/Services/PortalService.cs` (870 líneas) y `BusinessLogic/Abstractions/IPortalService.cs`
  - **Problema:** La clase `PortalService` concentra todas las operaciones del sistema (usuarios, postulaciones, entrevistas, historial clínico, incidentes, auditoría y reportes), violando el principio de Responsabilidad Única (SRP) y convirtiendo el servicio en un cuello de botella de mantenibilidad y pruebas.
  - **Acción correctiva:** Dividir `PortalService` en servicios cohesivos orientados al dominio:
    1. `IAdopcionWorkflowService` (Postulaciones, Evaluación, Entrevistas, Seguimiento, Cancelación, Finalización).
    2. `IMascotaService` (Catálogo, altas, fichas y estados).
    3. `IClinicaVeterinariaService` (Historial inicial, eventos médicos y validación veterinaria).
    4. `IUsuarioAuthService` (Registro, login, administración de roles y perfiles).
    5. `IControlOperativoService` (Blacklist, auditoría, notificaciones e incidentes).

- [ ] **[Arquitectura / Controllers] Modularizar controladores agrupados en `PortalControllers.cs`**
  - **Archivo:** `API/Controllers/PortalControllers.cs` (196 líneas)
  - **Problema:** Todos los controladores de la solución (`AuthController`, `UsuariosController`, `AdoptantesController`, `MascotasController`, `BlacklistController`, `SolicitudesController`, `ControlController`) conviven en un único archivo fuente, complicando la trazabilidad de Git y el trabajo colaborativo en equipo.
  - **Acción correctiva:** Separar cada controlador en su propio archivo físico dentro del directorio `API/Controllers/` (`MascotasController.cs`, `SolicitudesController.cs`, etc.).

- [ ] **[Gap Funcional / API] Incorporar filtros de búsqueda por especie, sexo y edad en Catálogo (`MascotasController.cs`)**
  - **Archivo:** `API/Controllers/PortalControllers.cs` (líneas 63-66) y `BusinessLogic/Services/PortalService.cs` (líneas 184-188)
  - **Problema:** El requerimiento funcional original (RF 5.1 y CU-09) exige filtrado y búsqueda de mascotas por especie, edad, sexo y estado. Actualmente el endpoint `GET /api/mascotas` solo admite el parámetro `estado`, obligando a transferir toda la colección al frontend para que este filtre en memoria.
  - **Acción correctiva:** Ampliar la firma de `PetsAsync` para recibir `(EstadoMascotaEnum? estado, EspecieEnum? especie, SexoEnum? sexo, string? busqueda)` y componer dinámicamente el predicado `Expression<Func<Mascota, bool>>` en la consulta EF.

- [ ] **[Documentación vs Código] Alinear código de estado HTTP en rechazo por Blacklist (`CU-01`, `CU-02`)**
  - **Archivos:** `CASOS DE USO/cu-01-gestionar-blacklist-de-usuarios.md`, `CASOS DE USO/cu-02-crear-solicitud-de-adopcion.md` vs `BusinessLogic/Services/PortalService.cs` (línea 402)
  - **Problema:** La especificación de los casos de uso documenta que cuando un usuario sancionado intenta postularse, la API debe responder `409 Conflict`. Sin embargo, `PortalService` arroja `AccessException`, lo que el middleware traduce a `403 Forbidden`.
  - **Acción correctiva:** Decidir si la semántica REST adoptada es `403 Forbidden` (recomendada) o `409 Conflict` y homogeneizar la tabla de códigos HTTP y flujos alternativos en la documentación de `CU-01` y `CU-02`.

- [ ] **[Documentación / Corrección] Corregir la precondición contradictoria en especificación de `CU-03`**
  - **Archivo:** `CASOS DE USO/cu-03-registrar-historial-clinico-inicial.md` (Sección 2 - Precondiciones)
  - **Problema:** La precondición estipula que la ficha clínica debe existir previamente, lo cual contradice el propósito de `CU-03`, que es precisamente registrar por primera vez el historial médico inicial (`HistorialClinico` + `EventoMedico` Inicial).
  - **Acción correctiva:** Ajustar la precondición para indicar: *"La mascota debe estar registrada en el sistema y no poseer historial clínico previo (CU-03 es el acto de apertura del historial)"*.

- [ ] **[Rendimiento / Infraestructura] Sustituir `SmtpClient` obsoleto y reutilizar conexiones en `CorreoJob`**
  - **Archivo:** `API/CorreoJob.cs` (líneas 30-36)
  - **Problema:** `System.Net.Mail.SmtpClient` está deprecado en plataformas modernas de .NET y además se instancia dentro de un bucle `foreach` por cada correo en cola, abriendo y cerrando handshakes TLS/TCP contra el servidor SMTP innecesariamente.
  - **Acción correctiva:** Migrar a la biblioteca recomendada `MailKit` / `MimeKit` o inyectar un cliente SMTP configurado como Singleton/Scoped para reutilizar el pool de conexiones del socket de red.

---

### 🟢 RIESGO BAJO (Deuda técnica y Mejoras Menores)

- [ ] **[Validación / DTOs] Validación estricta de formato URL para fotografías de mascotas (`MascotaFichaDTO`)**
  - **Archivo:** `Shared/DTOs/PortalContracts.cs` (línea 12)
  - **Problema:** La propiedad `FotoUrl` acepta cualquier cadena de texto arbitraria mientras no supere la longitud máxima. Si el usuario ingresa un esquema no seguro (`javascript:...` o URLs malformadas), puede causar problemas de presentación en el cliente.
  - **Acción correctiva:** Añadir la data annotation `[Url]` y validar que `Uri.TryCreate(d.FotoUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)` antes de persistir.

- [ ] **[Código Limpio / Shared] Evitar "magic strings" en estados de usuario y canales de notificación**
  - **Archivos:** `BusinessLogic/Services/PortalService.cs` y `API/CorreoJob.cs`
  - **Problema:** Se utilizan cadenas literales sin tipar para los estados de usuario (`"Activa"`, `"Baja"`, `"Suspendida"`), canales (`"Interna"`, `"CorreoBccPendiente"`, `"CorreoBccEnviado"`) y niveles de gravedad (`"Normal"`, `"Urgente"`).
  - **Acción correctiva:** Definir enums formales en `Shared/Enums/`: `EstadoUsuarioEnum`, `CanalNotificacionEnum` y `GravedadIncidenteEnum`.

- [ ] **[Consistencia de API] Eliminar parámetro `motivo` en DELETE que duplica la ruta PATCH (`MascotasController.cs`)**
  - **Archivo:** `API/Controllers/PortalControllers.cs` (líneas 78-83)
  - **Problema:** `DELETE /api/mascotas/{id}?motivo=...` invoca internamente a `s.ChangePetAsync(Actor, id, new(EstadoMascotaEnum.Inactiva, motivo))`. Esta semántica es redundante con `PATCH /api/mascotas/{id}/estado`.
  - **Acción correctiva:** Documentar formalmente el método `DELETE` como un alias de baja lógica en el OpenAPI / Scalar spec o estandarizar el flujo hacia el verbo `PATCH`.

- [ ] **[Seguridad / Defense-in-depth] Forzar configuración obligatoria de clave JWT en Testing (`Program.cs`)**
  - **Archivo:** `API/Program.cs` (líneas 52-57)
  - **Problema:** Si bien el fallback a claves fijas se restringe a ambientes no productivos, es una buena práctica de hardening que la clave provenga siempre de variables de entorno o User Secrets incluso en pruebas.
  - **Acción correctiva:** Emitir un warning explícito en la consola durante el arranque si se está utilizando una clave por defecto en modo local.

- [ ] **[Documentación / Arquitectura] Homogeneizar menciones a `CU-05` en la documentación general**
  - **Archivos:** `CASOS DE USO/cu-05-crear-proceso-de-adopcion.md` y `COSAS/Pendientes-Alta-Prioridad.md`
  - **Problema:** En algunos apartados `CU-05` figura como caso de uso independiente y en otros como subfunción absorbida por `CU-02`.
  - **Acción correctiva:** Incluir un banner informativo visible al inicio de `cu-05-crear-proceso-de-adopcion.md` indicando explícitamente: *"Nota de arquitectura: Implementado de forma atómica en el endpoint POST /api/solicitudes (CU-02) bajo el principio de entidad única SolicitudDeAdopcion"*.

