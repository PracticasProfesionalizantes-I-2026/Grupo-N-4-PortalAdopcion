# Índice de Casos de Uso y Trazabilidad — Portal de Adopción

> **Regenerado el 10/09/2026.** Catálogo consolidado de los 18 casos de uso del sistema (CU-01..CU-18), con actor, acción, resultado esperado, reglas de negocio disparadas, códigos HTTP y matriz de trazabilidad a tests.
> **Estado de los archivos:** los `.md` individuales de `CASOS DE USO/` se perdieron del disco. Este índice permite regenerarlos fielmente; el detalle por CU puede reconstruirse a partir de estas tablas.

---

## 1. Actores del sistema

| Actor | Descripción | Símbolo |
| --- | --- | --- |
| Administrador | Usuario con permisos globales (usuarios, roles, blacklist, reportes, auditoría) | `[ADMIN]` |
| Voluntario | Personal de la ONG que evalúa, entrevista y hace seguimiento | `[VOL]` |
| Veterinario | Personal con acceso al historial clínico | `[VET]` |
| Adoptante | Potencial adoptante que se registra y postula | `[ADOPT]` |
| Sistema | Actor automático (jobs, validaciones) | `[SYS]` |

---

## 2. Catálogo completo (CU-01..CU-18)

| ID | Nombre | Actor | Acción | Resultado esperado | Endpoint sugerido | RN |
| --- | --- | --- | --- | --- | --- | --- |
| CU-01 | Gestionar Blacklist | `[ADMIN]` | Agregar/modificar/eliminar sanción | Sanción aplicada/actualizada (200/204) | `POST/PUT/DELETE /api/blacklists` | RN-01, RN-02, RN-15 |
| CU-02 | Crear Solicitud de Adopción | `[ADOPT]` | Postularse a una mascota | Solicitud en `Pendiente` (201) | `POST /api/solicitudes` | RN-01, RN-03, RN-04, RN-14 |
| CU-03 | Registrar Historial Clínico Inicial | `[VET]` | Crear ficha clínica de la mascota | Historial único (201) | `POST /api/mascotas/{id}/historial` | RN-09 (unicidad) |
| CU-04 | Evaluar Solicitud | `[ADMIN]`/`[VOL]` | Aprobar/rechazar/dejar en observación | Estado evaluado (200) | `PATCH /api/solicitudes/{id}/evaluacion` | RN-05, RN-06, RN-01 |
| CU-05 | Crear Proceso (subfunción de CU-02) | `[SYS]` | Crear proceso en `Pendiente` automáticamente | Proceso creado (201) | interno a CU-02 | RN-03, RN-04, RN-07, RN-08 |
| CU-06 | Registrar Usuario | `[ADOPT]`/`[ADMIN]` | Alta de cuenta (4 roles) | Cuenta activa con rol (201) | `POST /api/usuarios` | RN-17 |
| CU-07 | Iniciar Sesión | `[ADOPT]`/`[ADMIN]`/`[VOL]`/`[VET]` | Autenticarse | Token JWT + rol (200) | `POST /api/auth/login` | RN-17, RN-01 |
| CU-08 | Gestionar Usuarios y Roles | `[ADMIN]` | Modificar rol/estado de cuentas | Actualización persistida (200/204) | `PUT /api/usuarios/{id}/rol` | RN-17, RN-08 |
| CU-09 | Gestionar Catálogo de Mascotas | `[ADMIN]`/`[VOL]` | Alta/edición/estado/baja de mascotas | Ficha persistida (201/200/204) | `POST/PUT/PATCH/DELETE /api/mascotas` | RN-04, RN-17, RN-08 |
| CU-10 | Registrar Evento Médico | `[VET]` | Agregar evento al historial | Evento persistido (201) | `POST /api/mascotas/{id}/historial/eventos` | RN-09, RN-08, RN-13 |
| CU-11 | Programar/Realizar Entrevista | `[ADMIN]`/`[VOL]` | Programar, registrar resultado | Proceso → `En Periodo de Prueba` o `Rechazada` (201/200) | `POST /api/solicitudes/{id}/entrevistas` | RN-05/06, RN-09 |
| CU-12 | Seguimiento del Periodo de Prueba | `[VOL]` | Registro diario (ánimo, comida, tareas) | Seguimiento + alerta al vet (201) | `POST /api/procesos/{id}/seguimientos` | RN-12, RN-13, RN-08, RN-16 |
| CU-13 | Cancelar Proceso | `[ADOPT]`/`[ADMIN]` | Cancelar con motivo obligatorio | `Cancelada` (200); si es en prueba → blacklist 1 año | `POST /api/solicitudes/{id}/cancelar` | RN-11, RN-12, RN-08 |
| CU-14 | Timeout Automático | `[SYS]` | Job diario: `En Entrevista` > 7 días | Procesos `Abandonada` (200) | job interno | RN-10, RN-08 |
| CU-15 | Finalizar Adopción y Transferir Historial | `[ADMIN]`/`[VOL]` | Cierre de adopción | Mascota `Adoptada` + historial clonado (200) | `POST /api/solicitudes/{id}/finalizar` | RN-13, RN-08 |
| CU-16 | Generar/Exportar Reportes | `[ADMIN]` | Reportes/estadísticas | Archivo CSV/PDF (200) | `GET /api/reportes` | RN-17, RN-08 |
| CU-17 | Consultar Auditoría | `[ADMIN]` | Logs filtrados | Resultados paginados (200) | `GET /api/auditoria` | RN-08, RN-17 |
| CU-18 | Reportar Incidente | `[VOL]` | Registrar incidente (urgente/normal) | Incidente persistido (201) | `POST /api/incidentes` | RN-16, RN-17, RN-08 |

---

## 3. Conjunto de códigos HTTP por semántica

| Código | Semántica | CU donde se usa |
| --- | --- | --- |
| 201 | Recurso creado | 02, 03, 05, 06, 09, 10, 11, 12, 18 |
| 200 | Operación de actualización/lectura exitosa | 04, 07, 08, 09, 11, 13, 14, 15, 16, 17 |
| 204 | Eliminación/actualización sin cuerpo | 01, 08, 09 |
| 400 | Validación fallida (campos, contraseña, fechas, motivo vacío) | 06, 09, 10, 11, 12, 13, 16, 17, 18 |
| 401 | No autenticado / credenciales inválidas | 02, 07 |
| 403 | No autorizado / cuenta suspendida / tarea no asignada | 06, 08, 12, 18 |
| 404 | Recurso inexistente | 01, 03, 04, 08, 10, 11, 13, 15, 17 |
| 409 | Conflicto de dominio o concurrencia (duplicados, estado inválido, blacklist) | 02, 04, 06, 08, 09, 11, 13, 15 |
| 500 | Error no controlado | genérico |

---

## 4. Matriz de trazabilidad CU ↔ Tests / Reglas

Cada CU detallado definió tests unitarios (Moq) y de integración (WebApplicationFactory + SQLite in-memory). Patrón esperado por CU:

| Capa | Qué se traza | Ejemplos por CU |
| --- | --- | --- |
| Unit tests (Service) | Reglas de negocio y excepciones | `SolicitudService_ConBlacklist_LanzaExcepcion` (CU-02), `BlacklistService_CancelacionPrueba_Sanciona1Anio` (CU-13), `TimeoutService_Entrevista7Dias_Abandona` (CU-14) |
| Integration tests (API) | Códigos HTTP y contratos | `POST /api/usuarios_CreaCuenta_Returns201` (CU-06), `PATCH evaluacion_Returns200` (CU-04), `POST cancelar_SinMotivo_Returns400` (CU-13) |
| Contrato de pruebas | Correo BCC (RN-03/RN-07), única solicitud activa (RN-04), auditoría (RN-08) | verificados por CU: 02, 04, 05, 06, 08, 13, 14, 15, 17 |

---

## 5. Cobertura: faltantes, duplicados y mal cubiertos

| Tipo | Ítem | Acción recomendada |
| --- | --- | --- |
| **Duplicado** | CU-02 ↔ CU-05 (creación en `Pendiente`) | Fusionar CU-05 en CU-02 (ver `Pendientes-Alta-Prioridad.md` P1) |
| **Faltante** | Cierre de sesión (logout / invalidar JWT) | Agregar CU o subflujo de CU-07 |
| **Faltante** | Recuperación de contraseña ("¿Olvidó su contraseña?") | Agregar CU |
| **Faltante** | Gestión de tareas de voluntarios (panel "TAREAS DISPONIBLES", máx 2 activas) | Agregar CU; confirmar alcance RN-16 |
| **Faltante** | Selección de rol en pantalla de inicio | Mapear explícitamente (asumida en CU-06/CU-07) |
| **Mal cubierto** | CU-03 precondición contradictoria | Corregir (ver P4) |
| **Mal cubierto** | CU-01..05 con RN antiguas | Renumerar (ver P2) |
| **No definido** | Cancelación/reprogramación de entrevista | Mover a flujo alternativo propio de CU-11 |

---

## 6. Notas para regenerar los `.md` individuales

Si se regeneran los 13 CU nuevos (cu-06..cu-18) usando la plantilla de `Documentos EJEMPLO/GUIA-Especificacion-Casos-de-Uso.md`, cada archivo debe incluir:
1. Tabla de encabezado (ID, nombre, actores, precondiciones, postcondiciones).
2. Flujo principal numerado con paso HTTP (método + ruta + request/response).
3. Flujos alternativos (400/401/403/404/409) referenciados desde el paso que los dispara.
4. Validación (capa presentación → 400) vs. Verificación (capa negocio → 404/409).
5. Matriz de trazabilidad a tests (unitarios + integración) y a RN unificadas.