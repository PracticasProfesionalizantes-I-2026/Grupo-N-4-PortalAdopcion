# Checklist de entrega — Portal de Adopción vs. base SistemaHelpDesk

> Grupo N° 4 · 08/10/2026. Se tomó como **base de referencia de artefactos** la
> entrega `Grupo-N-5-SistemaHelpDesk` (zip en `Downloads`) y se verificó que cada
> artefacto exista en el Portal. Las filas marcadas como *(nuevo)* fueron
> agregadas en esta iteración para equiparar la estructura.

| Artefacto esperado (base HelpDesk) | Portal de Adopción (copia `PortalAdopcion_Prueba`) | Estado |
| --- | --- | --- |
| `README.md` con estructura y comandos | `README.md` | ✔ |
| `AGENTS.md` con reglas de trabajo y arquitectura | `AGENTS.md` | ✔ |
| `Directory.Build.props` (dotnet format, tratamientos) | `Directory.Build.props` | ✔ |
| `src` por capas: API / BusinessLogic / DataAccess / Shared / Tests | `API` / `BusinessLogic` / `DataAccess` / `Shared` / `Tests` | ✔ |
| Colección Bruno por módulo (`bruno/`) | `bruno/` con 1 archivo por CU (18 + login) + environment `Local.bru` | ✔ |
| `Documentos/Tp-entrega/Casos de uso/cu-*.md` por caso | `Documentos\Tp-entrega\Casos de uso\cu-01..cu-18.md` *(nuevo, espejo de `CASOS DE USO/`)* | ✔ |
| Zip de la documentación de CU | `Documentos\Tp-entrega\Casos de uso - Portal de Adopcion.zip` *(nuevo)* | ✔ |
| `extract_docx.ps1` (utilidad de extracción) | `extract_docx.ps1` *(nuevo, parametrizado)* | ✔ |
| Texto extraído de la especificación (`extracted_text.txt`) | `Documentos\Tp-entrega\extracted_text.txt` *(nuevo)* | ✔ |
| `Documentos/Ejemplo(Profes)/*.url` (ejemplos del docente) | `Documentos\Ejemplo(Profes)\CU-02 Alta de Medico.md.url` + `GUIA-Especificacion-Casos-de-Uso.md.url` *(nuevo)* | ✔ |
| CI en `.github/` + PR template | `.github/workflows/dotnet.yml` + `pull_request_template.md` (la base HelpDesk NO lo tiene; acá es un extra) | ✔+ |
| Extra: modelo de RN unificado + informe técnico + actores | `COSAS/Modelo-de-Estados-y-Reglas-de-Negocio.md`, `Informe-Tecnico-Completo.md`, `Extracto-ACTORES-Original.txt` | ✔ |
| Extra: diagrama de flujo de estados | `COSAS/Diagrama-de-Estados.md` (Mermaid) + `.puml` | ✔ |

**Conclusión:** la copia de referencia cumple (y supera en CI y diagramas) la
baseline de artefactos. Los pendientes restantes NO son de estructura sino de
**traslado**: integrar `feature/base-api` → `main` y crear las 18 ramas
`feature/cu-XX` en el repo real `Grupo-N-4-PortalAdopcion` (ver
`CUs_POR_RAMA/` y `Organizacion_Por_Caso_de_Uso/`).