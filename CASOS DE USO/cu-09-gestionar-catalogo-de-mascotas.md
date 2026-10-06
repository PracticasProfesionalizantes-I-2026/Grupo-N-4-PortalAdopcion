# Caso de Uso: Gestionar Catálogo de Mascotas

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-09 |
| **Nombre** | Gestionar Catálogo de Mascotas |
| **Actor Principal** | Administrador / Voluntario (personal autorizado) |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | ONG → registrar y mantener el catálogo de mascotas; Adoptante → visualizar las mascotas disponibles; Veterinario → disponer de la ficha de la mascota para el historial; Administración → auditoría de cada cambio |
| **Disparador (Trigger)** | El personal registra una mascota nueva o modifica los datos/estado de una existente |
| **Prioridad / Frecuencia** | Alta; alta frecuencia (ingresos y actualizaciones de mascotas) |
| **Reglas de negocio relacionadas** | RN-04 (la mascota pasa a Reservada al postular y deja de aparecer como disponible); RN-09 (estados válidos de la mascota); RN-17 (solo personal autorizado registra mascotas); RN-08 (auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite al personal autorizado dar de alta, editar, cambiar el estado o dar de
baja las mascotas del catálogo, manteniendo la ficha y la auditoría de cada
operación.

### 2. PRECONDICIONES
1. El actor debe estar autenticado (Token JWT válido) con rol que permita gestionar mascotas (**RN-17**).
2. Para la operación de edición/estado/baja, la mascota debe existir en la **Capa de Persistencia**.
3. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/mascotas` con un JSON que contiene los datos de la ficha (especie, raza, sexo, edad, nombre, foto y estado inicial).
2. La **Capa de Presentación** (`MascotasController`) valida que el JSON sea estructuralmente correcto y que los campos obligatorios estén presentes.
3. La **Capa de Negocio** (`MascotaService`) verifica la autorización (**RN-17**) y valida el estado inicial (por defecto `Disponible`).
4. La **Capa de Persistencia** guarda la ficha en la tabla `Mascotas`.
5. El Sistema registra la operación en auditoría (**RN-08**).
6. El Sistema devuelve un código **201 Created** con los datos de la mascota creada.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 falta algún dato obligatorio de la ficha.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request** detallando el campo faltante. Fin del caso de uso.

* **3a. Sin autorización para gestionar mascotas (HTTP 403 Forbidden):**
  1. Si el actor no posee el rol requerido para registrar mascotas (**RN-17**).
  2. El Sistema rechaza la petición por autorización.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **3b. Mascota duplicada (HTTP 409 Conflict):**
  1. Si en el Paso 3 el sistema detecta una ficha duplicada (p.ej. mismo ID de referencia o chip).
  2. La **Capa de Negocio** frena la operación.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **3c. Mascota inexistente en operaciones de edición/estado/baja (HTTP 404 Not Found):**
  1. Si en una operación de actualización el `{id}` de la mascota no existe.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3d. Baja con proceso activo (HTTP 409 Conflict):**
  1. Si en el Paso 3 se intenta dar de baja una mascota con un proceso de adopción activo (**RN-09**).
  2. La **Capa de Negocio** frena la operación.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la **Capa de Persistencia** no puede guardar la ficha.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Edición de la ficha (PUT/PATCH → 200):** el personal actualiza datos (raza, edad, foto, nombre) de una mascota existente.
2. **Cambio de estado (PATCH → 200):** transición a `En Tratamiento`, `Disponible`, `Reservada`, etc., respetando las transiciones válidas (**RN-09/RN-04**).
3. **Baja de la mascota (DELETE → 204):** la baja es administrativa y **no elimina** el historial clínico (trazabilidad — **RN-08**).
4. El actor puede enviar el JSON desde el panel web o desde un cliente HTTP; el esquema y los códigos son idénticos por operación.

### 6. POSTCONDICIONES
1. La ficha de la mascota queda persistida con su estado, o actualizada según la operación realizada.
2. Los cambios de estado impactan en la visibilidad del catálogo (una mascota `Reservada`/`En Tratamiento` no aparece como disponible — **RN-04/RN-09**).
3. Cada operación queda registrada en auditoría (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del alta de mascota. |
| `200` | OK | Confirmación de edición o cambio de estado. |
| `204` | No Content | Confirmación de baja (soft) sin cuerpo. |
| `400` | Bad Request | Fallo en la validación de esquema o campos obligatorios. |
| `403` | Forbidden | Sin autorización para gestionar mascotas (RN-17). |
| `404` | Not Found | Mascota referenciada inexistente (edición/estado/baja). |
| `409` | Conflict | Violación de invariantes (RN-17 duplicado; RN-09 baja con proceso activo). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON y campos obligatorios de la ficha.
- **Verificación (Negocio, → 403/404/409):** autorización (**RN-17**); existencia de la mascota (`MascotaNotFoundException` → 404); duplicados y transiciones de estado inválidas (`MascotaDuplicadaException`/`EstadoException` → 409). Auditoría (**RN-08**) emitida en la Capa de Negocio.

### Matriz de trazabilidad CU-09 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `MascotaService_CrearMascota_RetornaFicha` | `POST /api/mascotas_CreaMascota_Returns201` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/mascotas_ConJsonInvalido_Returns400` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (se detecta vía `[Required]` en Presentación) | `POST /api/mascotas_ConDatosFaltantes_Returns400` |
| 3a. Sin autorización | `403 Forbidden` | — (se resuelve en la autorización) | `POST /api/mascotas_SinPermiso_Returns403` |
| 3b. Mascota duplicada | `409 Conflict` | `MascotaService_ConFichaDuplicada_LanzaMascotaDuplicadaException` | `POST /api/mascotas_ConFichaDuplicada_Returns409` |
| 3c. Mascota inexistente | `404 Not Found` | `MascotaService_ConMascotaInexistente_LanzaMascotaNotFoundException` | `PUT /api/mascotas/{id}_ConMascotaInexistente_Returns404` |
| 3d. Baja con proceso activo | `409 Conflict` | `MascotaService_DarDeBaja_ConProcesoActivo_LanzaExcepcion` | `DELETE /api/mascotas/{id}_ConProcesoActivo_Returns409` |
| 5.1 Cambio de estado | `200 OK` | `MascotaService_CambiarEstado_ActualizaFicha` | `PATCH /api/mascotas/{id}/estado_CambiaEstado_Returns200` |
| 5.3 Baja de mascota | `204 No Content` | `MascotaService_DarDeBaja_ConservaHistorial` | `DELETE /api/mascotas/{id}_DaDeBaja_Returns204` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).