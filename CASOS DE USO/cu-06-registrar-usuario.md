# Caso de Uso: Registrar Usuario

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-06 |
| **Nombre** | Registrar Usuario |
| **Actor Principal** | Usuario nuevo (Adoptante) / Administrador (roles de personal) |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Usuario nuevo → obtener una cuenta para ingresar y postular; ONG → gestionar los accesos según rol (adoptante, voluntario, veterinario, administrador); Administración → asegurar unicidad de identidades y contraseñas seguras |
| **Disparador (Trigger)** | Un usuario nuevo solicita registrarse, o el administrador crea una cuenta de personal con clave de invitación de la ONG |
| **Prioridad / Frecuencia** | Alta; alta frecuencia (adoptantes) / media (personal) |
| **Reglas de negocio relacionadas** | RN-17 (unicidad de email, DNI y matrícula del veterinario; contraseña segura; solo personal autorizado registra usuarios); RN-08 (auditoría); clave de invitación para roles de personal |

---

### 1. BREVE DESCRIPCIÓN
Permite registrar una cuenta de usuario en el sistema con uno de los cuatro roles
(Adoptante, Voluntario, Veterinario, Administrador), quedando activa y permitiendo
el posterior inicio de sesión (CU-07).

### 2. PRECONDICIONES
1. Para roles de personal (Voluntario, Veterinario, Administrador), el registro debe ser iniciado por un Administrador con la clave de invitación de la ONG (**RN-17**).
2. Los datos personales (nombre, DNI, email, teléfono) no deben estar registrados previamente (**RN-17**).
3. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/usuarios` con un JSON que contiene los datos personales (nombre, DNI, email, teléfono), el rol solicitado y la contraseña; para roles de personal se incluye además la clave de invitación.
2. La **Capa de Presentación** (`UsuariosController`) valida que el JSON sea estructuralmente correcto, los campos obligatorios y la política de contraseña (**RN-17**).
3. La **Capa de Negocio** (`UsuarioService`) verifica la unicidad de email, DNI y matrícula del veterinario (**RN-17**), valida la clave de invitación para roles de personal y construye la entidad `Usuario`.
4. La **Capa de Persistencia** guarda la cuenta con estado `Activa`, el rol asignado y la contraseña con hash.
5. El Sistema registra la operación en auditoría (**RN-08**).
6. El Sistema devuelve un código **201 Created** con los datos de la cuenta creada (sin credenciales).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 falta algún dato obligatorio (nombre, DNI, email, teléfono o contraseña).
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request** detallando el campo faltante. Fin del caso de uso.

* **2b. Contraseña débil (HTTP 400 Bad Request):**
  1. Si en el Paso 2 la contraseña no cumple la longitud mínima ni la complejidad requerida (**RN-17**).
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request** indicando la política incumplida. Fin del caso de uso.

* **3a. Email, DNI o matrícula duplicados (HTTP 409 Conflict):**
  1. Si en el Paso 3 la verificación determina que el email, el DNI (o la matrícula, en el caso de un veterinario) ya existen, violando **RN-17**.
  2. El Sistema frena la ejecución en la **Capa de Negocio** y lanza la excepción de dominio correspondiente.
  3. El Sistema devuelve un código **409 Conflict**. Fin del caso de uso.

* **3b. Clave de invitación inválida (HTTP 403 Forbidden):**
  1. Si en el Paso 3 se intenta registrar un rol de personal con una clave de invitación inexistente o expirada, o sin autorización (**RN-17**).
  2. El Sistema (Capa de Negocio) rechaza la operación.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la **Capa de Persistencia** no puede guardar la cuenta.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Registro público (Adoptante):** sin clave de invitación; el rol se asigna por defecto como `Adoptante`.
2. **Alta de personal:** ejecutada por el Administrador con la clave de invitación (**CU-08**); aplica a Voluntarios, Veterinarios y Administradores.
3. **Selección de rol en pantalla de inicio:** la pantalla "Seleccione su rol" del mockup mapea implícitamente los roles creados en este caso de uso.

### 6. POSTCONDICIONES
1. Se crea una cuenta persistente con estado `Activa` y el rol asignado.
2. El usuario puede iniciar sesión con el email y la contraseña elegidos (CU-07).
3. La operación queda registrada en auditoría (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa de la nueva cuenta. |
| `400` | Bad Request | Fallo en validación de esquema, datos faltantes o contraseña débil (RN-17). |
| `403` | Forbidden | Clave de invitación inválida o sin autorización para registrar personal (RN-17). |
| `409` | Conflict | Violación de unicidad de email, DNI o matrícula (RN-17). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON, campos obligatorios y política de contraseña (**RN-17**).
- **Verificación (Negocio, → 403/409):** clave de invitación para roles de personal (`InviteException` → 403); unicidad de email/DNI/matrícula (`EmailDuplicadoException` / `DocumentoDuplicadoException` / `MatriculaDuplicadaException` → 409). Auditoría (**RN-08**) emitida en la Capa de Negocio.

### Matriz de trazabilidad CU-06 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `UsuarioService_CrearUsuario_RetornaCuentaActiva` | `POST /api/usuarios_CreaCuenta_Returns201` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/usuarios_ConJsonInvalido_Returns400` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (se detecta vía `[Required]` en Presentación) | `POST /api/usuarios_ConDatosFaltantes_Returns400` |
| 2b. Contraseña débil | `400 Bad Request` | — (validación de política en Presentación) | `POST /api/usuarios_ConContraseñaDebil_Returns400` |
| 3a. Email/DNI/matrícula duplicado | `409 Conflict` | `UsuarioService_ConEmailDuplicado_LanzaEmailDuplicadoException` | `POST /api/usuarios_ConEmailDuplicado_Returns409` |
| 3b. Clave de invitación inválida | `403 Forbidden` | `UsuarioService_ConInvitacionInvalida_LanzaInviteException` | `POST /api/usuarios_ConInvitacionInvalida_Returns403` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).