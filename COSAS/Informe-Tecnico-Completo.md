# Informe Técnico Completo — Portal de Adopción de Mascotas para ONGs

> **Grupo N° 4** (Giordano Catalina, Ledesma Francisco)
> Análisis técnico y arquitectónico. Generado el 10/09/2026.
> **Aclaración de procedencia:** este informe consolida el análisis de la documentación original (spec de `BUSINESS`, mockups Excalidraw, casos de uso de `CASOS DE USO` y la guía de `Documentos EJEMPLO`). Los archivos originales desaparecieron del disco el 10/09/2026; los extractos de texto de la spec y de ACTORES quedan respaldados en esta misma carpeta.

---

## 1. CONTEXTO Y RESUMEN GENERAL

### Qué hace el sistema
**Portal de Adopción de Mascotas para ONGs.** Digitaliza el flujo completo de adopción que la ONG maneja hoy por redes/mensajería: registro de animales y adoptantes, postulación, evaluación, entrevista, periodo de prueba y adopción final, con control clínico, blacklist de usuarios, auditoría y reportes.

### Tecnologías, frameworks y arquitectura detectados
Detección indirecta (a partir de `Documentos EJEMPLO/GUIA-Especificacion-Casos-de-Uso.md` y los CU):

| Capa | Tecnología esperada | Evidencia |
| --- | --- | --- |
| Presentación | ASP.NET Core Web API, Controllers + DTOs, JWT, Swagger/Scalar | GUIA §3/§6; CU referencian `UsuariosController`, `SolicitudesController` |
| Negocio | Services (e.g. `BlacklistService`, `SolicitudService`) + excepciones de dominio | CU-01..CU-05 |
| Persistencia | EF Core (implícito), SQLite in-memory en tests, `DbInitializer` | GUIA §7.2 |
| Tests | xUnit + Moq (unitarios) y `WebApplicationFactory` (integración) | GUIA §7 |
| Cliente API | Bruno / Postman | SUB-VARIACIONES de múltiples CU |

Arquitectura declarada: **3 capas (Presentación / Negocio / Persistencia)**, REST con semántica HTTP (200/201/204/400/401/403/404/409/500) y separación Validación (capa presentación → 400) vs. Verificación (capa negocio → 404/409).

### Módulos o componentes principales
1. **Identidad y accesos** — registro (CU-06), login (CU-07), gestión de usuarios y roles (CU-08).
2. **Blacklist y sanciones** — CU-01.
3. **Catálogo de mascotas** — CU-09.
4. **Historial clínico** — CU-03 / CU-10.
5. **Solicitudes y procesos de adopción** (workflow) — CU-02/CU-05/CU-13/CU-14.
6. **Evaluación y entrevistas** — CU-04 / CU-11.
7. **Periodo de prueba y seguimiento** — CU-12 / CU-15.
8. **Reportes, estadísticas y auditoría** — CU-16 / CU-17.
9. **Incidentes** — CU-18.

> **Información faltante:** no existe código, modelo de base de datos ni diagramas de clases/secuencia; tampoco el diagrama de flujo de estados que el requerimiento exige explícitamente. Esto limita la sección de lógica y riesgos a nivel de contrato.

---

## 2. DIFERENCIAS DETECTADAS

No hay dos versiones de código para comparar; la comparación es entre **artefactos** (requerimientos vs. mockups vs. casos de uso) y entre **antes** (CU-01..05 + documento original) y **después** (análisis + CU-06..18 + modelo unificado).

| # | Antes | Después | Impacto | Clasificación |
| --- | --- | --- | --- | --- |
| 1 | RN duplicadas: RN-03/RN-04 (CU-02) vs. RN-18.2/18.3 (CU-05); RN-18.1/18.5 sin par | Catálogo unificado RN-01..RN-17 con tabla de correspondencia | Trazabilidad RN↔CU única y consistente | Inconsistencia corregida (mejora) |
| 2 | "Solicitud" y "Proceso" como dos entidades con mismo estado inicial y validaciones | Entidad única `SolicitudDeAdopcion`; CU-05 = subfunción de CU-02 | Evita duplicar lógica, endpoints y tests | Inconsistencia → decisión de diseño |
| 3 | FR sin CU (usuarios, mascotas, eventos, cancelación, timeout, transferencia de historial, reportes, auditoría) | 13 CU nuevos (CU-06..CU-18) con matriz de trazabilidad a tests | Cobertura de requerimientos completa a nivel de contrato | Cierre de brecha (mejora) |
| 4 | Workflow con estados implícitos y transiciones no definidas | Modelo de estados y transiciones válidas (ver `Modelo-de-Estados-y-Reglas-de-Negocio.md`) | Base para implementar validación de transiciones atómicas | Mejora |
| 5 | Mockup: blacklist impide iniciar sesión | Spec: solo impide postular; se permite login restringido | Cambio de comportamiento vs. mockup (decisión documentada) | Decisión de alcance |
| 6 | Out-of-scope: "sin notificaciones automáticas" vs. mockups con correos | Notificaciones in-app + auditoría; sin correo en MVP | Elimina contradicción | Decisión de alcance |
| 7 | Periodo de prueba sin duración definida | 15 días por defecto | Cálculo de días y regla de bloqueo (RN-12) determinista | Decisión de diseño |
| 8 | CU-03 precondición "la ficha clínica debe existir" contradice la creación del historial | Detectado y señalado | Evita un bug de implementación | Error detectado, sin corregir |
| 9 | Estados "En Observación" (solo en CU-04) y "Abandonado" (timeout) sin representación clara | Modelados en la máquina de estados y en CU-11/CU-14 | Estado terminal "Abandonada" ahora tiene su pool de tests | Inconsistencia corregida |
| 10 | Contratos HTTP implícitos en CU existentes | CU nuevos estandarizan verbos (POST/PUT/PATCH/DELETE) y códigos | API REST consistente, base para colección Bruno | Mejora de consistencia |

**Intencionales (mejoras):** #1, #2, #3, #4, #7, #10.
**Inconsistencias/errores:** #8 (aún abierto), #9, y la contradicción de notificaciones (#6, resuelta por decisión).
**Comportamiento cambiado:** #5 (respecto del mockup).

---

## 3. CASOS DE USO

### 3.1 Catálogo completo

| CU | Actor | Acción | Resultado esperado | RN disparadas |
| --- | --- | --- | --- | --- |
| CU-01 | Administrador | Gestionar blacklist (agregar/modificar/eliminar sanción) | Usuario bloqueado para postular; sanción auditada | RN-01, RN-02 |
| CU-02 | Adoptante | Crear solicitud de adopción | Solicitud en "Pendiente" (201) | RN-01, RN-03, RN-04, RN-14 |
| CU-03 | Veterinario | Registrar historial clínico inicial | Historial único por mascota (201) | RN-09(unicidad) |
| CU-04 | Admin/Voluntario | Evaluar/autorizar adopción | Aprobada / Rechazada / En Observación (200) | RN-05, RN-06, RN-01 |
| CU-05 | Sistema | Crear proceso en estado Pendiente (subfunción de CU-02) | Proceso creado (201) | RN-03, RN-04, RN-07, RN-08 |
| CU-06 | Usuario nuevo / Admin | Registrar usuario (4 roles) | Cuenta activa con rol (201) | RN-17, unicidad email/DNI/matrícula |
| CU-07 | Usuario registrado | Iniciar sesión | Token JWT + rol (200) | RN-17, RN-01 |
| CU-08 | Administrador | Gestionar usuarios y roles | Rol/estado actualizados (200/204) | RN-17, RN-08 |
| CU-09 | Admin/Voluntario | Gestionar catálogo de mascotas (alta/edición/estado/baja) | Ficha persistida (201/200/204) | RN-04, RN-17, RN-08 |
| CU-10 | Veterinario | Registrar evento médico | Evento en historial (201) | RN-09, RN-08, RN-13 |
| CU-11 | Admin/Voluntario | Programar y realizar entrevista | Proceso → Prueba o Rechazada (201/200) | RN-05/06, RN-09 |
| CU-12 | Voluntario | Seguimiento diario del periodo de prueba | Seguimiento + alertas al vet (201) | RN-12, RN-13, RN-08, RN-16 |
| CU-13 | Adoptante/Personal | Cancelar proceso (motivo obligatorio) | Cancelada + posible sanción 1 año (200) | RN-11, RN-12, RN-08 |
| CU-14 | Sistema | Timeout 7 días "En Entrevista" → "Abandonada" | Procesos liberados (200) | RN-10, RN-08 |
| CU-15 | Admin/Voluntario | Finalizar adopción y transferir historial | Mascota Adoptada + historial clonado (200) | RN-13, RN-08 |
| CU-16 | Administrador | Generar/exportar reportes | Archivo CSV/PDF (200) | RN-17, RN-08 |
| CU-17 | Administrador | Consultar auditoría | Logs filtrados (200) | RN-08, RN-17 |
| CU-18 | Voluntario | Reportar incidente | Incidente registrado (urgente o normal) (201) | RN-16, RN-17, RN-08 |

Ver detalle ampliado con códigos HTTP, alternativos y matriz de trazabilidad en `Indice-Casos-de-Uso-y-Trazabilidad.md`.

### 3.2 Casos alternativos y bordes definidos
- **400 (validación):** campos incompletos/JSON inválido (CU-06/09/10/12/13/18), contraseña débil (CU-06), rango de fechas (CU-16/17), motivo vacío (CU-13), fecha/hora inválida (CU-11).
- **401/403 (autenticación/autorización):** sin token (CU-02/CU-07), credenciales incorrectas, cuenta dada de baja, sin rol requerido, tarea no asignada (CU-12/18), clave de invitación inválida (CU-06).
- **404 (existencia):** usuario/mascota/solicitud/proceso/historial inexistentes.
- **409 (dominio/concurrencia):** duplicados (email/DNI/matrícula/mascota), usuario ya bloqueado, solicitud ya evaluada, mascota no disponible, proceso activo existente, blacklist, seguimiento duplicado del día, baja de último admin, concurrencia (CU-02/11/13/15).
- **Bordes temporales:** timeout exactamente a los 7 días (CU-14), 15 días del periodo de prueba (CU-12/15).
- **Comportamiento de rol:** cuenta suspendida entra pero no postula (CU-07), blacklist durante evaluación → Rechazada automática (CU-04).

### 3.3 Cobertura: faltantes, duplicados y mal cubiertos
| Tipo | Ítem | Detalle |
| --- | --- | --- |
| **Duplicado** | CU-02 ↔ CU-05 | Mismo flujo de creación ("Pendiente") con mismas RN; recomendado fusionar CU-05 en CU-02 |
| **Faltante** | Cierre de sesión (logout) | Hay login (CU-07) pero no logout/invalidación de token |
| **Faltante** | Recuperación de contraseña | El mockup tiene "¿Olvidó su contraseña?" sin CU |
| **Faltante** | Gestión de tareas de voluntarios | Panel "TAREAS DISPONIBLES Y ASIGNACIÓN DE VOLUNTARIOS" (máx 2) sin CU propio |
| **Faltante** | Selección de rol en el inicio | Pantalla "Seleccione su rol" del mockup no mapeada explícita |
| **Mal cubierto** | CU-03 | Precondición contradictoria (la ficha clínica que el CU crea se supone existente) |
| **Mal cubierto** | CU-01..CU-05 | Referencian RN-01..06 / RN-18.x; no renumerados al catálogo RN-01..17 |
| **No definido** | Cancelación/reprogramación de entrevista | Solo subvariación de CU-11 sin flujo alternativo propio |

---

## 4. DESGLOSE DE LÓGICA DE NEGOCIO ("baja de lógica")

> Base: contratos extraídos de los CU; **no hay código que verificar**. Complejidad a nivel de contrato. Ver el detalle completo en `Modelo-de-Estados-y-Reglas-de-Negocio.md`.

| Componente (service derivado) | CU | Entradas | Salidas | Reglas de negocio clave | Dependencias | Excepciones | Complejidad |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `UsuarioService` | 06, 08 | Datos personales, rol, invitación, contraseña | Cuenta creada/actualizada | Unicidad email/DNI/matrícula; política de contraseña; no degradar último admin | Auth, Auditoria | Email/Documento/MatriculaDuplicada, Invite, Validation | Media (validaciones cruzadas entre roles) |
| `AuthService` | 07 | Email/usuario + contraseña | Token JWT + claims de rol | Verificación hash; estados de cuenta; login permitido si suspendida | Usuario, Auditoria | InvalidCredentials, AccountDeactivated, UserNotFound | Media (seguridad) |
| `BlacklistService` | 01, 12/13 | DNI/usuario, motivo, duración | Sanción activa | RN-01/RN-02/RN-15; bloqueo automático 1 año por cancelación en prueba | Usuario, Auditoria | SanctionException, Validation, UserNotFound | Media-alta (regla automática + manual) |
| `MascotaService` | 09 | Ficha (especie, sexo, edad, foto, estado) | Mascota registrada/actualizada | RN-04; transiciones de estado de mascota; no baja con proceso activo | Persistencia, Auditoria | MascotaDuplicada, Estado, ProcesoActivo, NotFound | Media |
| `HistorialClinicoService` | 03, 10 | Datos clínicos iniciales / evento | Historial/evento persistido | Unicidad historial; profesional bloqueado por sesión | Mascota, Auditoria | HistorialExistente/NoCreado, Validation | Media |
| `SolicitudService` | 02, 13 | mascotaId / motivo-cancelación | Solicitud Pendiente / Cancelada | RN-03/RN-04/RN-14/RN-11/RN-12; concurrencia | Mascota, Usuario, Blacklist, Auditoria | MascotaNoDisponible, Blacklist, Concurrency | Alta (múltiples invariantes + concurrencia) |
| `EvaluacionService` | 04, 11 | Resultado, observaciones, motivo | Aprobada/Rechazada/En Observación/Entrevista | RN-05/RN-06/RN-09; decisiones auditadas | Solicitud, Mascota, Blacklist | SolicitudNoEncontrada, YaEvaluada | Media-alta |
| `SeguimientoService` | 12 | Seguimiento diario (ánimo, comida, tareas) | Registro + alerta al vet | RN-13; único por día; RN-16 (tarea asignada) | Proceso, Voluntario, Auditoria | DuplicadoDia, Estado, Validation | Media |
| `TimeoutService` | 14 | N/A (job diario) | Batch de procesos abandonados | RN-10; >7 días; atómico; 1 corrida/día | Proceso, Auditoria | Concurrency, Persist | Media (concurrencia y atomicidad) |
| `AdopcionService` | 15 | Confirmación de cierre | Proceso+mascota Adoptada; historial clonado | RN-13; seguimiento aprobado; clonado íntegro | Historial, Proceso, Auditoria | Estado, Seguimiento, CloneFail | Alta (transacción multi-entidad) |
| `ReporteService` | 16 | tipo, desde, hasta, formato | Archivo CSV/PDF | RN-17; rangos válidos; reporte vacío tolerado | Uso de datos, Auditoria | Validation | Baja-media |
| `AuditoriaService` | 17 | rango, nivel, tipo | Logs filtrados | RN-08; solo lectura; solo admin | Persistencia | Validation | Baja |
| `IncidenteService` | 18 | tipo, gravedad, detalle | Incidente + evento urgente | RN-16/RN-17/RN-08; urgente = máx prioridad | Usuario, Auditoria | Validation | Baja |

Notas transversales:
- **Concurrencia** recurrente (CU-02/11/13/15): transacciones atómicas con detección de conflicto (→ 409).
- **Auditoría** (RN-08) es dependencia transversal: conviene interceptor/middleware en vez de invocarla por service.

---

## 5. PROBLEMAS Y RIESGOS DETECTADOS

Sobre el contrato/documentación (no hay código):

| Gravedad | Problema | Tipo |
| --- | --- | --- |
| Alta | Duplicación funcional CU-02 ↔ CU-05 | Coherencia de especificación |
| Alta | RN renumeradas solo en los CU nuevos; CU-01..05 siguen con RN viejas/RN-18.x | Mantenibilidad |
| Alta | Falta el diagrama de flujo de estados requerido explícitamente | Requisito sin entregable |
| Alta | Precondición contradictoria en CU-03 | Error de especificación |
| Media | Contradicción notificaciones (out-of-scope vs. mockups con correos) — requiere aval | Requerimiento ambiguo |
| Media | Logout y recuperación de contraseña sin CU | Cobertura incompleta |
| Media | RN-16 "máx 2 tareas por voluntario" sin CU de gestión de tareas ni confirmación de alcance | Requerimiento sin respaldo |
| Media | NFR (consultas < 2 s, job diario) sin tests/trazabilidad asociados | Verificabilidad |
| Baja | Conflictos mockup ↔ spec (login blacklist, periodo de prueba) a validar | Decisiones a validar |
| Baja | Estado "En Observación" sin UI en mockups — riesgo de no verificado | Riesgo de no verificado |

> **Nota:** no es posible evaluar código duplicado real, acoplamiento, seguridad, rendimiento ni deuda técnica de implementación porque no hay código en el repositorio.

---

## 6. PROPUESTAS DE MEJORA

| Prioridad | Mejora | Resuelve | Patrón / refactor sugerido | Esfuerzo |
| --- | --- | --- | --- | --- |
| Alta | Fusionar CU-05 en CU-02 (subvariación automática) | Duplicación CU-02/CU-05 | Incluir CU-05 como paso 6a de CU-02 | Bajo |
| Alta | Renumerar RN en CU-01..05 al catálogo RN-01..17 | Referencias cruzadas rotas | Tabla de equivalencia + reemplazo de menciones | Bajo |
| Alta | Agregar el diagrama de flujo de estados (mascota/proceso) | Requisito explícito faltante | PlantUML en `BUSINESS/` + imagen exportada | Bajo |
| Alta | Crear esqueleto de código (solución .NET 3 capas + tests + DbInitializer) | No hay implementación | API + BusinessLogic + Persistence + Tests | Alto |
| Alta | Corregir precondición de CU-03 | Error detectado | Decidir creación del historial al registrar mascota vs. primer evento | Bajo |
| Media | Especificar CU de logout y recuperación de contraseña | Cobertura incompleta | Extender CU-07 o CU nuevos | Bajo-medio |
| Media | Decidir y documentar alcance de notificaciones y tareas (RN-16) | Requerimientos ambiguos | ADR (Architecture Decision Record) | Bajo |
| Media | Mapear NFR a tests (tiempo < 2 s, job diario) | Verificabilidad | Tests de carga + test de integración del job | Medio |
| Media | Modelar reportería/PDF como generador separado | Acoplamiento futuro | Strategy para formatos CSV/PDF; template engines | Medio |
| Baja | Centralizar auditoría con middleware/interceptor | Dependencia transversal RN-08 | Middleware + filtro de acción | Medio |
| Baja | Registrar ADRs de decisiones (blacklist-login, prueba 15 días, entidad única) | Trazabilidad de decisiones | Carpeta `BUSINESS/decisiones/` | Bajo |

---

## 7. CONCLUSIÓN

El proyecto está en fase de **especificación y diseño**, sin código implementado en el repositorio: lo existente es un buen conjunto de requerimientos de negocio, mockups de alta fidelidad y una base de casos de uso alineada a una guía técnica de referencia. El análisis corrigió las inconsistencias más graves (RN duplicadas, ambigüedad solicitud/proceso, workflow sin modelo) y cerró la brecha de cobertura con 13 casos de uso nuevos trazables a tests. Antes de codificar quedan dos acciones críticas: **implementar la refactorización de CU-01..05 (fusión CU-05 y renumeración RN)** y **agregar el diagrama de flujo de estados requerido**. Siguientes pasos recomendados: (1) validar con el cliente/docente las decisiones de alcance (notificaciones, tareas, login de blacklist), (2) crear el esqueleto .NET de 3 capas con `DbInitializer`, y (3) usar las matrices de trazabilidad de los 18 CU como backlog inicial de tests. La principal deuda es la ausencia de implementación: ningún riesgo de código, base de datos o rendimiento pudo verificarse.