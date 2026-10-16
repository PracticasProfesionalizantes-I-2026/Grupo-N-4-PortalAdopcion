-- =====================================================================
--  Portal de Adopción · Script de rendimiento y validaciones (SQLite)
-- =====================================================================
--  Motor: SQLite (Microsoft.Data.Sqlite / EF Core 10).
--  Estos objetos ya se aplican automáticamente mediante las migraciones:
--    * 20261016135209_ValidacionesIdentidad  -> CHECK de DNI / Teléfono / Estado civil
--    * 20261016151406_IndicesRendimiento     -> índices de filtros y ordenamientos
--  Este archivo sirve como referencia ejecutable / auditoría y para entornos
--  donde se administre la base por fuera de EF Core.
--
--  Uso:  sqlite3 portaladopcion-prueba.db < indices_y_validaciones.sql
-- =====================================================================

PRAGMA journal_mode = WAL;        -- lecturas concurrentes sin bloquear escrituras
PRAGMA synchronous  = NORMAL;     -- durabilidad suficiente para un portal de demo
PRAGMA busy_timeout = 5000;       -- 5 s de espera ante contención de escritura

-- ---------------------------------------------------------------------
-- 1. Índices de rendimiento
-- ---------------------------------------------------------------------
-- Catálogo de mascotas: filtro por estado + orden por nombre.
CREATE INDEX IF NOT EXISTS "IX_Mascotas_Estado" ON "Mascotas" ("Estado");
CREATE INDEX IF NOT EXISTS "IX_Mascotas_Nombre" ON "Mascotas" ("Nombre");

-- Procesos de adopción: filtro por estado activo, por voluntario y caducidad.
CREATE INDEX IF NOT EXISTS "IX_Solicitudes_Estado"                  ON "Solicitudes" ("Estado");
CREATE INDEX IF NOT EXISTS "IX_Solicitudes_VoluntarioId"            ON "Solicitudes" ("VoluntarioId");
CREATE INDEX IF NOT EXISTS "IX_Solicitudes_Estado_UltimaActividad"  ON "Solicitudes" ("Estado", "UltimaActividad");

-- Sanción vigente por adoptante (reemplaza al índice simple anterior).
DROP INDEX IF EXISTS "IX_SancionBlacklist_AdoptanteId";
CREATE INDEX IF NOT EXISTS "IX_SancionBlacklist_AdoptanteId_Revocada_Desde_Hasta"
    ON "SancionBlacklist" ("AdoptanteId", "Revocada", "Desde", "Hasta");

-- Auditoría: rango de fechas y filtro por usuario ordenado por fecha.
CREATE INDEX IF NOT EXISTS "IX_EventoAuditoria_Fecha"            ON "EventoAuditoria" ("Fecha");
CREATE INDEX IF NOT EXISTS "IX_EventoAuditoria_UsuarioId_Fecha"  ON "EventoAuditoria" ("UsuarioId", "Fecha");

-- Notificaciones: bandeja interna por usuario o por rol.
CREATE INDEX IF NOT EXISTS "IX_Notificacion_Canal_UsuarioId" ON "Notificacion" ("Canal", "UsuarioId");
CREATE INDEX IF NOT EXISTS "IX_Notificacion_Canal_Rol"       ON "Notificacion" ("Canal", "Rol");

-- Incidentes: filtro por voluntario ordenado por fecha.
CREATE INDEX IF NOT EXISTS "IX_Incidente_VoluntarioId_Fecha" ON "Incidente" ("VoluntarioId", "Fecha");

-- ---------------------------------------------------------------------
-- 2. Restricciones de validación (identidad)
-- ---------------------------------------------------------------------
-- SQLite no permite ALTER TABLE ... ADD CONSTRAINT, por lo que estas
-- restricciones se incorporan al crear/actualizar la tabla. Se listan
-- aquí como contrato verificable; la migración 20261016135209 las aplica.
--
--   Adoptantes:
--     CHECK ("Dni"          cumple solo dígitos 0-9)
--     CHECK ("Telefono"     cumple solo dígitos 0-9)
--     CHECK ("EstadoCivil"  IN ('Soltero/a','Casado/a','Divorciado/a','Viudo/a',
--                               'Conviviente/Unión de hecho','Separado/a'))
--   Usuario:
--     CHECK ("Dni"          cumple solo dígitos 0-9)
--
-- DDL equivalente (referencia):
--
--   CREATE TABLE "Adoptantes" (
--       ...,
--       CONSTRAINT "CK_Adoptantes_Dni_Numerico"
--           CHECK (trim("Dni") <> '' AND "Dni" NOT GLOB '*[^0-9]*'),
--       CONSTRAINT "CK_Adoptantes_Telefono_Numerico"
--           CHECK (trim("Telefono") <> '' AND "Telefono" NOT GLOB '*[^0-9]*'),
--       CONSTRAINT "CK_Adoptantes_EstadoCivil_Valido"
--           CHECK ("EstadoCivil" IN ('Soltero/a','Casado/a','Divorciado/a','Viudo/a',
--                                    'Conviviente/Unión de hecho','Separado/a'))
--   );
--
--   CREATE TABLE "Usuario" (
--       ...,
--       CONSTRAINT "CK_Usuario_Dni_Numerico"
--           CHECK (trim("Dni") <> '' AND "Dni" NOT GLOB '*[^0-9]*')
--   );

-- ---------------------------------------------------------------------
-- 3. Verificación
-- ---------------------------------------------------------------------
-- Lista los índices creados para comprobar su existencia:
--   SELECT name, tbl_name FROM sqlite_master
--   WHERE type = 'index' AND name LIKE 'IX_%' ORDER BY tbl_name, name;
--
-- Comprueba que la validación rechaza valores no numéricos (debe fallar):
--   INSERT INTO "Adoptantes" ("Id","Version","NombreCompleto","Email","Dni","Telefono",
--                             "Direccion","Edad","EstadoCivil","DatosLaborales","FechaRegistro","Baja")
--   VALUES ('11111111-1111-1111-1111-111111111111',0,'Prueba','x@x.com','12AB34','abc123',
--           'Calle',25,'Soltero/a','Estudiante',datetime('now'),0);
--   -- >> Error: CHECK constraint failed
