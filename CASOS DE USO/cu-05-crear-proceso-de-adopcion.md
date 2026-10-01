# Caso de Uso: Crear Proceso de Adopción en Estado "Pendiente"

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.
> **Nota de diseño:** este caso de uso es la **subfunción automática** del Paso 7
> de **CU-02** (postulación confirmada). Se mantiene como archivo independiente
> por fidelidad al documento original. `Solicitud` y `Proceso` se modelan como una
> única entidad (`SolicitudDeAdopcion`).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-05 |
| **Nombre** | Crear Proceso de Adopción en Estado "Pendiente" |
| **Actor Principal** | Sistema (subfunción automática de CU-02) |
| **Alcance / Nivel** | Sistema; subfunción |
| **Stakeholders e intereses** | Adoptante → obtener la confirmación del proceso creado; ONG (Personal de Adopciones) → recibir el proceso para su evaluación (CU-04); Administración → auditoría del alta del proceso |
| **Disparador (Trigger)** | El Sistema recibe la confirmación de una solicitud de adopción válida por parte del adoptante (Paso 7 de CU-02) |
| **Prioridad / Frecuencia** | Alta; por cada postulación confirmada |
| **Reglas de negocio relacionadas** | RN-03 (confirmación de postulación en copia oculta); RN-04 (una única postulación activa por mascota; reserva automática); RN-07 (código de confirmación); RN-08 (auditoría). *El modelo de transiciones agrega RN-14; se sigue el set del índice de CU.* |

---

### 1. BREVE DESCRIPCIÓN
Permite al Sistema crear automáticamente el proceso de adopción en estado
`Pendiente` cuando una solicitud de adopción cumple todas las validaciones de
negocio, quedando disponible para su posterior evaluación por personal autorizado.

### 2. PRECONDICIONES
1. La solicitud de adopción debe haber sido confirmada por el adoptante (CU-02).
2. La mascota debe existir, estar `Disponible` y no reservada por otro proceso (**RN-04/RN-09**).
3. El adoptante no debe poseer otro proceso de adopción activo (**RN-04**).
4. El adoptante no debe encontrarse en blacklist (**RN-01**).
5. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Sistema recibe la confirmación de la solicitud de adopción (Paso 7 de CU-02).
2. El Sistema re-verifica la disponibilidad de la mascota (**RN-04/RN-09**).
3. El Sistema verifica que el adoptante no posea procesos activos (**RN-04**).
4. El Sistema verifica que el adoptante no se encuentre en blacklist (**RN-01**).
5. El Sistema valida la integridad de la información recibida.
6. El Sistema genera un identificador único, crea el proceso con estado `Pendiente` y lo vincula a la mascota y al adoptante.
7. El Sistema genera el código de confirmación (**RN-07**) y registra fecha/hora de creación y usuario responsable de la solicitud.
8. El Sistema registra la operación en auditoría (**RN-08**).
9. El Sistema devuelve un código **201 Created** y notifica al personal de la ONG (in-app).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. La mascota dejó de estar disponible (HTTP 409 Conflict):**
  1. Si en el Paso 2 la mascota fue reservada o adoptada durante el procesamiento (**RN-04/RN-09**).
  2. El Sistema cancela la creación del proceso.
  3. El Sistema devuelve un código **409 Conflict** informando al adoptante que la mascota ya no se encuentra disponible. Fin del caso de uso.

* **3a. El adoptante posee otro proceso activo (HTTP 409 Conflict):**
  1. Si en el Paso 3 se detecta un proceso de adopción activo asociado al usuario (**RN-04**).
  2. El Sistema rechaza la creación del nuevo proceso.
  3. El Sistema devuelve un código **409 Conflict** con el motivo del rechazo. Fin del caso de uso.

* **4a. El usuario pertenece a la blacklist (HTTP 409 Conflict):**
  1. Si en el Paso 4 se detecta una sanción vigente (**RN-01**).
  2. El Sistema impide la creación del proceso.
  3. El Sistema devuelve un código **409 Conflict** con el motivo del bloqueo. Fin del caso de uso.

* **5a. Error en la validación de datos (HTTP 400 Bad Request):**
  1. Si en el Paso 5 se detecta información inconsistente o incompleta.
  2. El Sistema cancela la operación sin generar registros parciales.
  3. El Sistema devuelve un código **400 Bad Request** e informa al usuario que la solicitud no pudo procesarse. Fin del caso de uso.

* **9a. Error en la notificación (HTTP 200 - continúa):**
  1. Si en el Paso 9 el Sistema no puede enviar la notificación al personal autorizado.
  2. El Sistema registra el incidente en auditoría (**RN-08**).
  3. El proceso continúa en estado `Pendiente`; el caso de uso no falla.

* **6a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 6 la **Capa de Persistencia** no puede guardar el proceso.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. La creación del proceso se ejecuta de forma **transaccional y atómica** junto con el alta de la solicitud de CU-02: o se persiste el proceso completo, o no queda ningún registro parcial.
2. El código de confirmación (**RN-07**) se genera en el momento de la creación y se utiliza en la confirmación de la postulación (copia oculta - **RN-03**).

### 6. POSTCONDICIONES
1. Se crea un nuevo proceso con identificador único y estado `Pendiente`, vinculado al adoptante solicitante y a la mascota seleccionada.
2. La solicitud/proceso queda disponible para la evaluación del Personal de Adopciones (CU-04).
3. La operación queda registrada en auditoría con trazabilidad completa (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del nuevo proceso en `Pendiente`. |
| `400` | Bad Request | Fallo en la validación de datos recibidos. |
| `409` | Conflict | Violación de invariantes (RN-01 blacklist, RN-04 proceso activo / mascota no disponible). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** integridad y formato de los datos de la solicitud confirmada.
- **Verificación (Negocio, → 409):** disponibilidad de la mascota (**RN-04/RN-09**), ausencia de procesos activos (**RN-04**) y blacklist (**RN-01**). Auditoría (**RN-08**) y código de confirmación (**RN-07**) emitidos por el Sistema. La notificación fallida no afecta la persistencia del proceso (9a).

### Matriz de trazabilidad CU-05 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `SolicitudService_CrearProceso_RetornaProcesoPendiente` | `POST /api/solicitudes_CreaProceso_Returns201` |
| 2a. Mascota no disponible | `409 Conflict` | `SolicitudService_CrearProceso_ConMascotaNoDisponible_Cancela` | `POST /api/solicitudes_CreaProceso_ConMascotaNoDisponible_Returns409` |
| 3a. Proceso activo | `409 Conflict` | `SolicitudService_CrearProceso_ConProcesoActivo_Rechaza` | `POST /api/solicitudes_CreaProceso_ConProcesoActivo_Returns409` |
| 4a. Blacklist | `409 Conflict` | `SolicitudService_CrearProceso_ConBlacklist_Impedir` | `POST /api/solicitudes_CreaProceso_ConBlacklist_Returns409` |
| 5a. Error de validación | `400 Bad Request` | `SolicitudService_CrearProceso_ConDatosInvalidos_LanzaValidationException` | `POST /api/solicitudes_CreaProceso_ConDatosInvalidos_Returns400` |
| 9a. Error en notificación | `200` (continúa) | `NotificacionService_ErrorAlNotificar_RegistraIncidente` | `POST /api/solicitudes_CreaProceso_ConErrorNotificacion_Returns201` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).