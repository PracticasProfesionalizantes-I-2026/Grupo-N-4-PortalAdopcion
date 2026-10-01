# Caso de Uso: Gestionar Blacklist de Usuarios

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**
> (ver `COSAS/Modelo-de-Estados-y-Reglas-de-Negocio.md`).
> Operación compuesta: el flujo principal modela el **alta de sanción**; la
> modificación (PUT) y la eliminación (DELETE) se cubren como sub-variaciones.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-01 |
| **Nombre** | Gestionar Blacklist de Usuarios |
| **Actor Principal** | Administrador del Sistema |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Administración → impedir que usuarios infractores vuelvan a postularse y registrar las sanciones; Adoptante sancionado → conocer el motivo y la duración de su sanción; Sistema → conservar trazabilidad y auditoría de cada sanción |
| **Disparador (Trigger)** | El administrador detecta una infracción de las normas del proceso de adopción o necesita revisar una sanción existente |
| **Prioridad / Frecuencia** | Alta; media frecuencia (sanciones ocasionales) |
| **Reglas de negocio relacionadas** | RN-01 (blacklist impide nuevos procesos de adopción); RN-02 (sanciones con motivo, fecha y duración, editables solo por personal autorizado y auditadas); RN-15 (sanción ligada al perfil dni/email, no eludible con cuenta nueva); RN-08 (auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador registrar, modificar o eliminar sanciones (blacklist)
sobre usuarios que hayan incumplido las normas del proceso de adopción,
impidiendo temporalmente que inicien nuevas postulaciones.

### 2. PRECONDICIONES
1. El sistema debe estar en funcionamiento y con la **Capa de Persistencia** accesible (base de datos inicializada por `DbInitializer`).
2. El actor debe poseer un estado de autenticación activo (Token JWT válido) con rol **Administrador** y permisos para gestionar sanciones (**RN-02**).
3. El usuario sobre el que se aplicará la sanción debe existir en el sistema y no estar eliminado o inactivo permanentemente (**RN-15**).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `POST /api/blacklists` con un JSON que contiene los datos de la sanción (DNI o correo del usuario infractor, motivo, fecha de inicio y duración).
2. La **Capa de Presentación** (`BlacklistController`) valida que el JSON sea estructuralmente correcto y que los campos requeridos estén presentes y no vacíos (data annotations `[Required]` sobre el DTO de creación de sanción).
3. La **Capa de Negocio** (`BlacklistService`) verifica que el usuario exista en la **Capa de Persistencia** y que no posea una sanción vigente; valida el motivo y la duración aplicando **RN-02**.
4. La **Capa de Persistencia** registra la sanción con fecha de inicio y vencimiento en la tabla `Blacklists`.
5. El Sistema registra la operación en el módulo de **auditoría** (usuario responsable, fecha/hora y motivo — **RN-08**).
6. El Sistema devuelve un código **200 OK** con los datos de la sanción registrada. A partir de este momento la regla **RN-01** impide al usuario iniciar nuevas solicitudes de adopción mientras la sanción esté vigente.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por error de esquema.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 la petición no incluye el usuario (DNI/correo), el motivo o la duración de la sanción.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** detallando el campo faltante. Fin del caso de uso.

* **3a. Usuario inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 no se encuentra un usuario con el DNI o correo enviado en la **Capa de Persistencia**.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente (`UsuarioNotFoundException`).
  3. El Sistema devuelve un código **404 Not Found** indicando que el usuario no existe. Fin del caso de uso.

* **3b. Usuario con sanción vigente (HTTP 409 Conflict):**
  1. Si en el Paso 3 el usuario ya posee una sanción activa, violando **RN-02** (una única sanción vigente por perfil).
  2. El Sistema frena la ejecución en la **Capa de Negocio** y lanza la excepción de dominio `SanctionException`.
  3. El Sistema devuelve un código **409 Conflict**; el administrador puede optar por modificar la sanción existente o cancelar la operación. Fin del caso de uso.

* **3c. Usuario eliminado o inactivo permanentemente (HTTP 409 Conflict):**
  1. Si en el Paso 3 el usuario se encuentra dado de baja o con baja definitiva (soft), no es posible sancionarlo (**RN-15**).
  2. El Sistema (Capa de Negocio) rechaza la operación.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la **Capa de Persistencia** no puede guardar la sanción (ej. falla de conexión o corrupción de la base de datos).
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Modificar sanción:** el administrador envía `PUT /api/blacklists/{id}` con el nuevo motivo o duración; el Sistema valida y persiste el cambio devolviendo **200 OK** y registrando la modificación en auditoría (**RN-02/RN-08**). Aplica al detalle 3b.
2. **Eliminar sanción:** el administrador envía `DELETE /api/blacklists/{id}`; el Sistema elimina la sanción y devuelve **204 No Content**. Aplica al detalle 3b.
3. **Consulta/búsqueda previa:** antes de sancionar, el administrador puede buscar al usuario por DNI, correo o nombre (`GET /api/blacklists?q=...`). En todas las variantes el esquema y los códigos siguen la semántica del caso de uso.

### 6. POSTCONDICIONES
1. La sanción queda registrada en la tabla `Blacklists` con motivo, fecha de inicio y vencimiento.
2. El usuario queda imposibilitado de iniciar nuevas solicitudes de adopción mientras la sanción esté vigente (impacto en la elegibilidad — **RN-01**).
3. La operación queda registrada en el módulo de auditoría con el responsable y la fecha/hora (trazabilidad completa — **RN-08/RN-15**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Confirmación de sanción aplicada o modificada. |
| `204` | No Content | Confirmación de eliminación de sanción (sin cuerpo). |
| `400` | Bad Request | Fallo en la validación de esquema o campos obligatorios. |
| `404` | Not Found | Usuario a sancionar inexistente en la Capa de Persistencia. |
| `409` | Conflict | Violación de invariantes (RN-02: sanción vigente; RN-15: usuario dado de baja). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** campos obligatorios y formato del JSON (`[Required]` sobre el DTO + `ModelState.IsValid` y model binding).
- **Verificación (Negocio, → 404/409):** existencia del usuario (`UsuarioNotFoundException` → 404); sanción vigente única (**RN-02** → `SanctionException` → 409); usuario dado de baja (**RN-15** → 409). La auditoría (**RN-08**) se registra como dependencia transversal.

### Matriz de trazabilidad CU-01 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `BlacklistService_AgregarSanción_RegistraYRetornaSanción` | `POST /api/blacklists_AgregaSanción_Returns200` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/blacklists_ConJsonInvalido_Returns400` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (se detecta vía `[Required]` en Presentación) | `POST /api/blacklists_SinMotivo_Returns400` |
| 3a. Usuario inexistente | `404 Not Found` | `BlacklistService_ConUsuarioInexistente_LanzaUsuarioNotFoundException` | `POST /api/blacklists_ConUsuarioInexistente_Returns404` |
| 3b. Sanción vigente | `409 Conflict` | `BlacklistService_ConSanciónVigente_LanzaSanctionException` | `POST /api/blacklists_ConSanciónVigente_Returns409` |
| 3c. Usuario dado de baja | `409 Conflict` | `BlacklistService_ConUsuarioDeBaja_RechazaSanción` | `POST /api/blacklists_ConUsuarioDeBaja_Returns409` |
| 5.1 Modificar sanción | `200 OK` | `BlacklistService_ModificarSanción_PersistirCambios` | `PUT /api/blacklists_ModificaSanción_Returns200` |
| 5.2 Eliminar sanción | `204 No Content` | `BlacklistService_EliminarSanción_QuitarRestricción` | `DELETE /api/blacklists_EliminaSanción_Returns204` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).