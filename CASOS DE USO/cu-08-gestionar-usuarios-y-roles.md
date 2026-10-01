# Caso de Uso: Gestionar Usuarios y Roles

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-08 |
| **Nombre** | Gestionar Usuarios y Roles |
| **Actor Principal** | Administrador del Sistema |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Administración → controlar accesos, roles y estados de cuenta; Usuario → ver actualizado su rol/estado y los permisos asociados; Sistema → registrar cada cambio (auditoría) |
| **Disparador (Trigger)** | El administrador modifica el rol o el estado (activa, suspendida, baja) de una cuenta de usuario |
| **Prioridad / Frecuencia** | Media; media frecuencia (gestiones administrativas ocasionales) |
| **Reglas de negocio relacionadas** | RN-17 (solo personal autorizado gestiona usuarios; roles válidos); RN-08 (toda modificación queda auditada) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador modificar el rol y el estado de las cuentas de usuario,
manteniendo la trazabilidad de cada cambio en el módulo de auditoría.

### 2. PRECONDICIONES
1. El actor debe estar autenticado (Token JWT válido) con rol **Administrador** (**RN-17**).
2. La cuenta a modificar debe existir en la **Capa de Persistencia**.
3. El sistema debe estar operativo y con acceso a la base de datos.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `PUT /api/usuarios/{id}/rol` (o `PUT /api/usuarios/{id}/estado`) con un JSON que contiene el nuevo rol y/o estado de la cuenta.
2. La **Capa de Presentación** (`UsuariosController`) valida que el JSON sea estructuralmente correcto y que el rol/estado sea un valor válido.
3. La **Capa de Negocio** (`UsuarioService`) verifica la autorización (**RN-17**), valida la transición de estado y la regla de no degradar al **último administrador**.
4. La **Capa de Persistencia** actualiza el rol y/o el estado de la cuenta.
5. El Sistema registra la operación en auditoría con el responsable y la fecha (**RN-08**).
6. El Sistema devuelve un código **200 OK** con los datos actualizados (o **204 No Content** si no hay cuerpo).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Rol o estado inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el rol o el estado enviado no pertenece al conjunto válido.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Usuario inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `{id}` no corresponde a ninguna cuenta registrada.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3b. Sin permiso de administración (HTTP 403 Forbidden):**
  1. Si el actor no posee rol de administración (**RN-17**).
  2. El Sistema (Capa de Presentación o Middleware de autorización) rechaza la petición.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **3c. No degradar al último administrador (HTTP 409 Conflict):**
  1. Si en el Paso 3 la operación degrada al último administrador activo del sistema.
  2. La **Capa de Negocio** frena la operación.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **3d. Transición de estado inválida (HTTP 409 Conflict):**
  1. Si en el Paso 3 la cuenta se encuentra en un estado que impide la transición solicitada (p.ej. baja definitiva).
  2. La **Capa de Negocio** frena la operación.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la **Capa de Persistencia** no puede actualizar la cuenta.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Suspensión / activación (PATCH):** el administrador suspende (blacklist manual) o reactiva una cuenta mediante actualización de estado.
2. **Baja de cuenta (DELETE, → 204):** la baja es *soft*: la cuenta queda desactivada sin eliminar su historial (trazabilidad — RN-08).
3. El actor puede enviar el JSON desde el panel web o desde un cliente HTTP; el esquema y el resultado son idénticos.

### 6. POSTCONDICIONES
1. El rol y/o el estado de la cuenta quedan actualizados en la **Capa de Persistencia**.
2. Los permisos del usuario se ajustan según el nuevo rol/estado (impacto en las funcionalidades accesibles).
3. El cambio queda registrado en auditoría con el responsable y la fecha/hora (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Confirmación de actualización de rol/estado (con cuerpo). |
| `204` | No Content | Confirmación de baja sin cuerpo en la respuesta. |
| `400` | Bad Request | Fallo en la validación de esquema o valores inválidos. |
| `403` | Forbidden | Sin autorización de administración (RN-17). |
| `404` | Not Found | Cuenta referenciada inexistente. |
| `409` | Conflict | Violación de invariantes (último administrador, transición inválida). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON y valores de rol/estado.
- **Verificación (Negocio, → 403/404/409):** autorización (**RN-17**); existencia de la cuenta (`UsuarioNotFoundException` → 404); no degradar al último administrador y transiciones válidas (`EstadoException` → 409). Auditoría (**RN-08**) emitida en la Capa de Negocio.

### Matriz de trazabilidad CU-08 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `UsuarioService_ModificarRol_ActualizaCuenta` | `PUT /api/usuarios/{id}/rol_ModificaRol_Returns200` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `PUT /api/usuarios/{id}/rol_ConJsonInvalido_Returns400` |
| 2a. Rol inválido | `400 Bad Request` | — (se detecta vía validación de esquema) | `PUT /api/usuarios/{id}/rol_ConRolInvalido_Returns400` |
| 3a. Usuario inexistente | `404 Not Found` | `UsuarioService_ConUsuarioInexistente_LanzaUsuarioNotFoundException` | `PUT /api/usuarios/{id}/rol_ConUsuarioInexistente_Returns404` |
| 3b. Sin permiso | `403 Forbidden` | — (se resuelve en la autorización) | `PUT /api/usuarios/{id}/rol_SinPermisoAdmin_Returns403` |
| 3c. Último administrador | `409 Conflict` | `UsuarioService_DegradarUltimoAdministrador_LanzaExcepcion` | `PUT /api/usuarios/{id}/rol_DegradaUltimoAdmin_Returns409` |
| 3d. Transición inválida | `409 Conflict` | `UsuarioService_ConTransicionInvalida_LanzaEstadoException` | `PUT /api/usuarios/{id}/rol_ConTransicionInvalida_Returns409` |
| 5.2 Baja de cuenta | `204 No Content` | `UsuarioService_DarDeBaja_DesactivaCuenta` | `DELETE /api/usuarios/{id}_DaDeBaja_Returns204` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).