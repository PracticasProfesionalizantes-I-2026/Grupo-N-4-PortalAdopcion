# Caso de Uso: Finalizar Adopción y Transferir Historial

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-15 |
| **Nombre** | Finalizar Adopción y Transferir Historial |
| **Actor Principal** | Administrador / Voluntario (Personal autorizado) |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | ONG → cerrar la adopción formalmente; Adoptante → recibir la mascota y su historial clínico transferido; Veterinario → validar el seguimiento cuando el estado esté en alerta (RN-13); Administración → auditoría del cierre |
| **Disparador (Trigger)** | El personal confirma el cierre de la adopción al finalizar el periodo de prueba de la mascota |
| **Prioridad / Frecuencia** | Alta; media frecuencia (un cierre por adopción) |
| **Reglas de negocio relacionadas** | RN-13 (periodo de prueba de 15 días; si el seguimiento está en alerta, no se puede finalizar sin validación del veterinario); RN-08 (auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite cerrar formalmente la adopción al cumplirse el periodo de prueba,
actualizando el proceso y la mascota al estado `Adoptada`, y transfieriendo
(clonando) el historial clínico al perfil del adoptante.

### 2. PRECONDICIONES
1. El actor debe estar autenticado (Token JWT válido) con permisos para finalizar adopciones.
2. El proceso debe encontrarse en estado `En Periodo de Prueba` (**RN-13**).
3. El seguimiento del periodo debe estar aprobado; si está en alerta, debe existir validación del veterinario (**RN-13**).
4. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `POST /api/solicitudes/{id}/finalizar` con un JSON que contiene la confirmación del cierre y el usuario responsable.
2. La **Capa de Presentación** (`AdopcionController`) valida que el JSON sea estructuralmente correcto y los datos obligatorios.
3. La **Capa de Negocio** (`AdopcionService`) verifica que el proceso esté en `En Periodo de Prueba`, que el seguimiento esté aprobado y, si estuviera en alerta, que exista la validación del veterinario (**RN-13**).
4. La **Capa de Persistencia** actualiza el proceso y la mascota al estado `Adoptada`, **clona** el historial clínico al perfil del adoptante (transacción multi-entidad) y registra fecha y responsable.
5. El Sistema registra la operación en auditoría (**RN-08**).
6. El Sistema devuelve un código **200 OK** con la confirmación de la adopción finalizada.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Solicitud/proceso inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `{id}` no existe en la **Capa de Persistencia**.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3b. Proceso no en periodo de prueba (HTTP 409 Conflict):**
  1. Si en el Paso 3 el proceso no se encuentra en `En Periodo de Prueba`.
  2. El Sistema frena la ejecución en la **Capa de Negocio**.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **3c. Seguimiento en alerta sin validación del veterinario (HTTP 409 Conflict):**
  1. Si en el Paso 3 el seguimiento está en `Alerta` y no existe la validación del veterinario (**RN-13**).
  2. El Sistema frena la finalización.
  3. El Sistema devuelve un código **409 Conflict** solicitando la validación veterinaria. Fin del caso de uso.

* **3d. Sin permiso para finalizar (HTTP 403 Forbidden):**
  1. Si el actor no posee el rol requerido para cerrar adopciones.
  2. El Sistema rechaza la petición por autorización.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **4a. Fallo de persistencia / clonado del historial (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la transacción multi-entidad (proceso + mascota + clonado del historial) no se completa.
  2. El Sistema realiza **rollback transaccional** y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. La transferencia del historial es un **clonado íntegro** al perfil del adoptante; el historial original de la mascota queda archivado (trazabilidad — RN-08).
2. El cierre solo es válido cuando el seguimiento del periodo está aprobado (RN-13); la duración por defecto del periodo es de 15 días.

### 6. POSTCONDICIONES
1. El proceso y la mascota quedan persistidos en estado `Adoptada`.
2. El historial clínico queda clonado/transferido al perfil del adoptante.
3. La operación queda registrada en auditoría con el responsable y la fecha/hora (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Confirmación de la adopción finalizada y el historial transferido. |
| `400` | Bad Request | Fallo en la validación de esquema. |
| `403` | Forbidden | Sin permiso para finalizar adopciones. |
| `404` | Not Found | Solicitud/proceso referenciado inexistente. |
| `409` | Conflict | Violación de invariantes (RN-13: estado no válido, alerta sin validación veterinaria). |
| `500` | Internal Server Error | Error técnico no controlado (rollback de la transacción multi-entidad). |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON y datos obligatorios.
- **Verificación (Negocio, → 403/404/409):** permiso de cierre; existencia de la entidad (`SolicitudNotFoundException` → 404); estado `En Periodo de Prueba` y validación veterinaria de la alerta (**RN-13** → 409). El clonado íntegro del historial se ejecuta de forma transaccional en la Capa de Negocio; la auditoría (**RN-08**) cierra la operación.

### Matriz de trazabilidad CU-15 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `AdopcionService_FinalizarAdopcion_ClonaHistorial` | `POST /api/solicitudes/{id}/finalizar_FinalizaAdopcion_Returns200` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/solicitudes/{id}/finalizar_ConJsonInvalido_Returns400` |
| 3a. Proceso inexistente | `404 Not Found` | `AdopcionService_ConProcesoInexistente_LanzaSolicitudNotFoundException` | `POST /api/solicitudes/{id}/finalizar_ConProcesoInexistente_Returns404` |
| 3b. No en periodo de prueba | `409 Conflict` | `AdopcionService_NoEnPeriodoDePrueba_LanzaEstadoException` | `POST /api/solicitudes/{id}/finalizar_NoEnPeriodoDePrueba_Returns409` |
| 3c. Alerta sin validación veterinaria | `409 Conflict` | `AdopcionService_ConAlertaSinValidacionVet_LanzaEstadoException` | `POST /api/solicitudes/{id}/finalizar_ConAlertaSinValidacionVet_Returns409` |
| 3d. Sin permiso | `403 Forbidden` | — (se resuelve en la autorización) | `POST /api/solicitudes/{id}/finalizar_SinPermiso_Returns403` |
| 4a. Fallo del clonado | `500 Internal Server Error` | `AdopcionService_ConFalloCloneHistorial_Rollback` | `POST /api/solicitudes/{id}/finalizar_ConFalloClonado_Returns500` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).