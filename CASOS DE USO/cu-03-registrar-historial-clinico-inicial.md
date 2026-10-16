# Caso de Uso: Registrar Historial Clínico Inicial

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.
> **Corrección de la precondición contradictoria** de la versión original ("la
> ficha clínica debe existir"): este caso de uso **crea** el historial, por lo que
> la precondición se corrige según P4 (opción b: el historial nace con el primer
> registro clínico). La unicidad del historial no posee una RN dedicada en el
> catálogo RN-01..RN-17; se documenta como **invariante de dominio** con su nota
> P4 correspondiente.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-03 |
| **Nombre** | Registrar Historial Clínico Inicial |
| **Actor Principal** | Veterinario |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Veterinario → dejar constancia oficial del estado de salud inicial; ONG / Mascota → contar con la ficha clínica para el seguimiento sanitario; Administrador (secundario) → habilitar el registro cuando corresponda; Administración → trazabilidad y auditoría clínica |
| **Disparador (Trigger)** | El veterinario solicita registrar el historial clínico de una mascota registrada en el sistema |
| **Prioridad / Frecuencia** | Media; media frecuencia (al ingreso de cada mascota a la ONG) |
| **Reglas de negocio relacionadas** | Unicidad de historial por mascota (invariante de dominio, nota P4); RN-08 (auditoría); RN-09 (la mascota debe tener un estado válido) |

---

### 1. BREVE DESCRIPCIÓN
Permite al veterinario crear el historial clínico inicial de una mascota
registrada, incorporando la información médica necesaria para su seguimiento
sanitario y sirviendo como registro oficial de su condición de salud.

### 2. PRECONDICIONES
1. El veterinario debe estar autenticado (Token JWT válido) con permisos para gestionar información clínica.
2. La mascota debe estar registrada y poseer un estado válido dentro del sistema (**RN-09**).
3. La mascota no debe poseer un historial clínico previamente registrado (invariante de unicidad).
4. Debe existir disponibilidad del servicio de almacenamiento.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/mascotas/{id}/historial` con un JSON que contiene la ficha clínica inicial (estado general de salud, peso, edad estimada, vacunas aplicadas, desparasitación, esterilización, enfermedades diagnosticadas, alergias conocidas y antecedentes médicos relevantes).
2. La **Capa de Presentación** (`HistorialClinicoController`) valida que el JSON sea estructuralmente correcto y que los campos obligatorios estén presentes.
3. La **Capa de Negocio** (`HistorialClinicoService`) verifica que la mascota exista y tenga un estado válido (**RN-09**), y que no posea un historial previo (invariante de unicidad).
4. La **Capa de Persistencia** genera un identificador único, asocia el historial a la mascota y registra fecha/hora de creación y usuario responsable.
5. El Sistema registra la operación en auditoría (**RN-08**).
6. El Sistema devuelve un código **201 Created** con el historial clínico creado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Información obligatoria incompleta (HTTP 400 Bad Request):**
  1. Si en el Paso 2 faltan datos obligatorios de la ficha clínica.
  2. El Sistema (Capa de Presentación) indica los campos pendientes.
  3. El Sistema devuelve un código **400 Bad Request**. El flujo retorna al Paso 1 para corregir los datos. Fin del caso de uso.

* **2b. Datos clínicos inconsistentes (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el Sistema detecta inconsistencias en la información clínica (ej. fechas, rangos de valores inválidos).
  2. El Sistema informa los errores encontrados.
  3. El Sistema devuelve un código **400 Bad Request**. El flujo retorna al Paso 1. Fin del caso de uso.

* **3a. Mascota inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `{id}` de la mascota no existe en la **Capa de Persistencia**.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3b. Historial clínico ya existente (HTTP 409 Conflict):**
  1. Si en el Paso 3 la mascota ya posee un historial clínico, violando el invariante de unicidad.
  2. El Sistema frena la ejecución en la **Capa de Negocio**.
  3. El Sistema devuelve un código **409 Conflict** y ofrece consultar el historial existente (CU-10). Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la **Capa de Persistencia** no puede guardar el historial.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Cancelación del registro:** el veterinario puede cancelar el registro antes de confirmarlo; el Sistema descarta la información temporal y no registra el historial (no es un error).
2. **Actor secundario:** un Administrador con permisos clínicos puede ejecutar el caso de uso con las mismas validaciones.
3. El historial queda disponible para el registro de eventos médicos posteriores (**CU-10**).

### 6. POSTCONDICIONES
1. Se crea un historial clínico único, persistente y asociado a la mascota correspondiente.
2. El historial queda disponible para registrar tratamientos, controles, vacunaciones y observaciones posteriores (CU-10).
3. La operación queda registrada en auditoría con usuario responsable y fecha/hora (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del nuevo historial clínico. |
| `400` | Bad Request | Fallo en la validación de esquema, datos obligatorios o consistencia clínica. |
| `404` | Not Found | Mascota referenciada inexistente en la Capa de Persistencia. |
| `409` | Conflict | Violación del invariante de unicidad (historial ya existente). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON, campos obligatorios y consistencia de la información clínica.
- **Verificación (Negocio, → 404/409):** existencia y estado válido de la mascota (**RN-09** → 404); unicidad del historial (invariante de dominio → `HistorialExistenteException` → 409). Auditoría (**RN-08**) registrada en la Capa de Negocio.

### Matriz de trazabilidad CU-03 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `HistorialClinicoService_CrearHistorialInicial_RetornaHistorial` | `POST /api/mascotas/{id}/historial_CreaHistorial_Returns201` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/mascotas/{id}/historial_ConJsonInvalido_Returns400` |
| 2a. Datos obligatorios faltantes | `400 Bad Request` | `HistorialClinicoService_ConDatosObligatoriosFaltantes_LanzaValidationException` | `POST /api/mascotas/{id}/historial_ConDatosFaltantes_Returns400` |
| 2b. Datos inconsistentes | `400 Bad Request` | `HistorialClinicoService_ConDatosInconsistentes_LanzaValidationException` | `POST /api/mascotas/{id}/historial_ConDatosInconsistentes_Returns400` |
| 3a. Mascota inexistente | `404 Not Found` | `HistorialClinicoService_ConMascotaInexistente_LanzaMascotaNotFoundException` | `POST /api/mascotas/{id}/historial_ConMascotaInexistente_Returns404` |
| 3b. Historial existente | `409 Conflict` | `HistorialClinicoService_ConHistorialExistente_LanzaHistorialExistenteException` | `POST /api/mascotas/{id}/historial_ConHistorialExistente_Returns409` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).