# Caso de Uso: Iniciar Sesión

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.
> Decisión de diseño documentada: la cuenta **suspendida** (blacklist) **puede
> ingresar** pero no postular (RN-01); consultar
> `COSAS/Modelo-de-Estados-y-Reglas-de-Negocio.md`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-07 |
| **Nombre** | Iniciar Sesión |
| **Actor Principal** | Usuario registrado (Adoptante, Voluntario, Veterinario, Administrador) |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Usuario → acceder a sus funcionalidades según rol; Sistema → autenticar de forma segura y emitir el token; Administración → registro de accesos (auditoría) |
| **Disparador (Trigger)** | El usuario ingresa sus credenciales en la pantalla de inicio de sesión |
| **Prioridad / Frecuencia** | Alta; muy alta frecuencia |
| **Reglas de negocio relacionadas** | RN-17 (verificación contra hash de contraseña y estados de cuenta); RN-01 (cuenta suspendida puede ingresar pero no postular); RN-08 (auditoría de accesos) |

---

### 1. BREVE DESCRIPCIÓN
Permite a un usuario registrado autenticarse en el sistema mediante email y
contraseña, obteniendo un Token JWT con los claims de su rol para acceder a las
funcionalidades correspondientes.

### 2. PRECONDICIONES
1. El usuario debe poseer una cuenta registrada en el sistema (CU-06).
2. La cuenta no debe estar dada de baja ni con baja definitiva (soft).
3. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `POST /api/auth/login` con un JSON que contiene el email (o usuario) y la contraseña.
2. La **Capa de Presentación** (`AuthController`) valida que el JSON sea estructuralmente correcto y que las credenciales no estén vacías.
3. La **Capa de Negocio** (`AuthService`) verifica las credenciales contra el hash almacenado (**RN-17**), consulta el estado de la cuenta y obtiene el rol del usuario.
4. El Sistema genera el Token JWT con los claims del rol y su fecha de expiración.
5. El Sistema registra el acceso en auditoría (**RN-08**).
6. El Sistema devuelve un código **200 OK** con el token, el rol y la expiración.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Credenciales vacías (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el email o la contraseña llegan vacíos.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Usuario inexistente o credenciales incorrectas (HTTP 401 Unauthorized):**
  1. Si en el Paso 3 no existe un usuario con ese email o la contraseña no coincide con el hash almacenado (**RN-17**).
  2. La **Capa de Negocio** lanza `InvalidCredentialsException` sin revelar cuál campo falló.
  3. El Sistema devuelve un código **401 Unauthorized**. Fin del caso de uso.

* **3b. Cuenta dada de baja (HTTP 401 Unauthorized):**
  1. Si en el Paso 3 la cuenta se encuentra dada de baja o con baja definitiva (soft).
  2. La **Capa de Negocio** rechaza la autenticación (`AccountDeactivatedException`).
  3. El Sistema devuelve un código **401 Unauthorized**. Fin del caso de uso.

* **3c. Cuenta suspendida por blacklist (HTTP 200 OK - acceso restringido):**
  1. Si en el Paso 3 la cuenta está suspendida por una sanción vigente (**RN-01**).
  2. El Sistema **permite** el ingreso (decisión de diseño: el usuario debe ver su estado), emite el token y lo informa.
  3. El Sistema devuelve un código **200 OK**; el usuario podrá consultar pero **no postular** (RN-01). El caso de uso continúa.

* **4a. Error en la emisión del token (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la generación del JWT falla.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El inicio de sesión aplica por igual a los cuatro roles; el rol obtenido determina las rutas y permisos del token.
2. El token expira según la política configurada (JWT); una vez expirado el usuario debe reautenticarse.

### 6. POSTCONDICIONES
1. Se establece una sesión autenticada con Token JWT y claims de rol, habilitando las funcionalidades correspondientes.
2. El evento de acceso queda registrado en auditoría (**RN-08**).
3. Una cuenta suspendida ingresa con restricción para postular (impacto en permisos — **RN-01**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Autenticación exitosa con token JWT y rol. |
| `400` | Bad Request | Fallo en la validación de esquema o credenciales vacías. |
| `401` | Unauthorized | Credenciales incorrectas, usuario inexistente o cuenta dada de baja. |
| `500` | Internal Server Error | Error técnico no controlado en la emisión del token. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON y credenciales no vacías.
- **Verificación (Negocio, → 401):** existencia y credenciales contra el hash (**RN-17**, `InvalidCredentialsException`); cuenta dada de baja (`AccountDeactivatedException`). La cuenta suspendida ingresa con restricción (**RN-01**, decisión de diseño). Auditoría (**RN-08**) emitida en la Capa de Negocio.

### Matriz de trazabilidad CU-07 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `AuthService_IniciarSesion_ConCredencialesValidas_RetornaToken` | `POST /api/auth/login_ConCredencialesValidas_Returns200` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/auth/login_ConJsonInvalido_Returns400` |
| 2a. Credenciales vacías | `400 Bad Request` | — (se detecta vía `[Required]` en Presentación) | `POST /api/auth/login_ConCredencialesVacias_Returns400` |
| 3a. Credenciales incorrectas | `401 Unauthorized` | `AuthService_ConCredencialesIncorrectas_LanzaInvalidCredentialsException` | `POST /api/auth/login_ConCredencialesIncorrectas_Returns401` |
| 3b. Cuenta dada de baja | `401 Unauthorized` | `AuthService_ConCuentaDeBaja_LanzaAccountDeactivatedException` | `POST /api/auth/login_ConCuentaDeBaja_Returns401` |
| 3c. Cuenta suspendida (blacklist) | `200 OK` (acceso restringido) | `AuthService_ConCuentaSuspendida_PermiteIngresoRestringido` | `POST /api/auth/login_ConCuentaSuspendida_Returns200` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).