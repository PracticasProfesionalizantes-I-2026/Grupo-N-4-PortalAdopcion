# Caso de Uso: Reportar Incidente

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.
> Fuera del MVP no se envían correos automáticos: el incidente **urgente** se
> registra con máxima prioridad y queda visible para la administración
> (decisión de alcance documentada en `COSAS/Modelo-de-Estados-y-Reglas-de-Negocio.md`).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-18 |
| **Nombre** | Reportar Incidente |
| **Actor Principal** | Voluntario |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Voluntario → registrar incidencias del proceso de seguimiento; Administración → visibilizar incidentes urgentes; Sistema → auditar cada incidente |
| **Disparador (Trigger)** | El voluntario registra un incidente (urgente o normal) durante el seguimiento de un proceso |
| **Prioridad / Frecuencia** | Media; frecuencia variable (incidentes del día a día) |
| **Reglas de negocio relacionadas** | RN-16 (el incidente corresponde a una actividad/tarea asignada al voluntario); RN-17 (solo personal autorizado registra incidentes); RN-08 (auditoría); incidente urgente = máxima prioridad visible para administración |

---

### 1. BREVE DESCRIPCIÓN
Permite al voluntario reportar incidentes (urgentes o normales) ocurridos durante
el seguimiento, dejando registro persistente y auditable; los urgentes se marcan
con máxima prioridad para la administración.

### 2. PRECONDICIONES
1. El voluntario debe estar autenticado (Token JWT válido) y tener asignada la actividad/tarea del proceso (**RN-16**).
2. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/incidentes` con un JSON que contiene el tipo de incidente, la gravedad (`urgente` o `normal`), el detalle y la entidad relacionada (proceso/mascota).
2. La **Capa de Presentación** (`IncidentesController`) valida que el JSON sea estructuralmente correcto, los campos obligatorios y la gravedad.
3. La **Capa de Negocio** (`IncidenteService`) verifica la asignación del voluntario (**RN-16**) y la autorización (**RN-17**).
4. La **Capa de Persistencia** registra el incidente; si es `urgente`, se marca con **máxima prioridad** y queda visible para la administración.
5. El Sistema registra la operación en auditoría (**RN-08**).
6. El Sistema devuelve un código **201 Created** con los datos del incidente registrado.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota o cuerpo vacío).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Detalle vacío o gravedad inválida (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el detalle está vacío o la gravedad no es `urgente`/`normal`.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Entidad relacionada inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el proceso o la mascota referenciados no existen en la **Capa de Persistencia**.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **3b. Tarea no asignada o sin autorización (HTTP 403 Forbidden):**
  1. Si en el Paso 3 el voluntario no tiene asignada la actividad o no posee el permiso (**RN-16/RN-17**).
  2. El Sistema rechaza la operación.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **4a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 4 la **Capa de Persistencia** no puede guardar el incidente.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Incidente urgente:** se marca con máxima prioridad y queda visible para la administración; no se envía correo automático (decisión de alcance MVP).
2. **Incidente normal:** queda registrado para consulta y seguimiento por la administración.
3. El actor puede enviar el JSON desde el panel web (formulario de incidentes / "notificación urgente") o desde un cliente HTTP.

### 6. POSTCONDICIONES
1. El incidente queda persistido en la tabla `Incidentes` con su tipo, gravedad, detalle y entidad relacionada.
2. Los incidentes urgentes quedan visibles con máxima prioridad para la administración.
3. La operación queda registrada en auditoría (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del incidente. |
| `400` | Bad Request | Fallo en la validación de esquema, detalle vacío o gravedad inválida. |
| `403` | Forbidden | Tarea no asignada o sin autorización (RN-16/RN-17). |
| `404` | Not Found | Entidad relacionada (proceso/mascota) inexistente. |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** formato del JSON, detalle no vacío y gravedad válida.
- **Verificación (Negocio, → 403/404):** asignación de la tarea (**RN-16**) y autorización (**RN-17**) → 403; existencia de la entidad relacionada (`ProcesoNotFoundException`/`MascotaNotFoundException` → 404). La máxima prioridad del incidente urgente se asigna en la Capa de Negocio y la auditoría (**RN-08**) cierra la operación.

### Matriz de trazabilidad CU-18 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `IncidenteService_ReportarIncidente_RetornaRegistro` | `POST /api/incidentes_ReportaIncidente_Returns201` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `POST /api/incidentes_ConJsonInvalido_Returns400` |
| 2a. Detalle vacío / gravedad inválida | `400 Bad Request` | — (se detecta vía `[Required]` en Presentación) | `POST /api/incidentes_ConDetalleVacio_Returns400` |
| 3a. Entidad inexistente | `404 Not Found` | `IncidenteService_ConEntidadInexistente_LanzaNotFoundException` | `POST /api/incidentes_ConEntidadInexistente_Returns404` |
| 3b. Tarea no asignada | `403 Forbidden` | `IncidenteService_ConTareaNoAsignada_LanzaAccesoDenegadoException` | `POST /api/incidentes_ConTareaNoAsignada_Returns403` |
| 5.1 Incidente urgente | `201 Created` | `IncidenteService_ReportarUrgente_MarcaMaximaPrioridad` | `POST /api/incidentes_ReportaUrgente_Returns201` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).