# Grupo N° 4 — Portal de Adopción de Mascotas (API REST)

**Integrantes:** Catalina Giordano, Francisco Ledesma

> API RESTful en **.NET 10** con arquitectura **N-Tier** (Controller → Service → Repository → DbContext)
> para la gestión de adopciones de mascotas de una ONG.

---

## 1. Sistema

El portal permite a una ONG administrar su catálogo de **mascotas** disponibles, registrar
**adoptantes** y gestionar **solicitudes de adopción** respetando reglas de negocio que impiden
reservas duplicadas o adopciones sobre animales no disponibles.

| Entidad | Descripción |
|---|---|
| **Mascota** | Animal en adopción (`Disponible` / `Reservada` / `Adoptada`) |
| **Adoptante** | Persona registrada que desea adoptar (email y DNI únicos) |
| **SolicitudDeAdopcion** | Vínculo mascota ↔ adoptante (`Pendiente` / `Aprobada` / `Rechazada` / `Cancelada`) |

## 2. Arquitectura N-Tier

```
PortalAdopcion.slnx
├─ API/                        Capa de presentación
│  ├─ Controllers/             MascotasController, AdoptantesController, SolicitudesController
│  └─ Program.cs               DI (scoped), SQLite, enums como string en JSON, Scalar
├─ BusinessLogic/              Capa de negocio
│  ├─ Abstractions/            I*Service
│  └─ Services/                MascotaService, AdoptanteService, SolicitudService
├─ DataAccess/                 Capa de datos (única que toca EF Core)
│  ├─ Entities/                Mascota, Adoptante, SolicitudDeAdopcion
│  ├─ Repositories/            3 repos + interfaces (AsNoTracking, Guid en CreateAsync)
│  ├─ PortalAdopcionDbContext  DeleteBehavior.Restrict, enums como string, índices únicos
│  └─ DbInitializer.cs         Migrate() + seed de prueba al arrancar
├─ Migrations/                 InitialCreate (compilada dentro de DataAccess)
├─ Shared/                     DTOs (<Entidad>Create|Update|Response) y excepciones tipadas
├─ Tests/
│  ├─ BusinessLogic.Tests/     37 tests unitarios (xUnit + Moq, sin BD)
│  └─ API.Integration.Tests/   36 tests de integración (WebApplicationFactory + SQLite in-memory)
└─ bruno/                      Colección con 3 requests (flujo principal)
```

**Flujo de datos:** `Controller → Service → Repository → DbContext`

**DTOs y mapeos:** las entidades de base de datos nunca se exponen. Cada service mapea a
`<Entidad>ResponseDTO` con métodos privados `MapToResponseDTO` (sin AutoMapper).

## 3. Reglas de negocio

| Regla | Descripción |
|---|---|
| **RN-01** | Una mascota admite una sola solicitud activa (`Pendiente`); al crearla pasa a `Reservada`. |
| **RN-02** | Un adoptante no puede tener más de una solicitud activa. |
| **RN-03** | No se crean solicitudes sobre mascotas `Reservada`/`Adoptada`. |
| **RN-04** | Rechazo/cancelación exigen `motivo`. |

**Transiciones:** `Aprobada` → mascota `Adoptada` · `Rechazada`/`Cancelada` → mascota `Disponible`.
**Integridad:** no se eliminan mascotas/adoptantes con solicitudes asociadas (409).

## 4. Ejecución

```bash
dotnet run --project API          # aplica migración + seed y levanta en http://localhost:5104
dotnet test PortalAdopcion.slnx   # 73 tests (unitarios + integración)
dotnet build PortalAdopcion.slnx  # compilar
```

- Documentación interactiva (Swagger/OpenAPI): **http://localhost:5104/scalar/v1**
- La base SQLite local (`API/portaladopcion.db`) se crea automáticamente al primer arranque.

## 5. Catálogo de endpoints

### Mascotas — `MASCOTA`

| Método | Ruta | Descripción | OK | Errores |
|---|---|---|---|---|
| GET | `/api/mascotas` | Listar (filtro `?estado=Disponible`) | 200 | — |
| GET | `/api/mascotas/{id}` | Obtener por id | 200 | 404 |
| POST | `/api/mascotas` | Crear | 201 | 400 |
| PUT | `/api/mascotas/{id}` | Actualizar | 200 | 400 / 404 |
| DELETE | `/api/mascotas/{id}` | Eliminar | 204 | 404 / 409 |

```http
POST /api/mascotas
{
  "nombre": "Rex",
  "especie": "Perro",
  "raza": "Doberman",
  "edad": 3,
  "sexo": "Macho"
}
# 201 Created
{
  "id": "11111111-1111-1111-1111-111111111111",
  "nombre": "Rex",
  "especie": "Perro",
  "raza": "Doberman",
  "edad": 3,
  "sexo": "Macho",
  "estado": "Disponible",
  "fechaRegistro": "2026-09-17T..."
}
```

### Adoptantes

| Método | Ruta | Descripción | OK | Errores |
|---|---|---|---|---|
| GET | `/api/adoptantes` | Listar | 200 | — |
| GET | `/api/adoptantes/{id}` | Obtener por id | 200 | 404 |
| POST | `/api/adoptantes` | Crear | 201 | 400 / 409 |
| PUT | `/api/adoptantes/{id}` | Actualizar | 200 | 400 / 404 / 409 |
| DELETE | `/api/adoptantes/{id}` | Eliminar | 204 | 404 / 409 |

```http
POST /api/adoptantes
{
  "nombreCompleto": "Maria Lopez",
  "email": "maria.lopez@mail.com",
  "dni": "40111222",
  "telefono": "1122334455",
  "direccion": "Calle 1 234"
}
```

### Solicitudes de adopción

| Método | Ruta | Descripción | OK | Errores |
|---|---|---|---|---|
| GET | `/api/solicitudes` | Listar | 200 | — |
| GET | `/api/solicitudes/{id}` | Obtener por id | 200 | 404 |
| POST | `/api/solicitudes` | Crear (RN-01/02/03) | 201 | 400 / 404 / 409 |
| PATCH | `/api/solicitudes/{id}/estado` | Aprobar / Rechazar | 200 | 400 / 404 / 409 |
| DELETE | `/api/solicitudes/{id}?motivo=...` | Cancelar | 204 | 400 / 404 |

```http
POST /api/solicitudes
{
  "mascotaId": "11111111-1111-1111-1111-111111111111",
  "adoptanteId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
}
# 201 Created — la mascota pasa a "Reservada"
{
  "id": "efd89481-...",
  "mascotaId": "11111111-...",
  "adoptanteId": "aaaaaaaa-...",
  "fechaSolicitud": "2026-09-17T...",
  "estado": "Pendiente",
  "motivo": null
}

PATCH /api/solicitudes/{id}/estado        # Aprobar → mascota "Adoptada"
{ "estado": "Aprobada" }

PATCH /api/solicitudes/{id}/estado        # Rechazar (RN-04: motivo obligatorio)
{ "estado": "Rechazada", "motivo": "No cumple requisitos" }
```

**Formato de error:** todos los errores devuelven `{ "message": "..." }`
(404 `NotFoundException` · 400 `ValidationException` · 409 `ConflictException`).

## 6. Datos de prueba (seed)

| Mascota | Id |
|---|---|
| Tom (Gato, 2 años) | `11111111-1111-1111-1111-111111111111` |
| Luna (Perro, 1 año) | `22222222-2222-2222-2222-222222222222` |

| Adoptante | Id |
|---|---|
| Ana Garcia (`ana.garcia@mail.com`) | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` |
| Juan Perez (`juan.perez@mail.com`) | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` |

---

## 7. Probando con Bruno

La carpeta `bruno/` contiene una colección con **3 requests** que cubren el flujo principal:
`Mascotas_Listar` (GET `/api/mascotas`), `Adoptantes_Crear` (POST `/api/adoptantes`) y
`Solicitudes_Crear` (POST `/api/solicitudes`). El resto de endpoints se prueban desde
**/scalar/v1**. Abrila desde **Bruno** (`File > Open Collection` → `bruno/`) con la API levantada.