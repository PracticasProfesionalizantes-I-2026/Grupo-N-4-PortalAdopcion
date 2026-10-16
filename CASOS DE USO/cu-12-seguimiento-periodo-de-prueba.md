# Caso de Uso: Realizar Seguimiento del Periodo de Prueba

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-12 |
| **Nombre** | Realizar Seguimiento del Periodo de Prueba |
| **Actor Principal** | Voluntario |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Voluntario → registrar el día a día de la mascota en periodo de prueba; Veterinario → recibir la alerta cuando el estado lo requiera (RN-13); ONG → monitorear el periodo de prueba; Administración → auditoría de los seguimientos |
| **Disparador (Trigger)** | El voluntario registra la entrada diaria de seguimiento de un proceso en estado `En Periodo de Prueba` |
| **Prioridad / Frecuencia** | Media; alta frecuencia durante el periodo (diaria, 15 días por defecto) |
| **Reglas de negocio relacionadas** | RN-13 (periodo de 15 días; estado en alerta impide finalizar sin validación del veterinario); RN-16 (una entrada por día; ánimo, alimentación, tareas, medicación, limpieza; solo 2 tareas activas por voluntario); RN-08 (auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite al voluntario registrar el seguimiento diario de una mascota en periodo de
prueba (ánimo, alimentación, tareas, medicación, limpieza), generando alerta al
veterinario cuando el estado lo requiera.

### 2. PRECONDICIONES
1. El voluntario debe estar autenticado (Token JWT válido) y tener asignado el proceso de seguimiento (**RN-16**).
2. El proceso debe encontrarse en estado `En Periodo de Prueba`.
3. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/procesos/{id}/seguimientos` con un JSON que contiene la fecha del día, el ánimo, la alimentación, las tareas, la medicación, la limpieza y las observaciones.
2. La **Capa de Presentación** (`SeguimientoController`) valida que el JSON sea estructuralmente correcto y que los campos obligatorios estén presentes.
3. La **Capa de Negocio** (`SeguimientoService`) verifica que el voluntario tenga asignado el proceso (**RN-16**), que no exista un seguimiento para el mismo día (única entrada diaria) y aplica las validaciones del periodo (**RN-13**).
4. La **Capa de Persistencia** guarda el seguimiento en la tabla `Seguimientos`.
5. Si el estado registrado está en **alerta**, el Sistema notifica al veterinario (**RN-13/RN-16**).
6. El Sistema registra la operación en auditoría (**RN-08**).
7. El Sistema devuelve un código **201 Created** con el seguimiento registrado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 falta alguno de los campos del seguimiento.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request** detallando el campo faltante. Fin del caso de uso.

* **3a. Proceso inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `{id}` del proceso no existe en la **Capa de Persistencia**.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3b. Voluntario sin tarea asignada (HTTP 403 Forbidden):**
  1. Si en el Paso 3 el voluntario no tiene asignado el proceso, o ya posee el máximo de 2 tareas activas (**RN-16**).
  2. El Sistema rechaza la operación.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **3c. Seguimiento duplicado del día (HTTP 409 Conflict):**
  1. Si en el Paso 3 ya existe un seguimiento registrado para el mismo día y proceso (**RN-16**, única entrada diaria).
  2. El Sistema frena la ejecución en la **Capa de Negocio**.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **3d. Estado del proceso no válido (HTTP 409 Conflict):**
  1. Si en el Paso 3 el proceso no se encuentra en `En Periodo de Prueba` (p.ej. ya terminó).
  2. El Sistema frena la ejecución en la **Capa de Negocio**.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la **Capa de Persistencia** no puede guardar el seguimiento.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Entrada diaria:** se permite una única entrada por día y proceso (RN-16); el cumplimiento culmina a los 15 días del periodo (RN-13).
2. **Nota y alarma:** si el ánimo o los indicadores están en alerta, el seguimiento marca el estado del día y el Veterinario es notificado (RN-13).
3. El voluntario puede registrar desde el panel web ("Seguimiento Diario" / "Monitoreo") o desde un cliente HTTP.

### 6. POSTCONDICIONES
1. El seguimiento queda persistido en la tabla `Seguimientos` asociado al proceso y al voluntario.
2. Si el estado está en alerta, el Veterinario recibe la notificación (impacto para CU-15: no se puede finalizar sin validación veterinaria — **RN-13**).
3. La operación queda registrada en auditoría (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del seguimiento. |
| `400` | Bad Request | Fallo en la validación de esquema o campos obligatorios. |
| `403` | Forbidden | Voluntario sin tarea asignada o sin permiso (RN-16). |
| `404` | Not Found | Proceso referenciado inexistente. |
| `409` | Conflict | Violación de invariantes (seguimiento duplicado del día; estado no válido). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON y campos obligatorios del seguimiento.
- **Verificación (Negocio, → 403/404/409):** asignación y límite de tareas (**RN-16** → 403); existencia del proceso (`ProcesoNotFoundException` → 404); unicidad diaria (**RN-16**) y estado del proceso (**RN-13**) → 409. Auditoría (**RN-08**) emitida en la Capa de Negocio.

### Matriz de trazabilidad CU-12 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `SeguimientoService_RegistrarSeguimiento_RetornaRegistro` | `POST /api/procesos/{id}/seguimientos_RegistraSeguimiento_Returns201` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/procesos/{id}/seguimientos_ConJsonInvalido_Returns400` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (se detecta vía `[Required]` en Presentación) | `POST /api/procesos/{id}/seguimientos_ConDatosFaltantes_Returns400` |
| 3a. Proceso inexistente | `404 Not Found` | `SeguimientoService_ConProcesoInexistente_LanzaProcesoNotFoundException` | `POST /api/procesos/{id}/seguimientos_ConProcesoInexistente_Returns404` |
| 3b. Tarea no asignada | `403 Forbidden` | `SeguimientoService_ConVoluntarioSinTarea_LanzaAccesoDenegadoException` | `POST /api/procesos/{id}/seguimientos_ConVoluntarioSinTarea_Returns403` |
| 3c. Seguimiento duplicado del día | `409 Conflict` | `SeguimientoService_ConSeguimientoDelDia_LanzaDuplicadoDiaException` | `POST /api/procesos/{id}/seguimientos_ConSeguimientoDelDia_Returns409` |
| 3d. Estado no válido | `409 Conflict` | `SeguimientoService_ConEstadoInvalido_LanzaEstadoException` | `POST /api/procesos/{id}/seguimientos_ConEstadoInvalido_Returns409` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).