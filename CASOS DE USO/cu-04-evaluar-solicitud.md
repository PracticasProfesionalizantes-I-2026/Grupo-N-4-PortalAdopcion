# Caso de Uso: Evaluar Solicitud

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-04 |
| **Nombre** | Evaluar Solicitud |
| **Actor Principal** | Administrador / Voluntario (Personal de Adopciones) |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Personal de Adopciones → aprobar, rechazar o dejar en observación una postulación; Adoptante → conocer la resolución de su solicitud; ONG → asegurar que la adopción sea viable; Mascota → continuar disponible solo si la solicitud es viable (RN-09); Administración → auditoría de cada decisión |
| **Disparador (Trigger)** | El Personal de Adopciones selecciona una solicitud en estado `Pendiente` (o `En Observación`) para iniciar su análisis |
| **Prioridad / Frecuencia** | Alta; alta frecuencia (núcleo del workflow de adopción) |
| **Reglas de negocio relacionadas** | RN-05 (mínimo 15 días desde el inicio para aprobar; motivo obligatorio en rechazo; estado En Observación); RN-06 (toda decisión registra la entidad autorizante y la fecha); RN-01 (blacklist bloquea la autorización); RN-09 (la mascota debe continuar disponible) |

---

### 1. BREVE DESCRIPCIÓN
Permite al Personal de Adopciones evaluar una solicitud previamente registrada,
verificando el cumplimiento de los requisitos y determinando si la adopción se
aprueba, se rechaza o queda en observación, con auditoría de cada decisión.

### 2. PRECONDICIONES
1. El usuario debe estar autenticado (Token JWT válido) con permisos para evaluar solicitudes de adopción.
2. La solicitud debe existir y encontrarse en estado `Pendiente` (o `En Observación`).
3. El adoptante asociado debe permanecer activo y no encontrarse en blacklist (**RN-01**).
4. La mascota debe continuar disponible para adopción (**RN-09**).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `PATCH /api/solicitudes/{id}/evaluacion` con un JSON que contiene el resultado de la evaluación (`Aprobada`, `Rechazada` o `En Observación`), las observaciones y el motivo (obligatorio cuando el resultado es rechazo — **RN-05**).
2. La **Capa de Presentación** (`EvaluacionController`) valida que el JSON sea estructuralmente correcto y que el motivo esté presente en caso de rechazo.
3. La **Capa de Negocio** (`EvaluacionService`) verifica que la solicitud exista y esté en estado evaluable (**RN-05**), que hayan transcurrido al menos 15 días desde el inicio del proceso (**RN-05**), que la mascota continúe disponible (**RN-09**) y que el adoptante no esté en blacklist (**RN-01**).
4. La **Capa de Persistencia** actualiza el estado de la solicitud (`Aprobada` / `Rechazada` / `En Observación`) y registra observaciones, motivo y fecha/hora de resolución.
5. El Sistema registra la entidad autorizante, el usuario responsable y la fecha (**RN-06**) y emite el evento de auditoría (**RN-08**).
6. El Sistema devuelve un código **200 OK** con el nuevo estado de la solicitud. Si fue aprobada, queda habilitada la entrevista (CU-11).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Motivo de rechazo faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el resultado es `Rechazada` y no se incluye un motivo, violando **RN-05**.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Solicitud inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `{id}` de la solicitud no existe en la **Capa de Persistencia**.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3b. Solicitud ya evaluada (HTTP 409 Conflict):**
  1. Si en el Paso 3 la solicitud ya fue resuelta (estado terminal).
  2. El Sistema frena la ejecución en la **Capa de Negocio**.
  3. El Sistema devuelve un código **409 Conflict** informando que no puede volver a evaluarse. Fin del caso de uso.

* **3c. Evaluación antes de los 15 días mínimos (HTTP 409 Conflict):**
  1. Si en el Paso 3 se intenta aprobar antes de transcurridos 15 días desde el inicio del proceso, violando **RN-05**.
  2. El Sistema frena la ejecución en la **Capa de Negocio**.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **3d. La mascota ya no está disponible (HTTP 409 Conflict):**
  1. Si en el Paso 3 se detecta que la mascota cambió de estado durante la evaluación, violando **RN-09**.
  2. El Sistema informa que la adopción no puede continuar y la solicitud pasa al estado `Cancelada`, registrando el motivo.
  3. El Sistema devuelve un código **409 Conflict** y registra el evento en auditoría. Fin del caso de uso.

* **3e. Adoptante incorporado a la Blacklist (HTTP 409 Conflict):**
  1. Si en el Paso 3 el adoptante fue incluido en la blacklist antes de finalizar la evaluación, violando **RN-01**.
  2. El Sistema bloquea la autorización; la solicitud pasa automáticamente al estado `Rechazada`, con el motivo registrado.
  3. El Sistema devuelve un código **409 Conflict** y registra el evento en auditoría. Fin del caso de uso.

* **5a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la **Capa de Persistencia** no puede actualizar la solicitud.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Dejar en observación:** si la información presentada resulta insuficiente, el Personal registra observaciones y la solicitud queda en `En Observación`, manteniéndose evaluable (**RN-05**).
2. **Cancelación de la evaluación:** el evaluador puede descartar la resolución temporal antes de confirmarla; la solicitud mantiene su estado y no se registra ninguna decisión (no es un error).
3. El actor puede enviar el JSON desde el panel web o desde un cliente HTTP; el esquema y el resultado son idénticos.

### 6. POSTCONDICIONES
1. La solicitud queda evaluada con estado `Aprobada`, `Rechazada` o `En Observación`, según corresponda.
2. Si la solicitud fue aprobada, queda habilitada la creación de la entrevista (CU-11).
3. La decisión queda auditada con la entidad autorizante, el usuario responsable y la fecha/hora (**RN-06/RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Confirmación de la evaluación aplicada (Aprobada / Rechazada / En Observación). |
| `400` | Bad Request | Fallo en la validación de esquema o motivo de rechazo faltante (RN-05). |
| `404` | Not Found | Solicitud referenciada inexistente en la Capa de Persistencia. |
| `409` | Conflict | Violación de invariantes o concurrencia (RN-05 15 días, RN-09 mascota no disponible, RN-01 blacklist, solicitud ya evaluada). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON y presencia del motivo en rechazo (**RN-05**).
- **Verificación (Negocio, → 404/409):** existencia y estado evaluable de la solicitud (`SolicitudNotFoundException` → 404; `YaEvaluadaException` → 409); transcurso de 15 días (**RN-05**); mascota disponible (**RN-09**); adoptante no en blacklist (**RN-01**). Auditoría (**RN-08**) y registro de autorización (**RN-06**) emitidos en la Capa de Negocio.

### Matriz de trazabilidad CU-04 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `EvaluacionService_AprobarSolicitud_ActualizaEstado` | `PATCH /api/solicitudes/{id}/evaluacion_Aprueba_Returns200` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `PATCH /api/solicitudes/{id}/evaluacion_ConJsonInvalido_Returns400` |
| 2a. Rechazo sin motivo | `400 Bad Request` | — (se detecta vía `[Required]` en Presentación) | `PATCH /api/solicitudes/{id}/evaluacion_RechazoSinMotivo_Returns400` |
| 3a. Solicitud inexistente | `404 Not Found` | `EvaluacionService_ConSolicitudInexistente_LanzaSolicitudNotFoundException` | `PATCH /api/solicitudes/{id}/evaluacion_ConSolicitudInexistente_Returns404` |
| 3b. Solicitud ya evaluada | `409 Conflict` | `EvaluacionService_ConSolicitudYaEvaluada_LanzaYaEvaluadaException` | `PATCH /api/solicitudes/{id}/evaluacion_ConSolicitudYaEvaluada_Returns409` |
| 3c. Antes de los 15 días | `409 Conflict` | `EvaluacionService_AntesDe15Dias_LanzaValidationException` | `PATCH /api/solicitudes/{id}/evaluacion_AntesDe15Dias_Returns409` |
| 3d. Mascota no disponible | `409 Conflict` | `EvaluacionService_ConMascotaNoDisponible_CancelaSolicitud` | `PATCH /api/solicitudes/{id}/evaluacion_ConMascotaNoDisponible_Returns409` |
| 3e. Blacklist durante evaluación | `409 Conflict` | `EvaluacionService_ConBlacklist_RechazaAutomaticamente` | `PATCH /api/solicitudes/{id}/evaluacion_ConBlacklist_Returns409` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).