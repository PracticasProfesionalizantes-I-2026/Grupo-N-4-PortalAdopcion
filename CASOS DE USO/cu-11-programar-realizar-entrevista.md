# Caso de Uso: Programar y Realizar Entrevista

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-11 |
| **Nombre** | Programar y Realizar Entrevista |
| **Actor Principal** | Administrador / Voluntario (Personal de Adopciones) |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Personal de Adopciones → evaluar la compatibilidad del adoptante con la mascota; Adoptante → participar del encuentro con fecha y medio asignados; ONG → avanzar el proceso hacia el periodo de prueba; Administración → auditar la decisión de la entrevista |
| **Disparador (Trigger)** | El Personal de Adopciones programa una entrevista para una solicitud aprobada (CU-04) |
| **Prioridad / Frecuencia** | Alta; alta frecuencia (núcleo del workflow) |
| **Reglas de negocio relacionadas** | RN-05/RN-06 (decisiones auditadas con la entidad autorizante y la fecha; el rechazo exige motivo); RN-09 (la mascota debe continuar disponible); RN-08 (auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite al Personal de Adopciones programar y realizar la entrevista con el
adoptante de una solicitud aprobada; según el resultado, el proceso pasa a
`En Periodo de Prueba` o `Rechazada`.

### 2. PRECONDICIONES
1. El actor debe estar autenticado (Token JWT válido) con permisos de evaluación.
2. La solicitud debe existir y encontrarse en estado `Aprobada` (CU-04).
3. La mascota debe continuar disponible para adopción (**RN-09**).
4. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/solicitudes/{id}/entrevistas` con un JSON que contiene la fecha/hora, el medio (presencial, videollamada o telefónica) y el solicitante.
2. La **Capa de Presentación** (`EntrevistaController`) valida que el JSON sea estructuralmente correcto y que la fecha sea válida y futura.
3. La **Capa de Negocio** (`EvaluacionService`) verifica que la solicitud esté en estado `Aprobada` y que la mascota continúe disponible (**RN-09**).
4. La **Capa de Persistencia** registra la entrevista programada.
5. Al realizarse la entrevista, el Actor registra el resultado (favorable o desfavorable con motivo).
6. El Sistema actualiza el estado del proceso: favorable → `En Periodo de Prueba` (**RN-13**); desfavorable → `Rechazada` con motivo (**RN-05**).
7. El Sistema registra la decisión con la entidad autorizante y la fecha (**RN-06**) y emite la auditoría (**RN-08**).
8. El Sistema devuelve un código **201 Created** (programación) o **200 OK** (resultado registrado).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Fecha/hora inválida (HTTP 400 Bad Request):**
  1. Si en el Paso 2 la fecha no es futura, está vacía o el medio no es válido.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Solicitud inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `{id}` de la solicitud no existe en la **Capa de Persistencia**.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3b. Solicitud no aprobada (HTTP 409 Conflict):**
  1. Si en el Paso 3 la solicitud no se encuentra en estado `Aprobada` (estado evaluable).
  2. El Sistema frena la ejecución en la **Capa de Negocio**.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **3c. Mascota ya no disponible (HTTP 409 Conflict):**
  1. Si en el Paso 3 la mascota cambió de estado durante el proceso (**RN-09**).
  2. El Sistema frena la autorización y avisa que la adopción no puede continuar.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **3d. Sin permiso de evaluación (HTTP 403 Forbidden):**
  1. Si el actor no posee el rol requerido para programar/realizar entrevistas.
  2. El Sistema rechaza la petición por autorización.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la **Capa de Persistencia** no puede registrar la entrevista o el resultado.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Cancelación / reprogramación de la entrevista:** el Personal puede cancelar o reprogramar la fecha antes de su realización; la solicitud mantiene su estado `Aprobada` (no es un error).
2. **Resultados de la entrevista:** favorable → `En Periodo de Prueba` (CU-12/CU-15); desfavorable → `Rechazada` con motivo obligatorio (**RN-05**).
3. El medio puede ser presencial, videollamada o telefónica (agenda de entrevistas del día); el esquema de registro es el mismo.

### 6. POSTCONDICIONES
1. La entrevista queda programada (y registrada su fecha/hora y medio) o su resultado queda persistido.
2. El proceso pasa a `En Periodo de Prueba` o `Rechazada`, según el resultado (**RN-13/RN-05**).
3. La decisión queda auditada con la entidad autorizante, responsable y fecha (**RN-06/RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de programación de la entrevista. |
| `200` | OK | Confirmación del resultado de la entrevista registrado. |
| `400` | Bad Request | Fallo en la validación de esquema, fecha inválida o medio no válido. |
| `403` | Forbidden | Sin permiso de evaluación. |
| `404` | Not Found | Solicitud referenciada inexistente. |
| `409` | Conflict | Violación de invariantes (solicitud no aprobada; mascota no disponible). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON, fecha válida/futura y medio válido.
- **Verificación (Negocio, → 403/404/409):** autorización de evaluación; existencia de la solicitud (`SolicitudNotFoundException` → 404); estado `Aprobada` y mascota disponible (**RN-09** → 409). Auditoría (**RN-08**) y registro de autorización (**RN-06**) emitidos en la Capa de Negocio.

### Matriz de trazabilidad CU-11 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (programar) | `201 Created` | `EvaluacionService_ProgramarEntrevista_RegistraCita` | `POST /api/solicitudes/{id}/entrevistas_ProgramaEntrevista_Returns201` |
| Flujo principal (resultado) | `200 OK` | `EvaluacionService_RegistrarResultadoEntrevista_ActualizaProceso` | `PATCH /api/solicitudes/{id}/entrevistas/resultado_RegistraResultado_Returns200` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/solicitudes/{id}/entrevistas_ConJsonInvalido_Returns400` |
| 2a. Fecha inválida | `400 Bad Request` | — (se detecta vía validación de esquema) | `POST /api/solicitudes/{id}/entrevistas_ConFechaInvalida_Returns400` |
| 3a. Solicitud inexistente | `404 Not Found` | `EvaluacionService_ConSolicitudInexistente_LanzaSolicitudNotFoundException` | `POST /api/solicitudes/{id}/entrevistas_ConSolicitudInexistente_Returns404` |
| 3b. Solicitud no aprobada | `409 Conflict` | `EvaluacionService_ConSolicitudNoAprobada_LanzaEstadoException` | `POST /api/solicitudes/{id}/entrevistas_ConSolicitudNoAprobada_Returns409` |
| 3c. Mascota no disponible | `409 Conflict` | `EvaluacionService_ConMascotaNoDisponible_LanzaMascotaNoDisponibleException` | `POST /api/solicitudes/{id}/entrevistas_ConMascotaNoDisponible_Returns409` |
| 3d. Sin permiso | `403 Forbidden` | — (se resuelve en la autorización) | `POST /api/solicitudes/{id}/entrevistas_SinPermiso_Returns403` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).