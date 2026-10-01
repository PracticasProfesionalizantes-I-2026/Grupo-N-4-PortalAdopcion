# AGENTS.md — Contexto operativo del Portal de Adopción (.NET 10)

## Propósito

API RESTful en **.NET 10** con arquitectura **N-Tier** para un **portal de adopción de mascotas**
para ONGs (Grupo N° 4 — PortalAdopcion). Tres entidades de dominio: `Mascota`, `Adoptante`,
`SolicitudDeAdopcion`.

## Comandos CLI

| Acción | Comando |
|---|---|
| Compilar | `dotnet build PortalAdopcion.slnx` |
| Ejecutar | `dotnet run --project API` (aplica migraciones + seed vía `DbInitializer`) |
| Probar | `dotnet test PortalAdopcion.slnx` |
| Nueva migración | `dotnet ef migrations add <Nombre> --project DataAccess --output-dir ..\Migrations` |
| Scalar (docs) | `http://localhost:5104/scalar/v1` |

## Convención de capas (N-Tier)

Flujo obligatorio: **Controller → Service → Repository → DbContext**. Nunca se salta una capa.

- **API/Controllers**: reciben HTTP, inyectan `*Service` por constructor y mapean excepciones
  tipadas con `try/catch` explícito por tipo. Sin lógica de negocio.
  `NotFoundException`→404, `ValidationException`→400, `ConflictException`→409.
- **BusinessLogic/Services**: toda la **validación y reglas de negocio**. Usan interfaces de
  repositorio; **no usan LINQ-to-entities**. Mapean entidades→DTO con métodos privados
  `MapToResponseDTO` (sin AutoMapper). Nombres: `<Entidad>Service` + interface `I<Entidad>Service`.
- **DataAccess/Repositories**: única capa que toca EF Core. `AsNoTracking()` en lecturas,
  `Guid` asignado en `CreateAsync`, transiciones de estado atómicas dentro de un `SaveChanges`.
- **DataAccess/PortalAdopcionDbContext**: `DeleteBehavior.Restrict` en las relaciones hijas
  (`Solicitud → Mascota` y `Solicitud → Adoptante`); enums almacenados como string; email/DNI únicos.
- **Shared**: DTOs (`<Entidad>CreateDTO`, `<Entidad>UpdateDTO`, `<Entidad>ResponseDTO`) y
  excepciones tipadas en archivos propios. Sin dependencias de EF.

## Reglas de negocio implementadas (en `SolicitudService`, `MascotaService`, `AdoptanteService`)

- **RN-01** — Una mascota admite una sola solicitud activa (`Pendiente`); al crearla pasa a `Reservada`.
- **RN-02** — Un adoptante no puede tener más de una solicitud activa.
- **RN-03** — No se crean solicitudes sobre mascotas `Reservada/Adoptada`.
- **RN-04** — Rechazo/cancelación exigen `motivo` (ValidationException → 400).
- Transiciones: `Aprobada` → mascota `Adoptada`; `Rechazada`/`Cancelada` → mascota vuelve a `Disponible`.
- No se eliminan mascotas/adoptantes con solicitudes asociadas (integridad `Restrict` + verificación previa).

## Tests

- `Tests/BusinessLogic.Tests` — xUnit + **Moq**, servicios con repos de mocks (sin BD).
  Nombres de convención: `Metodo_Escenario_Resultado` (Ej.: `Create_EmailDuplicado_LanzaConflictException`).
- `Tests/API.Integration.Tests` — `WebApplicationFactory<Program>` + **SQLite in-memory**
  (`PortalAdopcionApiFactory`), HTTP real por endpoint con datos sembrados por `DbInitializer`.

## Notas

- Al agregar una entidad nueva: DTOs en `Shared`, entidad+repo+interfaz en `DataAccess`, service
  en `BusinessLogic`, controller en `API`, y registrarla en DI en `API/Program.cs`.
- Registrar siempre los nuevos repositorios/servicios en `Program.cs` (scoped).
- Las migraciones se generan en la **raíz** `Migrations/` (`--output-dir ..\Migrations`) y se
  compilan dentro de `DataAccess` vía `<Compile Include="..\Migrations\**\*.cs" />` en su `.csproj`.
- La BD local `API/portaladopcion.db` se crea al arrancar; está excluida del repo (`.gitignore`).