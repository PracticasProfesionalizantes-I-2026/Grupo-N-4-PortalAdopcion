# Informe de Auditoría y Evaluación Integral de Proyecto

**Proyecto:** Portal de Adopción de Mascotas para ONGs  
**Autores del Proyecto:** Catalina Giordano, Francisco Ledesma (Grupo N° 4)  
**Evaluador:** Arquitecto de Software Senior, Analista Funcional y Auditor QA  
**Fecha:** Noviembre 2026  
**Alcance de la Auditoría:** Documentación de Requerimientos, Especificación de Casos de Uso (`CASOS DE USO/`, `COSAS/`, `README.md`) y Código Fuente (`API/`, `BusinessLogic/`, `DataAccess/`, `Shared/`, `Tests/`).

---

## 1. Visión General del Proyecto y Madurez del SDLC

El proyecto **Portal de Adopción de Mascotas para ONGs** presenta un grado de avance y madurez técnica **muy superior a la media académica**, situándose en un estadio pre-profesional avanzado. La solución implementa una plataforma web completa bajo **.NET 10 (C# 13)**, respaldada por Entity Framework Core (SQLite WAL), autenticación JWT y frontend SPA vanilla en HTML5/CSS/JavaScript.

A nivel de **Ciclo de Vida de Desarrollo de Software (SDLC)**, se observa un proceso estructurado con las siguientes características notables:
1. **Fase de Análisis y Especificación:** Cuenta con especificaciones exhaustivas para 18 Casos de Uso (`CU-01` a `CU-18`), tablas de reglas de negocio unificadas (`RN-01` a `RN-17`), matrices de trazabilidad, modelado de estados (diagramas PlantUML y Mermaid) y contratos HTTP.
2. **Fase de Diseño y Arquitectura:** Se definió y respetó una arquitectura multicapa desacoplada: Presentación (`API` / Controladores REST) $\rightarrow$ Lógica de Negocio (`BusinessLogic` / `IPortalService`) $\rightarrow$ Persistencia (`DataAccess` / `IPortalRepository` / `PortalAdopcionDbContext`) $\rightarrow$ Contratos y Tipos Comunes (`Shared`).
3. **Fase de Implementación y Concurrencia:** Destaca la preocupación por la integridad transaccional (control de concurrencia optimista con tokens `Version`, índices únicos filtrados en base de datos para impedir doble postulación simultánea y SQLite configurado en modo `WAL`).
4. **Fase de Pruebas y QA:** Dispone de una suite integral de pruebas con xUnit, combinando tests de integración con base de datos en memoria y tests unitarios puros de lógica de negocio aislados mediante mocks (`Moq`), alcanzando 63 pruebas automatizadas.

No obstante, la auditoría detecta discrepancias de diseño, deuda técnica en la separación de responsabilidades de ciertas clases (antipatrón *God Class* en el servicio), bugs menores en flujos específicos y discrepancias de códigos de estado HTTP frente a la documentación de los Casos de Uso.

---

## 2. Fortalezas Identificadas

### 2.1 En el Análisis Funcional y Diseño Documental
- **Modelado Robusto de Estados y Reglas de Negocio:** La separación de transiciones entre `SolicitudDeAdopcion` y `Mascota` resuelve uno de los problemas conceptuales más comunes en sistemas de adopción: entender que **Aprobar una postulación no equivale a Entregar/Adoptar al animal**, contemplando fases intermedias indispensables (entrevista, período de prueba de 15 días, alertas de comportamiento y validación clínica).
- **Especificaciones de Casos de Uso Rigurosas:** Los 18 archivos de casos de uso siguen una estructura formal homogénea (Actores, Pre/Postcondiciones, Flujo Principal, Caminos Alternativos tipificados por código HTTP y Matrices de Trazabilidad).
- **Gestión Documental del Desvío Histórico:** El equipo documentó con honestidad técnica la resolución de incidencias previas (fusión de `CU-05` dentro de `CU-02` documentada en `Pendientes-Alta-Prioridad.md` y unificación de reglas `RN-01..RN-17`).

### 2.2 En la Arquitectura y Código Fuente
- **Aislamiento Estricto de Persistencia:** La capa de Controladores (`PortalControllers.cs`) no interactúa directamente con `DbContext`; delega 100% de la lógica a `IPortalService`. A su vez, `PortalService` consume `IPortalRepository`, manteniendo las consultas EF con `AsNoTracking()` para lecturas y transacciones atómicas para escrituras.
- **Seguridad en Identidad y Criptografía:** Implementación de PBKDF2-SHA256 con 210.000 iteraciones y sal criptográfica de 16 bytes (`PasswordSecurity.cs`), con validación de versión de cuenta (`u.Version`) en eventos JWT para invalidar sesiones activas tras cambios de rol o estado.
- **Protección contra Concurrencia en Persistencia:** Aplicación de índices únicos filtrados en la base de datos (`SolicitudDeAdopcion.MascotaId` y `AdoptanteId` para estados activos), evitando colisiones de reserva concurrentes que superen validaciones en memoria.
- **Prevención de Vulnerabilidades Comunes:** Sanitización contra CSV Formula Injection en la exportación (`ReportFiles.Csv`), manejo centralizado de excepciones con Problem Details (RFC 7807) en middleware, y restricción de binding JSON estricto (`JsonUnmappedMemberHandling.Disallow`).

---

## 3. Análisis de Brechas (Gap Analysis)

### 3.1 Documentación vs. Casos de Uso (Fase 1)
- **Trazabilidad Funcional General:** Existe una cobertura nominal de los 18 Casos de Uso documentados frente al código.
- **Orfandad y Fusión Funcional (`CU-05` vs `CU-02`):** Documentalmente se mantiene el archivo independiente `cu-05-crear-proceso-de-adopcion.md`, pero en la arquitectura real no existe la entidad "Proceso" separada de "Solicitud". Aunque esto está justificado técnicamente como una optimización simplificadora, documentalmente `CU-05` figura formalmente como un caso de uso independiente cuando en realidad es un subpaso de `CU-02`.
- **Precondición Contradictoria en Historial Clínico (`CU-03`):** El documento `cu-03-registrar-historial-clinico-inicial.md` señala como precondición que *"la ficha clínica debe existir previamente"*, cuando el objetivo funcional del caso de uso es precisamente dar de alta la ficha clínica inicial junto con la primera observación.
- **Discrepancia en Códigos HTTP (Documentación vs Implementación):**
  - En `CU-01` y `CU-02`, la documentación establece que un usuario en Blacklist que intenta postularse debe recibir `409 Conflict`. Sin embargo, el código (`PortalService.cs`, línea 402) lanza `AccessException` que el middleware traduce a `403 Forbidden`. Funcionalmente `403` es más semántico a nivel REST, pero existe una desalineación con la especificación documental.
- **Mapeo de Actores y NFRs en Precondiciones:** La especificación de casos de uso asume componentes como *"DbInitializer"* en las precondiciones, mezclando detalles de infraestructura de software con precondiciones del dominio de negocio.

### 3.2 Diseño vs. Código Fuente (Fase 2)
- **Requerimientos No Implementados (Gaps Core):**
  1. *Filtros Avanzados y Paginación en Catálogo (`CU-09` / Spec Funcional 5.1):* La especificación original exige búsqueda y filtrado por especie, edad, sexo y estado. La API implementada en `MascotasController.List` únicamente acepta el parámetro opcional `estado`. Las búsquedas complejas quedaron delegadas al filtrado en memoria en el cliente web.
  2. *Carga Real de Imágenes/Fotografías:* La especificación funcional contempla fotografías de los animales. En la implementación, se persiste únicamente un campo de texto con una URL (`FotoUrl`), sin almacenamiento local ni validación de formatos de imagen.
  3. *Límite de Voluntarios en Persistencia (RN-16):* La regla de negocio que limita a un voluntario a un máximo de 2 procesos activos se valida únicamente en la capa de servicio (`PortalService`), careciendo de una restricción o índice en base de datos para evitar condiciones de carrera si dos administradores asignan al mismo voluntario simultáneamente.
- **Scope Creep (Lógicas no especificadas en Casos de Uso):**
  - Endpoint `POST /api/demo/reiniciar`: Lógica de reset total de datos en base de datos con borrado en cascada manual (`PortalRepository.ResetDemoDataAsync`). Aunque está restringida a entorno Development y loopback, no pertenece al dominio del negocio.
  - Generador artesanal de archivos PDF (`ReportFiles.Pdf`): Implementación de un generador de binarios PDF de bajo nivel manipulando offsets y objetos PostScript/PDF sin bibliotecas externas.

---

## 4. Conclusión de Arquitectura y Calidad de Código

### 4.1 Principios SOLID y Limpieza de Código
- **Violación del Principio de Responsabilidad Única (SRP - God Service):**  
  La clase `PortalService.cs` acumula 870 líneas de código y monopoliza las operaciones de los 18 casos de uso: autenticación, usuarios, catálogo, clínica veterinaria, workflows de adopción, auditoría, alertas, incidentes y reportes. Debe refactorizarse modularmente en servicios especializados (`AdopcionWorkflowService`, `MascotaService`, `ClinicaService`, `UsuarioService`, `ReporteService`).
- **Controladores Anémicos vs. Responsabilidad de Presentación:**  
  Los controladores están agrupados en un único archivo (`PortalControllers.cs`), donde algunos métodos contienen lógica de branching (`ControlController.Report`, `BlacklistController.Delete`).

### 4.2 Calidad Estática, Rendimiento y Resiliencia
- **Gestión de Recursos SMTP:**  
  En `CorreoJob.cs`, la instancia `SmtpClient` se crea y destruye dentro de un bucle por cada mensaje en lugar de reutilizar conexiones o utilizar abstracciones modernas como `MailKit` / `MimeKit`.
- **Riesgo en Timeout de Migraciones:**  
  Existe una lógica personalizada (`CleanupStaleMigrationLockAsync`) en `Program.cs` para eludir bloqueos de SQLite causados por sincronización de carpetas en la nube (OneDrive). Si bien resuelve un síntoma de desarrollo, altera el comportamiento estándar de migración en producción.

---

## 5. Dictamen del Auditor

| Dimensión Evaluada | Calificación | Estado |
|---|---|---|
| **Completitud Funcional (Casos de Uso)** | 9.0 / 10 | **Aprobado con Distinción** |
| **Diseño Documental y Trazabilidad** | 8.5 / 10 | **Aprobado** |
| **Arquitectura de Software y Patrones** | 8.5 / 10 | **Aprobado** |
| **Calidad de Código y Robustez Transaccional** | 9.0 / 10 | **Aprobado con Distinción** |
| **Aseguramiento de Calidad (Tests y QA)** | 9.5 / 10 | **Excelente** |

**Veredicto General:** **PROYECTO APROBADO CON NIVEL SOBRESALIENTE.**  
El grupo ha demostrado una comprensión profunda de las problemáticas reales de concurrencia, seguridad en APIs web y modelado de reglas de negocio complejas. Las oportunidades de mejora detectadas corresponden a refinamientos de diseño hacia arquitecturas limpias desacopladas y alineación de discrepancias documentales menores.

