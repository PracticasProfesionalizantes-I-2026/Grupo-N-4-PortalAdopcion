# Pendientes de Alta Prioridad — Portal de Adopción

> Grupo N° 4. Documento separado con las **diferencias pendientes de alta prioridad** y las acciones requeridas. Generado el 10/09/2026.
> Contexto: los archivos originales (`BUSINESS/`, `CASOS DE USO/cu-01..18`, `COSAS/cosas leer.docx`) se perdieron del disco local el 10/09/2026 y NO formaban parte del control de versiones. Este documento queda como registro de trabajo pendiente.

---

## Resumen ejecutivo

Quedan **6 pendientes de prioridad alta** antes de poder codificar el MVP. Los primeros 4 son puramente documentales (hoy, tras la pérdida de archivos, equivalen a *reescribir* la especificación). El 5.º y el 6.º son la protección del trabajo contra nuevas pérdidas.

| # | Pendiente | Tipo | Esfuerzo |
| --- | --- | --- | --- |
| P1 | Fusionar CU-05 en CU-02 (eliminar duplicación Solicitud/Proceso) | Documental | Bajo |
| P2 | Renumerar RN en CU-01..05 al catálogo RN-01..17 | Documental | Bajo |
| P3 | Generar el diagrama de flujo de estados (mascota/proceso) requerido por la spec | Documental | Bajo |
| P4 | Corregir la precondición contradictoria de CU-03 | Documental | Bajo |
| P5 | Reconstruir la documentación perdida desde este respaldo en `COSAS/` | Recuperación | Alto |
| P6 | Commitear/pushear todo a git como política (evitar pérdida recurrente) | Proceso | Bajo |

---

## P1 — Fusionar CU-05 en CU-02

- **Problema:** dos CU describen la misma creación en estado `Pendiente` con las mismas RN (contras: duplicación de lógica y de tests).
- **Decisión ya tomada (documentada en `Modelo-de-Estados-y-Reglas-de-Negocio.md`):** existe una única entidad `SolicitudDeAdopcion` ("Solicitud" = "Proceso").
- **Acción:** convertir CU-05 en una subvariación AUTOMÁTICA del paso 6a de CU-02. Resultado esperado de CU-02: `POST /solicitudes` → `201` con solicitud en `Pendiente` (el sistema genera el código de confirmación RN-07 y registra auditoría RN-08).

## P2 — Renumerar RN en CU-01..05

- **Problema:** CU-01..05 referencian las RN antiguas (RN-03/RN-04, RN-18.1..18.5) que no existen en el catálogo unificado.
- **Acción:** reemplazar las menciones usando la tabla de correspondencia de `Modelo-de-Estados-y-Reglas-de-Negocio.md`:

| RN antigua | RN unificada | Comentario |
| --- | --- | --- |
| RN-01 (CU-01) | RN-01 | Blacklist |
| RN-02 (CU-01) | RN-02 | Gestión de sanciones |
| RN-03 (CU-02) | RN-03 | Confirmación de postulación (copia oculta) |
| RN-04 (CU-02) | RN-04 | Reserva única de mascota |
| RN-05 (CU-04) | RN-05 | Evaluación (15 días mínimo, motivo obligatorio) |
| RN-06 (CU-04) | RN-06 | Autorización con entidad y fecha |
| RN-18.1 | RN-07 | Código de confirmación |
| RN-18.2 | RN-08 | Mascota reservada/no disponible |
| RN-18.3 | RN-03 | (mismo que confirmación de postulación) |
| RN-18.4 | RN-09 | Auditoría → ver nota |
| RN-18.5 | RN-10 | Timeout 7 días |

> **Nota de diseño:** en el catálogo unificado, RN-08 = Auditoría, RN-09 = Mascota no disponible, RN-10 = Timeout 7 días. Esta tabla se debe ajustar al modelo definitivo que quede en `Modelo-de-Estados-y-Reglas-de-Negocio.md`.

## P3 — Diagrama de flujo de estados

- **Problema:** la spec pide explícitamente un diagrama de flujo y nunca se generó.
- **Acción:** producir un diagrama de estados con PlantUML (`.puml` + imagen exportada PNG/SVG) que cubra:
  - Estados del **proceso**: `Pendiente` → `En Observación`/`Aprobada`/`Rechazada` → `En Entrevista` → `En Periodo de Prueba` → `Adoptada`; terminales `Rechazada`, `Cancelada`, `Abandonada`.
  - Estados de la **mascota**: `Disponible`, `Reservada`, `En Entrevista`, `En Periodo de Prueba`, `Adoptada`, `En Tratamiento`, `Inactiva/Baja`.
  - Estados de la **cuenta**: `Activa`, `Suspendida`, `Dada de baja`; sanción blacklist.
- **Fuente:** transiciones válidas definidas en `Modelo-de-Estados-y-Reglas-de-Negocio.md`.

## P4 — Precondición contradictoria de CU-03

- **Problema:** CU-03 (registrar historial clínico inicial) declara precondición "la ficha clínica debe existir", lo que contradice que el CU la crea.
- **Acción:** decidir y documentar cuándo nace el historial: **(a)** al registrar la mascota (CU-09) con carta de donación/tránsito, o **(b)** con el primer evento médico (CU-03/CU-10). Recomendado: la opción (b) — el historial se crea junto con el primer evento; CU-03 pasa a ser el caso base de CU-10.

## P5 — Reconstruir la documentación perdida

- **Estado actual:** los archivos originales (`BUSINESS/Portal de adopcion...docx`, `MOCKUPS.excalidraw`, `CASOS DE USO/cu-01..18.md`, `ACTORES.docx` en la carpeta original, `COSAS/cosas leer.docx`) se perdieron del disco local.
- **Lo que queda (respaldo en `COSAS/`):**
  - Extracto de texto de la spec original: `Extracto-Spec-Original.txt`
  - Extracto de texto de ACTORES: `Extracto-ACTORES-Original.txt`
  - Informe de diferencias/cambios: `Diferencias-cambios-y-justificacion.md` y `cosas leer.docx`
  - Modelo de estados + RN: `Modelo-de-Estados-y-Reglas-de-Negocio.md`
  - Catálogo completo de casos de uso: `Indice-Casos-de-Uso-y-Trazabilidad.md`
  - Análisis técnico: `Informe-Tecnico-Completo.md`
- **Pendiente:** si el material original aparece en OneDrive (papelera/versiones anteriores) o en otra PC del grupo, restaurarlo ahí. Si no, los 13 CU detallados (cu-06..18) deben regenerarse a partir de `Indice-Casos-de-Uso-y-Trazabilidad.md`.

## P6 — Control de versiones

- **Regla:** commitear y pushear toda la documentación a GitHub (el repo remoto `PracticasProfesionalizantes-I-2026/Grupo-N-4-PortalAdopcion` está configurado).
- **Acción:** `git add` de `COSAS/` y commit inicial de esta documentación para que `BUSINESS/`, `CASOS DE USO/` y `COSAS/` dejen de ser untracked.
- **Justificación:** todos los archivos perdidos eran no versionados; con control de versiones activo el riesgo de pérdida desaparece.

---

## Orden de ejecución sugerido

1. **P6** (commitear el respaldo de `COSAS/` ahora mismo) → meseta de seguridad.
2. **P2 + P1** (refactor documental de CU-01..05) junto con la regeneración de los `.md`.
3. **P3 + P4** (diagrama y precondición de CU-03).
4. **P5** resta solo si no aparece el material original (regenerar 13 CU detallados).