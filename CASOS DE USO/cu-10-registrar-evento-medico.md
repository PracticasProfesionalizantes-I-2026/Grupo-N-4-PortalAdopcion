# Caso de Uso: Registrar Evento Médico

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.
> Según P4 (opción b), si el historial clínico aún no existe se crea junto con el
> primer evento médico; el caso base de creación es **CU-03**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-10 |
| **Nombre** | Registrar Evento Médico |
| **Actor Principal** | Veterinario |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Veterinario → documentar la evolución sanitaria; ONG / Mascota → contar con el historial clínico completo; Administración → auditoría de los eventos clínicos |
| **Disparador (Trigger)** | El veterinario incorpora un nuevo evento (tratamiento, control, vacunación, desparasitación u observación) al historial de una mascota |
| **Prioridad / Frecuencia** | Media; media frecuencia (eventos por mascota) |
| **Reglas de negocio relacionadas** | RN-08 (auditoría); RN-13 (decisiones de salud vinculadas al periodo de prueba); historial clínico preexistente (creado en CU-03 o junto con el primer evento — P4) |

---

### 1. BREVE DESCRIPCIÓN
Permite al veterinario agregar un evento médico al historial clínico de una
mascota, dejando constancia de tratamientos, controles, vacunaciones y
observaciones para su seguimiento sanitario.

### 2. PRECONDICIONES
1. El veterinario debe estar autenticado (Token JWT válido) con permisos sobre la información clínica.
2. La mascota debe existir y tener un estado válido en el sistema.
3. El historial clínico de la mascota debe existir (CU-03) o crearse junto con el primer evento (P4, opción b).
4. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/mascotas/{id}/historial/eventos` con un JSON que contiene el tipo de evento, la descripción, la fecha/hora y el profesional actuante.
2. La **Capa de Presentación** (`HistorialClinicoController`) valida que el JSON sea estructuralmente correcto, los campos obligatorios y las fechas.
3. La **Capa de Negocio** (`HistorialClinicoService`) verifica que la mascota y el historial existan, y registra el profesional (bloqueo por sesión de la ficha).
4. La **Capa de Persistencia** agrega el evento al historial con fecha/hora y profesional.
5. El Sistema registra la operación en auditoría (**RN-08**).
6. El Sistema devuelve un código **201 Created** con los datos del evento registrado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante o fecha inválida (HTTP 400 Bad Request):**
  1. Si en el Paso 2 falta el tipo de evento, la descripción o la fecha, o la fecha es inválida.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Mascota inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `{id}` de la mascota no existe en la **Capa de Persistencia**.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3b. Historial no creado (HTTP 404 Not Found):**
  1. Si en el Paso 3 la mascota no posee historial clínico y el sistema no lo crea junto con el evento (P4).
  2. La **Capa de Negocio** no encuentra el historial.
  3. El Sistema devuelve un código **404 Not Found** y ofrece crearlo vía CU-03. Fin del caso de uso.

* **3c. Profesional sin permiso o ficha bloqueada (HTTP 403 Forbidden):**
  1. Si en el Paso 3 otro profesional está operando la ficha clínica (bloqueo por sesión) o el actor no posee permisos clínicos.
  2. El Sistema rechaza la operación.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la **Capa de Persistencia** no puede guardar el evento.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El tipo de evento puede ser tratamiento, control veterinario, vacunación, desparasitación u observación; el esquema de registro es el mismo.
2. Si el historial aún no existe, el sistema lo crea junto con el primer evento (P4, opción b), quedando el evento asociado al nuevo historial.

### 6. POSTCONDICIONES
1. El evento queda persistido en el historial clínico de la mascota.
2. El evento queda visible para el personal clínico autorizado y para el seguimiento del periodo de prueba (**RN-13**).
3. La operación queda registrada en auditoría (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del evento médico. |
| `400` | Bad Request | Fallo en la validación de esquema, datos obligatorios o fechas. |
| `403` | Forbidden | Profesional sin permiso o ficha bloqueada por sesión. |
| `404` | Not Found | Mascota o historial inexistente. |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON, campos obligatorios y fechas.
- **Verificación (Negocio, → 403/404):** permisos clínicos y bloqueo de ficha (`AccesoDenegadoException` → 403); existencia de mascota e historial (`MascotaNotFoundException`/`HistorialNoEncontradoException` → 404). Auditoría (**RN-08**) emitida en la Capa de Negocio.

### Matriz de trazabilidad CU-10 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `HistorialClinicoService_RegistrarEvento_RetornaEvento` | `POST /api/mascotas/{id}/historial/eventos_RegistraEvento_Returns201` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/mascotas/{id}/historial/eventos_ConJsonInvalido_Returns400` |
| 2a. Datos faltantes / fecha inválida | `400 Bad Request` | `HistorialClinicoService_ConFechaInvalida_LanzaValidationException` | `POST /api/mascotas/{id}/historial/eventos_ConFechaInvalida_Returns400` |
| 3a. Mascota inexistente | `404 Not Found` | `HistorialClinicoService_ConMascotaInexistente_LanzaMascotaNotFoundException` | `POST /api/mascotas/{id}/historial/eventos_ConMascotaInexistente_Returns404` |
| 3b. Historial no creado | `404 Not Found` | `HistorialClinicoService_ConHistorialNoCreado_LanzaHistorialNoEncontradoException` | `POST /api/mascotas/{id}/historial/eventos_ConHistorialNoCreado_Returns404` |
| 3c. Sin permiso clínico | `403 Forbidden` | `HistorialClinicoService_ConProfesionalSinPermiso_LanzaAccesoDenegadoException` | `POST /api/mascotas/{id}/historial/eventos_ConProfesionalSinPermiso_Returns403` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).