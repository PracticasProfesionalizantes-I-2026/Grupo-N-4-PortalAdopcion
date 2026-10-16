# Caso de Uso: Crear Solicitud de Adopción

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.
> La subfunción automática que crea el proceso en estado `Pendiente` se
> especifica en **CU-05**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-02 |
| **Nombre** | Crear Solicitud de Adopción |
| **Actor Principal** | Adoptante |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Adoptante → iniciar formalmente una postulación y recibir confirmación; ONG (personal de adopciones) → recibir la solicitud para evaluarla; Mascota → dejar de aparecer como disponible mientras haya una postulación activa (RN-04); Administración → trazabilidad y auditoría de la postulación |
| **Disparador (Trigger)** | El adoptante selecciona la opción "Solicitar adopción" en la ficha de una mascota disponible |
| **Prioridad / Frecuencia** | Alta; alta frecuencia (acción central del portal) |
| **Reglas de negocio relacionadas** | RN-01 (blacklist impide postular); RN-03 (el formulario incluye correo, teléfono, dirección, estado civil, edad, motivo y datos laborales; confirmación en copia oculta); RN-04 (una única postulación activa por mascota; reserva automática); RN-07 (código de confirmación); RN-14 (la postulación requiere sesión iniciada con datos correctos) |

---

### 1. BREVE DESCRIPCIÓN
Permite a un adoptante registrado iniciar formalmente un proceso de adopción
sobre una mascota disponible, quedando la solicitud registrada en estado
`Pendiente` para su posterior evaluación (CU-04).

### 2. PRECONDICIONES
1. El adoptante debe tener un estado de autenticación activo (Token JWT válido) con una cuenta activa (**RN-14**).
2. El perfil del adoptante debe estar completo (**RN-03**).
3. La mascota debe estar registrada y disponible para adopción (no reservada, no en tratamiento y sin restricciones administrativas).
4. El adoptante no debe estar en blacklist (**RN-01**) ni poseer otro proceso de adopción activo (**RN-04**).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/solicitudes` con un JSON que contiene `mascotaId` y los datos del formulario de postulación (correo, teléfono, dirección, estado civil, edad, motivo y datos laborales — **RN-03**).
2. La **Capa de Presentación** (`SolicitudesController`) valida que el JSON sea estructuralmente correcto y que los campos requeridos estén presentes y no vacíos.
3. La **Capa de Negocio** (`SolicitudService`) verifica las reglas de negocio: el adoptante puede postular (**RN-01/RN-14**), la mascota existe y está disponible (**RN-09**), y no existe otra postulación activa sobre esa mascota (**RN-04**).
4. El Sistema crea la solicitud con estado `Pendiente`, genera un identificador único, la asocia al adoptante y a la mascota, y la marca como `Reservada` (**RN-04**); genera el código de confirmación (**RN-07**).
5. La **Capa de Persistencia** guarda la solicitud en la tabla `Solicitudes` con fecha/hora de creación y usuario responsable.
6. El Sistema registra la operación en auditoría (**RN-08**) y notifica al personal autorizado de la ONG (in-app).
7. El Sistema devuelve un código **201 Created** con el ID de la solicitud, su estado y la confirmación del registro exitoso.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante / perfil incompleto (HTTP 400 Bad Request):**
  1. Si en el Paso 2 falta algún dato obligatorio del formulario (**RN-03**) o el perfil del adoptante está incompleto.
  2. El Sistema informa los campos pendientes; la petición no se procesa.
  3. El Sistema devuelve un código **400 Bad Request** indicando los datos que deben completarse. El flujo retorna al Paso 3 una vez actualizado el perfil. Fin del caso de uso.

* **3a. Usuario sin sesión iniciada (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 el Token JWT es inválido o no se envía.
  2. El Sistema (Capa de Presentación) rechaza la petición por falta de autenticación.
  3. El Sistema devuelve un código **401 Unauthorized** solicitando autenticación (CU-07). Una vez autenticado, el flujo continúa desde el Paso 2. Fin del caso de uso.

* **3b. Adoptante incluido en la Blacklist (HTTP 409 Conflict):**
  1. Si en el Paso 3 se detecta que el adoptante posee una sanción vigente, violando **RN-01**.
  2. El Sistema (Capa de Negocio) rechaza la creación de la solicitud.
  3. El Sistema devuelve un código **409 Conflict**, informa el motivo y registra el intento en auditoría (**RN-08**). Fin del caso de uso.

* **3c. Mascota inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `mascotaId` no corresponde a ninguna mascota registrada.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3d. Mascota ya no disponible (HTTP 409 Conflict):**
  1. Si en el Paso 3 la mascota cambió de estado (reservada, en tratamiento o dada de baja) antes de la confirmación.
  2. La **Capa de Negocio** detecta el cambio de estado y cancela la operación.
  3. El Sistema devuelve un código **409 Conflict** informando que la mascota ya no puede solicitarse. Fin del caso de uso.

* **3e. Adoptante con proceso activo (HTTP 409 Conflict):**
  1. Si en el Paso 3 el adoptante ya posee otra postulación o proceso activo, violando **RN-04**.
  2. El Sistema (Capa de Negocio) rechaza la creación.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **7a. Conflicto de concurrencia (HTTP 409 Conflict):**
  1. Si en el Paso 4 otro usuario confirma una postulación sobre la misma mascota mientras se procesa la transacción.
  2. El Sistema detecta que el estado de la mascota cambió antes de finalizar la transacción y cancela la operación para evitar inconsistencias.
  3. El Sistema devuelve un código **409 Conflict** informando que la mascota ya no se encuentra disponible y registra el evento en auditoría. Fin del caso de uso.

* **5a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 5 la **Capa de Persistencia** no puede guardar la solicitud (ej. falla de conexión o corrupción de la base de datos).
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Cancelación antes de confirmar:** el adoptante puede descartar el formulario sin confirmar; el Sistema descarta la información temporal y no registra ninguna solicitud (no es un error).
2. El actor puede enviar el JSON desde el panel web o desde un cliente HTTP (Bruno, Postman, Swagger/Scalar); el esquema y el resultado (`201 Created`) son idénticos.
3. La confirmación de la postulación conlleva la creación automática del proceso en `Pendiente` (ver **CU-05**).

### 6. POSTCONDICIONES
1. Se crea un nuevo registro persistente en la tabla `Solicitudes` con estado `Pendiente`, asociado al adoptante y a la mascota.
2. La mascota pasa al estado `Reservada` y deja de aparecer como disponible en el catálogo (impacto en la visibilidad — **RN-04**).
3. La operación queda registrada en auditoría y la solicitud queda disponible para su evaluación (CU-04).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa de la nueva solicitud en `Pendiente`. |
| `400` | Bad Request | Fallo en la validación de esquema, datos obligatorios faltantes o perfil incompleto (RN-03). |
| `401` | Unauthorized | Petición sin token JWT válido (RN-14). |
| `404` | Not Found | Mascota referenciada inexistente en la Capa de Persistencia. |
| `409` | Conflict | Violación de invariantes de negocio o concurrencia (RN-01 blacklist, RN-04 reserva/proceso activo, mascota no disponible). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON, campos obligatorios del formulario (RN-03) y perfil completo.
- **Verificación (Negocio, → 401/404/409):** autenticación (401); existencia de la mascota (`MascotaNotFoundException` → 404); RN-01 blacklist, RN-04 reserva única/proceso activo y RN-09 mascota no disponible (`BlacklistException` / `MascotaNoDisponibleException` → 409). La auditoría (RN-08) y el código de confirmación (RN-07) se emiten en la Capa de Negocio.

### Matriz de trazabilidad CU-02 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `SolicitudService_CrearSolicitud_RetornaSolicitudPendiente` | `POST /api/solicitudes_CreaSolicitud_Returns201` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/solicitudes_ConJsonInvalido_Returns400` |
| 2a. Perfil incompleto | `400 Bad Request` | `SolicitudService_ConPerfilIncompleto_LanzaValidationException` | `POST /api/solicitudes_ConPerfilIncompleto_Returns400` |
| 3a. Sin sesión | `401 Unauthorized` | — (se detecta en la autenticación) | `POST /api/solicitudes_SinToken_Returns401` |
| 3b. Blacklist | `409 Conflict` | `SolicitudService_ConBlacklist_LanzaSanctionException` | `POST /api/solicitudes_ConBlacklist_Returns409` |
| 3c. Mascota inexistente | `404 Not Found` | `SolicitudService_ConMascotaInexistente_LanzaMascotaNotFoundException` | `POST /api/solicitudes_ConMascotaInexistente_Returns404` |
| 3d. Mascota no disponible | `409 Conflict` | `SolicitudService_ConMascotaNoDisponible_LanzaMascotaNoDisponibleException` | `POST /api/solicitudes_ConMascotaNoDisponible_Returns409` |
| 3e. Proceso activo | `409 Conflict` | `SolicitudService_ConProcesoActivo_LanzaProcesoActivoException` | `POST /api/solicitudes_ConProcesoActivo_Returns409` |
| 7a. Concurrencia | `409 Conflict` | `SolicitudService_ConConcurrencia_DetectaYCancela` | `POST /api/solicitudes_ConConcurrencia_Returns409` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).