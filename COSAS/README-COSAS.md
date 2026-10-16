# COSAS — Índice de documentación

> Carpeta de respaldo consolidado del Portal de Adopción de Mascotas (Grupo N° 4).
> **Contexto:** el 10/09/2026 los archivos de `BUSINESS/` y `CASOS DE USO/` y el `cosas leer.docx` original desaparecieron del disco local (nunca estuvieron en git). Esta carpeta reúne todo el análisis, el modelo y los respaldos para poder regenerar la documentación.

## Contenido

| Archivo | Qué es | Procedencia |
| --- | --- | --- |
| `README-COSAS.md` | Este índice | — |
| `Informe-Tecnico-Completo.md` | Análisis técnico/arquitectónico en 7 secciones (contexto, diferencias, CU, lógica, riesgos, mejoras, conclusión) | Generado 10/09/2026 |
| `Pendientes-Alta-Prioridad.md` | Documento aparte con las diferencias pendientes de prioridad alta (P1..P6) y orden de ejecución | Generado 10/09/2026 |
| `Modelo-de-Estados-y-Reglas-de-Negocio.md` | Máquina de estados (proceso/mascota/cuenta), RN-01..17 y correspondencia con RN antiguas | Regenerado de la autoría original (se perdió el `.md`) |
| `Indice-Casos-de-Uso-y-Trazabilidad.md` | Catálogo CU-01..18 con actores, acciones, resultados, endpoints, HTTP y matriz de tests | Regenerado 10/09/2026 |
| `Diferencias-cambios-y-justificacion.md` | Informe de diferencias/cambios y porqué (pasó a .md) | Copia exacta del contenido original |
| `cosas leer.docx` | Mismo informe en formato Word (estilos y encabezados) | Regenerado 10/09/2026 desde `build_docx.ps1` |
| `Extracto-Spec-Original.txt` | Texto extraído del documento original `Portal de adopcion para mascotas (4).docx` | Respaldo del original (perdido) |
| `Extracto-ACTORES-Original.txt` | Texto extraído de `ACTORES.docx` | Respaldo del original |

## Estado de los originales (pérdida del 10/09/2026)

- `BUSINESS/Portal de adopcion para mascotas (4).docx` — perdido → respaldo de texto en `Extracto-Spec-Original.txt`.
- `BUSINESS/MOCKUPS.excalidraw` — perdido sin respaldo (solo se conserva el análisis en `Informe-Tecnico-Completo.md`).
- `CASOS DE USO/cu-01..05` (md) y sus actores — perdidos; los actores quedan en `Extracto-ACTORES-Original.txt`; el comportamiento de los CU está resumido en `Indice-Casos-de-Uso-y-Trazabilidad.md`.
- `CASOS DE USO/cu-06..18` (md) — perdidos; reconstruibles desde `Indice-Casos-de-Uso-y-Trazabilidad.md` + plantilla de `Documentos EJEMPLO/GUIA-Especificacion-Casos-de-Uso.md`.
- `COSAS/cosas leer.docx` — regenerado (esta vez también con versión `.md`).

## Recomendación urgente

Commitear y pushear esta carpeta a git (repositorio `PracticasProfesionalizantes-I-2026/Grupo-N-4-PortalAdopcion`) para evitar otra pérdida. Ver `Pendientes-Alta-Prioridad.md` → P6.