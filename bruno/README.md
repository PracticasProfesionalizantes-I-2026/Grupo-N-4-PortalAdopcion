# Coleccion Bruno - Portal de Adopcion

Coleccion completa de requests para el **Portal de Adopcion** (.NET 10, N-Tier:
Controller → Service → Repository → DbContext). Entidades: **Mascota**, **Adoptante** y
**SolicitudDeAdopcion**. Cubre el 100% de los endpoints, los flujos end-to-end de negocio
y los casos de error tipados (400 / 404 / 409).

## 1. Requisitos

- API levantada en `http://localhost:5104` (`dotnet run --project API`).
  Al arrancar, `DbInitializer` aplica migraciones y siembra datos seed: Tom, Luna, Ana y Juan.
- App de Bruno (https://www.usebruno.com) **o** [bruno-cli](https://docs.usebruno.com/bruno-cli/overview)
  para ejecución headless / CI.

## 2. Importar la coleccion

En la app de Bruno: **Open Collection → Select Folder** → elegir la carpeta `bruno/` de este repo.

Una vez importada, activar el environment **Local** en el selector de environments (arriba a la derecha).

## 3. Variables del environment `Local.bru`

| Variable | Valor inicial | Fuente |
|---|---|---|
| `baseUrl` | `http://localhost:5104/api` | Editar si usas puerto/profile distinto (ej: `https://localhost:7289/api`) |
| `mascotaId` | seed Tom || Sobrescrita por `Mascotas/Create` y pasos E2E 01/08 |
| `adoptanteId` | seed Ana || Sobrescrita por `Adoptantes/Create` y pasos E2E 02/09 |
| `solicitudId` | `00000000-...` (placeholder) | Generada por `Solicitudes/Create` y por `Flujos/.../01` y 03/10 |
| `guidInexistente` | `99999999-...` | Guid fijo inexistente para casos 404 |
| `mascotaTomId`, `mascotaLunaId` | ids seed | Entidades sembradas por `DbInitializer` |
| `adoptanteAnaId`, `adoptanteJuanId` | ids seed | Entidades sembradas por `DbInitializer` |

Los GET By Id / Update / Delete usan `{{mascotaId}}`, `{{adoptanteId}}`, `{{solicitudId}}`
(no ids hardcodeados).

## 4. Como ejecutarla

### En la app

- **Request individual:** abrir y hacer clic en "Send". Los requests que guardan id en el
  environment lo persisten automaticamente.
- **Carpeta (secuencial):** clic derecho sobre una carpeta → **Run Folder** (los requests se
  ejecutan por `seq`, encadenando variables).

### Con bruno-cli

```bash
# Toda la coleccion (recursivo), environment Local
bru run bruno/ -r --env Local

# Solo los flujos (E2E + Errores)
bru run bruno/Flujos -r --env Local

# Solo el flujo E2E o el flujo de errores
bru run bruno/Flujos --env Local --folder "E2E Adopcion"
bru run bruno/Flujos --env Local --folder "Casos de Error (400,404,409)"
```

> En CI/headless los tests se reportan como failures si una assertion no se cumple. Para
> generacion de reportes: `bru run bruno/ -r --env Local --output reports/bruno.jsonl --format json`.

## 5. Cobertura de endpoints

| Endpoint | Metodo | Variable generada | Caso feliz | Errores cubiertos |
|---|---|---|---|---|
| `/api/mascotas` | GET | — | **200** lista `MascotaResponseDTO[]` | — |
| `/api/mascotas?estado=` | GET | — | **200** filtro por estado | — |
| `/api/mascotas/{id}` | GET | — | **200** | **404** id inexistente |
| `/api/mascotas` | POST | `mascotaId` | **201** estado `Disponible` | **400** edad fuera de rango, nombre/raza invalidos |
| `/api/mascotas/{id}` | PUT | — | **200** | **404** id inexistente |
| `/api/mascotas/{id}` | DELETE | — | **204** | **404** id inexistente; **409** con solicitudes asociadas |
| `/api/adoptantes` | GET | — | **200** lista | — |
| `/api/adoptantes/{id}` | GET | — | **200** | **404** id inexistente |
| `/api/adoptantes` | POST | `adoptanteId` | **201** | **400** email/telefono invalidos; **409** email o DNI duplicado |
| `/api/adoptantes/{id}` | PUT | — | **200** | **404** id inexistente; **409** duplicados |
| `/api/adoptantes/{id}` | DELETE | — | **204** | **404** id inexistente; **409** con solicitudes asociadas |
| `/api/solicitudes` | GET | — | **200** lista | — |
| `/api/solicitudes/{id}` | GET | — | **200** | **404** id inexistente |
| `/api/solicitudes` | POST | `solicitudId` | **201** estado `Pendiente`; mascota → `Reservada` | **400** ids vacios; **404** mascota/adoptante inexistentes; **409** mascota no disponible, o mascota/adoptante con solicitud activa |
| `/api/solicitudes/{id}/estado` | PATCH | — | **200** transicion de estado | **400** solo `Pendiente` cambia de estado, o rechazo/cancelacion sin motivo |
| `/api/solicitudes/{id}?motivo=` | DELETE | — | **204** cancelacion (mascota → `Disponible`) | **400** sin motivo; **404** id inexistente |

## 6. Flujos disponibles

- **Flujos/E2E Adopcion** — ciclo completo: seed inicial (Tom+Ana) → crear mascota/adoptante →
  crear solicitud (mascota `Reservada`) → aprobar (mascota `Adoptada`) → segundo par →
  cancelar (mascota `Disponible`). 14 pasos encadenados por `seq`.
- **Flujos/Casos de Error (400,404,409)** — 19 requests que ejercitan las Exceptions tipadas
  mapeadas a HTTP (404 `NotFoundException`, 400 `ValidationException`, 409 `ConflictException`).

## 7. Consideraciones importantes

- **Solicitudes:** la solicitud es una maquina de estados de una via
  (`Pendiente → Aprobada | Rechazada | Cancelada`). En la carpeta `3 - Solicitudes` la
  secuencia por `seq` es Create → Get All → Get By Id → Delete (Cancelar) → Update Estado:
  `Update Estado` responde **200** solo si se ejecuta sobre una solicitud recien creada
  (por eso el happy-path de aprobacion vive en `Flujos/E2E Adopcion`).
- **Re-ejecucion de flujos sobre la misma BD:** el E2E genera entidades nuevas en cada corrida,
  pero el paso `00 - Seed` (Tom+Ana) y el setup del flujo de errores devuelven **409** en una
  segunda corrida (Tom ya esta `Reservada`). Para repetir de raiz: borrar `API/portaladopcion.db`
  y reiniciar la API (se regenera y resiembra), y en Bruno editar el environment Local
  restaurando `mascotaId`/`adoptanteId`/`solicitudId` a sus valores iniciales.
- **Respuestas de error:** el API responde siempre `{ "message": "<motivo>" }` y las
  assertions de los casos de error validan status + existencia del mensaje.
- **Enums:** el API serializa enums como strings (`Perro`, `Gato`, `Disponible`, `Pendiente`, ...).