# Caso de Uso: Cancelar Proceso de Adopción

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-13 |
| **Nombre** | Cancelar Proceso de Adopción |
| **Actor Principal** | Adoptante / Administrador (Personal autorizado) |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Adoptante → renunciar al proceso con motivo registrado; ONG → liberar la mascota para nuevas postulaciones; Administración → aplicar sanción si la cancelación ocurre en periodo de prueba (RN-12) |
| **Disparador (Trigger)** | El adoptante (o el personal autorizado) solicita cancelar un proceso de adopción activo |
| **Prioridad / Frecuencia** | Media; media frecuencia (cancelaciones ocasionales) |
| **Reglas de negocio relacionadas** | RN-11 (la cancelación registra motivo y usuario responsable); RN-12 (cancelación en periodo de prueba → sanción de 1 año de blacklist); RN-01/RN-15 (aplicación y persistencia de la sanción); RN-08 (auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite cancelar un proceso de adopción activo registrando el motivo y el
responsable; si la cancelación ocurre durante el periodo de prueba, el sistema
aplica una sanción de blacklist de 1 año (**RN-12**).

### 2. PRECONDICIONES
1. El actor debe estar autenticado (Token JWT válido) y ser el adoptante titular del proceso o personal autorizado.
2. El proceso debe existir y encontrarse en un estado cancelable (activo, no terminal) (**RN-11**).
3. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `POST /api/solicitudes/{id}/cancelar` con un JSON que contiene el motivo de la cancelación (obligatorio) y el responsable (**RN-11**).
2. La **Capa de Presentación** (`SolicitudesController`) valida que el JSON sea estructuralmente correcto y que el motivo no esté vacío.
3. La **Capa de Negocio** (`SolicitudService`) verifica que el proceso exista y sea cancelable, valida el responsable (**RN-11**) y determina si la cancelación ocurre durante el periodo de prueba.
4. Si la cancelación ocurre en `En Periodo de Prueba`, el Sistema aplica la sanción de blacklist de 1 año (**RN-12**) ligada al perfil del adoptante (**RN-15**).
5. La **Capa de Persistencia** actualiza el proceso a `Cancelada`, registra motivo, usuario responsable y fecha/hora; si aplica, registra la sanción.
6. El Sistema registra la operación en auditoría (**RN-08**).
7. El Sistema devuelve un código **200 OK** con el estado `Cancelada` de la solicitud.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Motivo de cancelación vacío (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el motivo llega vacío o con solo espacios, violando **RN-11**.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Solicitud inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `{id}` de la solicitud no existe en la **Capa de Persistencia**.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3b. Adoptante no titular del proceso (HTTP 403 Forbidden):**
  1. Si en el Paso 3 el actor es un adoptante que no es el titular del proceso.
  2. El Sistema rechaza la operación por autorización.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **3c. Proceso en estado terminal (HTTP 409 Conflict):**
  1. Si en el Paso 3 el proceso ya se encuentra en un estado terminal (`Adoptada`, `Rechazada`, `Cancelada`, `Abandonada`).
  2. El Sistema frena la ejecución en la **Capa de Negocio**.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **5a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 5 la **Capa de Persistencia** no puede actualizar el proceso o registrar la sanción.
  2. El Sistema interrumpe la operación (rollback transaccional) y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Cancelación durante el periodo de prueba:** aplica automáticamente la sanción de 1 año de blacklist (**RN-12/RN-01**) y el adoptante no podrá postular durante ese período.
2. **Cancelación fuera del periodo de prueba:** el proceso se cancela con motivo y responsable, sin aplicación de sanción.
3. Tras la cancelación, la mascota libera su reserva y vuelve a aparecer como disponible (impacto en visibilidad — **RN-04**).

### 6. POSTCONDICIONES
1. El proceso queda persistido en estado `Cancelada` con motivo, responsable y fecha/hora (**RN-11**).
2. Si aplica, se persiste la sanción de blacklist de 1 año ligada al perfil del adoptante (**RN-12/RN-15**); el adoptante no podrá postular durante ese período (**RN-01**).
3. La operación queda registrada en auditoría (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Confirmación de la cancelación del proceso. |
| `400` | Bad Request | Fallo en la validación de esquema o motivo vacío (RN-11). |
| `403` | Forbidden | Adoptante no titular del proceso. |
| `404` | Not Found | Solicitud referenciada inexistente. |
| `409` | Conflict | Proceso en estado terminal (no cancelable). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON y motivo no vacío (**RN-11**).
- **Verificación (Negocio, → 403/404/409):** titularidad del proceso; existencia (`SolicitudNotFoundException` → 404); estado cancelable (`EstadoException` → 409). La sanción de 1 año se aplica automáticamente en la Capa de Negocio (**RN-12/RN-15**). Auditoría (**RN-08**) emitida en la Capa de Negocio.

### Matriz de trazabilidad CU-13 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `SolicitudService_CancelarProceso_ActualizaEstadoCancelada` | `POST /api/solicitudes/{id}/cancelar_CancelaProceso_Returns200` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/solicitudes/{id}/cancelar_ConJsonInvalido_Returns400` |
| 2a. Motivo vacío | `400 Bad Request` | `SolicitudService_ConMotivoVacio_LanzaValidationException` | `POST /api/solicitudes/{id}/cancelar_SinMotivo_Returns400` |
| 3a. Solicitud inexistente | `404 Not Found` | `SolicitudService_ConSolicitudInexistente_LanzaSolicitudNotFoundException` | `POST /api/solicitudes/{id}/cancelar_ConSolicitudInexistente_Returns404` |
| 3b. Adoptante no titular | `403 Forbidden` | `SolicitudService_ConAdoptanteNoTitular_LanzaAccesoDenegadoException` | `POST /api/solicitudes/{id}/cancelar_ConAdoptanteNoTitular_Returns403` |
| 3c. Proceso en estado terminal | `409 Conflict` | `SolicitudService_Cancelar_ConEstadoTerminal_LanzaEstadoException` | `POST /api/solicitudes/{id}/cancelar_ConEstadoTerminal_Returns409` |
| 5.1 Cancelación en periodo de prueba | `200 OK` | `SolicitudService_CancelacionEnPrueba_Sanciona1Anio` | `POST /api/solicitudes/{id}/cancelar_EnPeriodoDePrueba_Returns200` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).