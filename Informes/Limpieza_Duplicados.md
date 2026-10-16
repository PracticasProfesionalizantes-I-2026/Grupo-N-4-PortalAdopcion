# Inventario de duplicados y limpieza propuesta

Fotografía tomada el 15 de octubre de 2026 sobre la carpeta completa `COPIA PORTALADOPCION`.

**Estado: ejecutado el 15 de octubre de 2026 tras aprobación explícita.** Se creó un respaldo único con todo lo redundante antes de borrar:

- Respaldo: `C:\Users\catag\AppData\Local\Temp\opencode\backup_redundantes_20261015.zip` (501,7 MB; 8.644 entradas; integridad verificada con `tar -tf`, exit 0).

Luego se eliminaron los seis bloques listados abajo. Espacio liberado: **~734 MB**. La carpeta raíz pasó de ~905 MB a 171,4 MB.

## 1. Árbol vigente (el que se conserva)

- Proyecto: `COPIAPORTALENVISUAL/PortalAdopcion_Prueba_Completa/PortalAdopcion_Prueba`.
- Repositorio Git: la propia carpeta `COPIAPORTALENVISUAL` (origin `catagiordano/COPIAPORTALADOPCION.git`), rama `main`, HEAD `7e7083c`, 2 commits detrás de `origin/main`.

## 2. Estado del repositorio antes de limpiar (importante)

El árbol de trabajo quedó **a mitad de una reorganización sin confirmar**. No forma parte de ningún commit todavía:

- 207 archivos versionados ya no están en su ruta original: se movieron a `REDUNDANCIAS/carpetas_solapadas/` y `REDUNDANCIAS/versiones_antiguas/referencia/` (`Organizacion_Por_Caso_de_Uso`, `CUs_POR_RAMA`, `Documentos/Tp-entrega`, `Documentos/Ejemplo(Profes)`, `propuestas-pr`, `referencia/*`, tests viejos `API.Integration.Tests` y `BusinessLogic.Tests`, `PortalAdopcion_Prueba_Completa.zip`).
- Carpetas nuevas sin versionar: `REDUNDANCIAS/`, `Grupo-N-4-PortalAdopcion/`, `TEORIA PORTAL ADOPCION/`.
- Cambios propios de esta revisión (Lotes 1-3): 8 archivos modificados y 3 nuevos (capa de DTOs y tests de reglas de negocio con Moq).

Esta situación debe resolverse con un commit explícito cuando el grupo lo decida; no se toca desde aquí.

## 3. Duplicados fuera del repositorio

| Ruta | Tamaño | Qué es | Verificación | Riesgo de borrar |
|---|---|---|---|---|
| `Portal_Adopcion_/` | 107,6 MB | Copia completa del repo `COPIAPORTALENVISUAL` (incluye su propio `.git` de 36,2 MB y `AUDITORIA/`) | Mismo HEAD `7e7083c` e idéntico historial que el árbol vigente; `DataAccess/Entities/Mascota.cs` con el mismo hash SHA-256 | Bajo: es el mismo repo; solo repite trabajo sin commitear |
| `ELEMENTOS REPETIDOS (NO VER)/Grupo-N-4-PortalAdopcion/` | 183,2 MB | Snapshot antiguo del proyecto original (incluye `Tests/` de 104 MB y `bin/`) | `Mascota.cs` con hash distinto (versión previa) | Medio: es la copia más pesada; conviene respaldar antes |
| `ELEMENTOS REPETIDOS (NO VER)/COPIAPORTALADOPCION/` | 132,1 MB | Otra copia antigua + `INFORME_ESTADO.md`, `INFORME_COMPARACION_COPIAS.md` | `Mascota.cs` con hash distinto (versión previa) | Medio: contiene informes que podrían no estar en otro lado |
| `ELEMENTOS REPETIDOS (NO VER)/Grupo-N-4-PortalAdopcion-completado/` | 2,2 MB | Snapshot de la rama `feature/base-api` | `Mascota.cs` con hash distinto | Bajo |
| `ELEMENTOS REPETIDOS (NO VER)/documentacion portaladopcionpractica/` | 1,6 MB | Presentación, guías y planes (PPTX/PDF) | — | Bajo (documentación suelta) |
| `COPIA PORTALADOPCION.zip` (raíz) | 193,2 MB | Empaquetado de todo el conjunto | — | Bajo: es respaldo, se puede regenerar |

## 4. Duplicados dentro del repositorio

| Ruta | Tamaño | Qué es | Verificación | Riesgo de borrar |
|---|---|---|---|---|
| `REDUNDANCIAS/` | 112,4 MB | Cajón de la limpieza previa | Sin versionar (`??`) | Medio: `Grupo-N-4-PortalAdopcion_interno/` (108 MB) contiene un `.git` y una copia anidada del propio repo; el resto (`referencia/`, `duplicados_exactos/`, `copias_de_seguridad/`) son los archivos movidos en el punto 2 |
| `Grupo-N-4-PortalAdopcion/` (raíz del repo) | 3,2 MB | Wrapper suelto con solo `Tests/` | Sin versionar | Bajo |
| `TEORIA PORTAL ADOPCION/` (raíz del repo) | 0,1 MB | Apuntes de teoría (9 archivos) | Sin versionar; también presente en otros duplicados | Bajo |

## 5. Espacio recuperable

| Bloque | Tamaño |
|---|---|
| Fuera del repo (`Portal_Adopcion_`, `ELEMENTOS REPETIDOS`, `.zip`) | ~618,7 MB |
| Dentro del repo (`REDUNDANCIAS/`, `Grupo-N-4-PortalAdopcion/`, `TEORIA...`) | ~115,7 MB |
| **Total** | **~734,4 MB** |

## 6. Acciones ejecutadas

1. **Borrado (bajo riesgo)**: `Portal_Adopcion_/`, `ELEMENTOS REPETIDOS (NO VER)/Grupo-N-4-PortalAdopcion-completado/`, `documentacion portaladopcionpractica/`, `COPIA PORTALADOPCION.zip`, `Grupo-N-4-PortalAdopcion/` (raíz), `TEORIA PORTAL ADOPCION/`.
2. **Borrado tras respaldo**: `ELEMENTOS REPETIDOS (NO VER)/Grupo-N-4-PortalAdopcion/`, `.../COPIAPORTALADOPCION/`, `REDUNDANCIAS/`. Todo quedó dentro del `.zip` de respaldo.
3. **Pendiente en Git**: confirmar con un commit la reorganización del punto 2 (o revertirla). No se tocó el índice; el árbol sigue a mitad de camino a propósito.
4. **Pendiente de actualizar**: `LEEME_PRIMERO.md`, cuya sección «Qué leer» aún menciona `referencia/`, `propuestas-pr/` y `Plan_Ramas.csv` en su ubicación antigua.

Nota técnica: parte de los directorios estaban en `ReparsePoint` (OneDrive) y con rutas de más de 260 caracteres; el borrado requirió `\\?\` y `rd /s /q`. El proyecto canónico no se modificó y `dotnet test` siguió en 62/62.
