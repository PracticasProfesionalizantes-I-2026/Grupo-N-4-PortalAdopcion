# Registro de correcciones aplicadas — Portal de Adopción

**Proyecto:** Grupo N.º 4 — Portal de Adopción  
**Versión documentada:** `Grupo-N-4-PortalAdopcion-implementado.zip`  
**Tipo de intervención:** correcciones sobre código existente  
**Alcance:** cambios puntuales en lógica de negocio, sin incorporar funcionalidades nuevas.

---

## 1. Resumen general

Se aplicaron correcciones sobre el servicio de lógica de negocio para atender observaciones de la auditoría relacionadas con:

- Restricción de acceso de voluntarios a perfiles y solicitudes.
- Control de asignación de entrevistas, consultas de entrevistas y seguimientos.
- Protección de determinados datos del perfil durante un proceso de adopción activo.
- Prevención de la creación de un historial clínico duplicado después de una transferencia.
- Inclusión del día completo al filtrar reportes por una fecha final sin horario.

Las correcciones se realizaron modificando métodos y condiciones que ya existían. **No se agregaron endpoints, funcionalidades, clases, interfaces, métodos nuevos, paquetes ni dependencias.**

## 2. Archivo modificado

| Archivo | Cambio |
|---|---|
| `BusinessLogic/Services/PortalService.cs` | Se ajustaron condiciones de autorización, validaciones y filtros de fechas en métodos existentes. |

La comparación entre el ZIP original y el ZIP implementado confirmó que ambos contienen los mismos **1.160 archivos** y que el único archivo cuyo contenido cambió fue `PortalService.cs`. No se detectaron archivos agregados ni eliminados en esa comparación.

## 3. Correcciones aplicadas

### 3.1. Consulta de perfiles por parte de voluntarios

**Ubicación:** `PortalService.cs` — método `ProfilesAsync`.

**Situación corregida:** anteriormente, el voluntario podía consultar el listado general de adoptantes, igual que un administrador.

**Cambio aplicado:** se separó el comportamiento por rol:
- El administrador conserva la consulta general de perfiles.
- El voluntario obtiene los perfiles vinculados a solicitudes que tiene asignadas o que están pendientes/en observación y todavía no tienen un voluntario asignado.

**Impacto en el código:** se modificó una condición existente y se incorporó dentro del mismo método una consulta filtrada utilizando las entidades y propiedades ya existentes. No se creó un método nuevo.

**Impacto funcional:** se reduce la exposición de información de adoptantes que no está relacionada con el trabajo del voluntario. El administrador conserva su acceso general.

### 3.2. Consulta individual de perfiles

**Ubicación:** `PortalService.cs` — método `ProfileAsync`.

**Situación corregida:** faltaba comprobar que el perfil consultado por un voluntario estuviera relacionado con una solicitud asignada a él o disponible para evaluación.

**Cambio aplicado:** antes de devolver el perfil, se verifica que exista una solicitud del adoptante que cumpla alguna de esas condiciones. Si no existe, se devuelve una excepción de acceso restringido.

**Impacto en el código:** se agregó una condición de autorización dentro del método existente. No se modificó el DTO ni el endpoint.

**Impacto funcional:** un voluntario ya no debería poder consultar perfiles ajenos a sus solicitudes asignadas o disponibles para evaluación.

### 3.3. Edición de perfiles durante un proceso activo

**Ubicación:** `PortalService.cs` — método `UpdateProfileAsync`.

**Situación corregida:** se podían modificar la edad y el estado civil del adoptante mientras tenía un proceso de adopción activo.

**Cambio aplicado:**
- Se agregó el control de acceso del voluntario al perfil que intenta editar.
- Se consulta si el adoptante tiene una solicitud en un estado considerado activo por la regla existente (`EstadosActivos`).
- Si existe un proceso activo y se intenta cambiar la edad o el estado civil, se rechaza la operación mediante un conflicto.
- Se conserva el resto de la edición de datos del perfil.

**Impacto en el código:** se modificó el método existente y se reutilizaron `EstadosActivos`, `Civil(...)` y las excepciones ya presentes. No se creó una regla o servicio independiente.

**Impacto funcional:** durante un proceso activo se mantienen protegidos esos dos datos, mientras que los demás campos continúan editándose según las validaciones existentes.

### 3.4. Listado de solicitudes para voluntarios

**Ubicación:** `PortalService.cs` — método `RequestsAsync`.

**Situación corregida:** el listado general de solicitudes no diferenciaba suficientemente el alcance de consulta de cada rol.

**Cambio aplicado:**
- El adoptante conserva el listado de sus propias solicitudes.
- El voluntario obtiene solicitudes asignadas a él y solicitudes pendientes/en observación que todavía no tienen voluntario asignado.
- Se exige explícitamente el rol de administrador o veterinario para el listado general restante.

**Impacto en el código:** se incorporó una rama de autorización y filtrado en el método existente.

**Impacto funcional:** el voluntario deja de recibir el listado completo de solicitudes. Puede seguir viendo solicitudes disponibles para evaluación y las que tiene asignadas.

### 3.5. Consulta individual de solicitudes

**Ubicación:** `PortalService.cs` — método `RequestAsync`.

**Situación corregida:** la comprobación de propiedad utilizada para adoptantes no contemplaba correctamente el acceso de voluntarios a solicitudes asignadas o disponibles.

**Cambio aplicado:**
- Para adoptantes se mantiene la comprobación de pertenencia de la solicitud.
- Para voluntarios se permite consultar solicitudes asignadas a ellos o pendientes/en observación sin asignación.
- Para los demás casos se exige el rol de administrador o veterinario.

**Impacto en el código:** se reemplazó la comprobación general anterior por condiciones según el rol dentro del método existente.

**Impacto funcional:** se delimita qué solicitudes puede consultar cada voluntario sin quitar al adoptante el acceso a sus propias solicitudes.

### 3.6. Cancelación de solicitudes

**Ubicación:** `PortalService.cs` — método `CancelAsync`.

**Situación corregida:** la cancelación no verificaba de manera específica que el voluntario fuera responsable de la solicitud.

**Cambio aplicado:** cuando el actor es un voluntario, se utiliza la comprobación existente `Assigned(a, s)` antes de continuar con la cancelación. Para el adoptante se conserva la comprobación de pertenencia.

**Impacto en el código:** se reutilizó un método de autorización que ya existía; no se agregó otro.

**Impacto funcional:** un voluntario no puede cancelar un proceso asignado a otro voluntario. La lógica de cancelación y sus reglas de negocio existentes se mantienen.

### 3.7. Asignación de entrevistas

**Ubicación:** `PortalService.cs` — método `ScheduleAsync`.

**Situación corregida:** un voluntario podía intentar asignar una entrevista a otro voluntario o intervenir en un proceso que ya estaba asignado a otra persona.

**Cambio aplicado:** si el actor es un voluntario, se verifica que:
- La solicitud no esté asignada a otro voluntario.
- El voluntario indicado para la entrevista sea el propio actor.

**Impacto en el código:** se agregó una condición de autorización dentro del método existente. Se conservó la búsqueda y validación del usuario voluntario mediante la lógica ya presente.

**Impacto funcional:** el voluntario solo puede asignarse entrevistas a sí mismo y no puede tomar procesos de otro voluntario.

### 3.8. Consulta de entrevistas

**Ubicación:** `PortalService.cs` — método `InterviewsAsync`.

**Situación corregida:** la consulta utilizaba una comprobación común que no diferenciaba adecuadamente los permisos del voluntario.

**Cambio aplicado:**
- El voluntario debe tener la solicitud asignada.
- El adoptante debe ser titular de la solicitud.
- Administradores y veterinarios deben cumplir el rol permitido.

**Impacto en el código:** se modificó la autorización del método existente. No se cambió el modelo de entrevista ni su respuesta.

**Impacto funcional:** se limita la consulta de entrevistas al actor autorizado según su rol.

### 3.9. Consulta de seguimientos

**Ubicación:** `PortalService.cs` — método `FollowupsAsync`.

**Situación corregida:** la consulta de seguimientos no diferenciaba correctamente el acceso de voluntarios, adoptantes y personal administrativo/profesional.

**Cambio aplicado:**
- El voluntario debe tener asignada la solicitud.
- El adoptante debe ser titular de la solicitud.
- Administradores y veterinarios deben cumplir el rol permitido.

**Impacto en el código:** se ajustaron las condiciones de autorización del método existente. Se mantiene el ordenamiento de los seguimientos por fecha.

**Impacto funcional:** se evita que un voluntario consulte seguimientos de procesos ajenos.

### 3.10. Prevención de duplicación de historiales clínicos

**Ubicación:** `PortalService.cs` — método `CreateHistoryAsync`.

**Situación corregida:** la comprobación existente buscaba un historial únicamente por `MascotaId`. Después de una transferencia, el historial podía quedar relacionado mediante `OrigenMascotaId`, por lo que la comprobación anterior podía no detectarlo.

**Cambio aplicado:** la verificación ahora busca un historial cuyo `MascotaId` o `OrigenMascotaId` coincida con la mascota.

**Impacto en el código:** se amplió la condición de una consulta existente. No se modificaron las entidades, las relaciones ni la estructura de la base de datos.

**Impacto funcional:** se evita crear un nuevo historial inicial cuando ya existe uno asociado a esa mascota o transferido desde ella.

### 3.11. Filtro de fecha final en auditoría

**Ubicación:** `PortalService.cs` — método `AuditAsync`.

**Situación corregida:** cuando la fecha final se recibía sin una hora específica, el filtro podía considerar como límite las 00:00 de ese día y excluir los eventos posteriores.

**Cambio aplicado:** si la fecha final tiene hora `00:00:00`, se calcula un límite al final de ese mismo día. Si ya contiene un horario, se conserva el valor recibido.

**Impacto en el código:** se agregó una variable local para calcular el límite y se reutilizó en el filtro existente. No se cambió el contrato del método.

**Impacto funcional:** una consulta con fecha final sin horario incluye los eventos de todo ese día.

### 3.12. Filtro de fecha final en exportación de reportes

**Ubicación:** `PortalService.cs` — método de exportación de reportes.

**Situación corregida:** el mismo problema del límite a medianoche afectaba los registros exportados por rango de fechas.

**Cambio aplicado:** se calcula `hastaLimite` y se utiliza como límite superior en las consultas de adopciones, solicitudes y mascotas.

**Impacto en el código:** se ajustaron los filtros existentes. No se incorporaron nuevos tipos de reporte ni columnas.

**Impacto funcional:** los reportes incluyen los registros correspondientes al día final completo cuando la fecha se proporciona sin horario.

---

## 4. Impacto general sobre el proyecto

| Aspecto | ¿Se modificó? | Detalle |
|---|---:|---|
| Código fuente | Sí | Se modificó un único archivo: `PortalService.cs`. |
| Métodos existentes | Sí | Se ajustaron condiciones y validaciones en métodos ya implementados. |
| Métodos nuevos | No | No se incorporaron métodos. |
| Clases o interfaces | No | Se conservaron las existentes. |
| DTO | No | No se modificaron contratos de entrada o salida. |
| Controladores y endpoints | No | No se agregaron ni modificaron rutas. |
| Base de datos | No | No se modificaron entidades, tablas, índices ni relaciones. |
| Migraciones | No | No se generaron migraciones. |
| Dependencias | No | No se agregaron paquetes. |
| Arquitectura | No | Se conservaron las capas y referencias del proyecto. |
| Autenticación JWT | No | Se dejó sin cambios por decisión expresa del usuario. |
| Documentación preexistente | No | No se modificó la documentación que ya estaba en el ZIP. |

### ¿Las correcciones afectaron el código?

**Sí.** Las correcciones modificaron la lógica interna de `PortalService.cs`, por lo que sí existe un cambio en el código fuente.

Sin embargo, el alcance fue acotado:
- No se alteró la estructura general del proyecto.
- No se cambiaron las firmas públicas de los métodos.
- No se agregaron endpoints ni funcionalidades.
- No se modificaron los modelos de persistencia ni la base de datos.
- Sí se modificó el comportamiento de autorización y validación en los escenarios descriptos.

Por lo tanto, no sería correcto afirmar que el código no fue afectado. La descripción precisa es que **se modificó código existente de manera puntual, sin ampliar la arquitectura ni agregar componentes nuevos**.

## 5. Elementos que quedaron fuera de esta implementación

Los siguientes temas de la auditoría no se consideran corregidos por este paquete:

- **Autenticación JWT:** se dejó expresamente fuera de alcance por decisión del usuario, debido a errores que habían aparecido al intentar modificarla en una etapa anterior. No se modificaron `Program.cs`, `TokenIssuer.cs` ni la configuración JWT como parte de esta intervención.
- **Limitación de intentos de login y registro:** no se incorporó rate limiting.
- **Concurrencia:** no se realizaron cambios para resolver condiciones de carrera.
- **Otros puntos de validación, contratos HTTP, rendimiento, configuración de producción y documentación:** no se abordaron en esta intervención.
- **Hallazgos que requieren nuevas clases, métodos, componentes o cambios de arquitectura:** quedaron fuera del alcance acordado.

Que un punto no figure entre las correcciones aplicadas significa que no debe considerarse resuelto.

## 6. Verificación realizada y pendiente

### Verificación realizada

- Se comparó el ZIP original con el ZIP implementado.
- Ambos contienen 1.160 archivos.
- No se detectaron archivos agregados ni eliminados en esa comparación.
- Se identificó un único archivo modificado: `BusinessLogic/Services/PortalService.cs`.

### Verificación pendiente

No se ejecutaron `dotnet build`, `dotnet test` ni pruebas de integración con Bruno durante esta intervención. Por lo tanto, este documento registra los cambios realizados, pero no certifica que la solución compile ni que todas las pruebas pasen.

Se recomienda verificar en el entorno de desarrollo:

1. Compilación de la solución completa.
2. Ejecución de todos los tests existentes.
3. Acceso de voluntarios a perfiles y solicitudes asignadas/no asignadas.
4. Restricción de edición de edad y estado civil durante procesos activos.
5. Cancelación y programación de entrevistas por voluntarios.
6. Consulta de entrevistas y seguimientos según rol.
7. Creación de historial clínico después de una transferencia.
8. Reportes con fecha final sin horario y con horario explícito.

---

## 7. Conclusión

La implementación consistió en una serie de ajustes puntuales sobre métodos ya existentes de `PortalService.cs`. Se corrigieron controles de acceso por rol, validaciones de integridad de datos y límites de fechas en consultas y exportaciones.

**No se agregaron funcionalidades, clases, métodos, endpoints ni dependencias.** El código sí fue modificado, pero únicamente en el archivo indicado y sin cambiar la arquitectura, los contratos públicos ni la persistencia.

La autenticación JWT y los demás hallazgos no incluidos permanecen pendientes; no deben interpretarse como corregidos. La compilación y las pruebas automatizadas también quedan pendientes de ejecución en un entorno con el SDK de .NET instalado.
