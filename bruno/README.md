# Peticiones de prueba Bruno

Organización por dominio (una carpeta por bloque funcional, con los casos de uso numerados adentro):

- `0 - Setup/` — reinicio del entorno demo (deja la base como recién sembrada). **Debe correr siempre primero**; es lo que permite repetir la colección completa sin borrar el `.db`.
- `1 - Autenticacion/` — login, registro y sesión (CU-00/06/07) + login de veterinario y caso negativo de validación.
- `2 - Usuarios y Blacklist/` — gestión de usuarios y sanciones (CU-01/08).
- `3 - Mascotas y Clinica/` — mascotas, historial inicial y eventos médicos (CU-03/09/10).
- `4 - Proceso de Adopcion/` — solicitud, evaluación, entrevista, seguimiento, incidente, finalización, cancelación y abandono (CU-02+05/04/11/12/18/15/13/14).
- `5 - Administracion y Reportes/` — reporte CSV y auditoría (CU-16/17).

## Puesta en marcha

1. Levantar la API en modo demo: `dotnet run --project API --launch-profile Demo` (escucha en `http://localhost:5104`).
   - **Desde Visual Studio, usá `Ctrl+F5` (Iniciar sin depuración).** La API lanza `ValidationException`/`ConflictException` **a propósito** para responder **400/409** (p. ej. `06b`). Con el depurador adjunto (F5) VS **se detiene en esas excepciones** y **congela la API**: las peticiones de Bruno quedan sin respuesta y parecen "errores".
   - Si querés depurar igual, desactivá la parada en esas excepciones: *Depurar → Ventanas → Configuración de excepciones* (`Ctrl+Alt+E`) → desmarcá `Common Language Runtime Exceptions` (o sólo `PortalAdopcion.Shared.Exceptions.*`).
2. Abrir esta carpeta como colección en Bruno y seleccionar el entorno **Local**.
3. Ejecutar en orden. La propia colección captura los IDs y los tokens en **variables de ejecución** (runtime), así que `environments/Local.bru` no se modifica entre corridas:

   | Petición | Variable que captura |
   | --- | --- |
   | `00 Reiniciar entorno demo` | *(reinicia la base: no captura variables)* |
   | `00 Login` | `token`, `usuarioId` |
   | `00b Login veterinario` | `vetToken` |
   | `08 Usuarios` | `voluntarioId`, `adoptanteId`, `adoptanteSancionId` |
   | `09 Mascotas` | `mascotaId` |
   | `01 Blacklist` | `sancionId` |
   | `02 Solicitud y 05 Proceso` | `solicitudId` |
   | `02b Solicitudes lista` | `solicitudPendienteId`, `solicitudPruebaId` |

4. Ya no hace falta copiar el token a mano: el script `script:post-response` de `00 Login` guarda `{{token}}` automáticamente para el resto de la corrida.
5. El archivo `environments/Local.bru` sólo contiene `baseUrl`: los IDs y tokens se guardan con `bru.setVar` (runtime) y **no se persisten**, así el repo no se ensucia al ejecutar ni quedan tokens viejos guardados.

## Orden recomendado (ejecución completa)

```
0 - Setup:                    00 (reiniciar entorno demo)
1 - Autenticacion:            00 → 00b → 06 → 06b → 07
2 - Usuarios y Blacklist:     08 → 01
3 - Mascotas y Clinica:       09 → 03 → 10
4 - Proceso de Adopcion:      02 → 02b → 04 → 11 → 12 → 18 → 15 → 13 → 14
5 - Administracion:           16 → 17
```

## Repetibilidad

La colección se puede correr **completa, todas las veces que quieras, sin borrar la base de datos**.
El primer request (`0 - Setup / 00 Reiniciar entorno demo`) llama a `POST /api/demo/reiniciar`, que borra
todos los datos y vuelve a sembrar los ejemplos (mismos usuarios, mascotas y procesos que una base recién creada).
Así cada corrida arranca desde el mismo estado y **la corrida N responde igual que la primera**.

- El endpoint es **solo para `Development` + `Demo:Seed=true` + desde `localhost`**; en cualquier otro caso responde **404**.
- Se invalida el caché del catálogo de mascotas, así los IDs recién sembrados siempre están frescos.
- Nota: como reinicia los datos demo, no lo ejecutes si cargaste datos a mano que quieras conservar (para una demo real, sacá ese request del orden o usá un `.db` aparte).

## Notas de estado

- `12 Seguimiento` admite un único registro por día y proceso (índice único `SolicitudId + Fecha`); si se repite el mismo día, la API responde 409.
- `04 Evaluar` con `Aprobada` exige que la solicitud tenga al menos 15 días desde `FechaSolicitud`: por eso usa el proceso Pendiente sembrado por la demo.
- `18 Incidente` sólo aplica a procesos `EnPeriodoDePrueba`, por eso se ejecuta **antes** de `15 Finalizar`.
- `15 Finalizar` exige >= 15 días de período de prueba e historial clínico; usa el proceso `EnPeriodoDePrueba` sembrado.
- `06 Registro` genera **email y DNI únicos** en cada ejecución (vía `script:pre-request`), así que siempre responde **201**.
- Gracias a `0 - Setup / 00 Reiniciar entorno demo`, la colección es **totalmente repetible**: la segunda corrida (y las siguientes) vuelven a dar `22/22` peticiones y `52/52` tests sin borrar la base.
- `06b Registro inválido` envía `"dni": "12AB34"` y `"telefono": "abc123"` y debe responder **400** (validación de solo dígitos).
- `14 Control abandono demo` es exclusivo de `Development` y normalmente procesa 0 procesos.
- Roles: salud (03/10) con **Veterinario**; blacklist/reportes/auditoría/usuarios (01/08/14/16/17) con **Administrador**; el resto con Administrador o Voluntario asignado.

La colección ofrece ejemplos; Scalar (`/scalar/v1`) permite probar los métodos CRUD restantes. Consultar los flujos alternativos en CASOS DE USO y las pruebas automatizadas.
