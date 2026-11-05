# Caso de Uso: Consultar Auditoría (Logs)

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-17 |
| **Nombre** | Consultar Auditoría (Logs) |
| **Actor Principal** | Administrador del Sistema |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Administración → trazar cambios de estado, usuarios y responsables; Sistema → cumplir la normativa de solo lectura de la auditoría; ONG → integridad y transparencia |
| **Disparador (Trigger)** | El administrador consulta el historial general de auditoría aplicando filtros |
| **Prioridad / Frecuencia** | Media; frecuencia variable (consultas de control) |
| **Reglas de negocio relacionadas** | RN-08 (auditoría: cada evento registra usuario responsable, fecha/hora y motivo; la consulta es de solo lectura, filtrable y solo para administradores); RN-17 (solo personal autorizado) |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador consultar los logs de auditoría del sistema con filtros
(tipo de evento, nivel, usuario y rango de fechas), obteniendo resultados
paginados en modo de solo lectura.

### 2. PRECONDICIONES
1. El actor debe estar autenticado (Token JWT válido) con rol **Administrador** (**RN-08/RN-17**).
2. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/auditoria?desde=...&hasta=...&nivel=...&tipo=...&usuario=...&page=...` con los filtros opcionales y la paginación.
2. La **Capa de Presentación** (`AuditoriaController`) valida los rangos de fechas y los parámetros de filtro.
3. La **Capa de Negocio** (`AuditoriaService`) aplica los filtros y la paginación sobre el módulo de auditoría (**RN-08**).
4. El Sistema devuelve un código **200 OK** con los resultados paginados.
5. La consulta es de **solo lectura** (**RN-08**); el responsable de cada evento ya quedó registrado al momento del evento.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Rango de fechas inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 2 `desde` es posterior a `hasta` o el formato de fecha es inválido.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2b. Filtro no soportado (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el nivel, el tipo o el identificador de usuario no son reconocidos.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Sin autorización de administración (HTTP 403 Forbidden):**
  1. Si el actor no posee rol de administración (**RN-08/RN-17**).
  2. El Sistema rechaza la petición por autorización.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **3b. Error en la consulta de la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 3 la **Capa de Persistencia** no puede resolver la consulta.
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Consulta sin filtros:** devuelve el historial completo paginado (lista vacía tolerada si no hay eventos).
2. **Facturación de la consulta:** la consulta en sí es auditada (solo lectura, RN-08), permitiendo trazar quién la realizó y cuándo.
3. El actor puede consultar desde el panel web ("Historial general de auditoría y seguridad") o desde un cliente HTTP.

### 6. POSTCONDICIONES
1. El administrador obtiene los logs de auditoría filtrados y paginados.
2. La consulta no modifica ningún registro (modo solo lectura — **RN-08**).
3. El acceso a la auditoría queda registrado.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Resultados de auditoría devueltos (paginados). |
| `400` | Bad Request | Fallo en la validación de filtros o rangos de fechas. |
| `403` | Forbidden | Sin autorización de administración (RN-08/RN-17). |
| `500` | Internal Server Error | Error técnico no controlado en la consulta. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** rangos de fechas y parámetros de filtro.
- **Verificación (Negocio, → 403):** autorización de administración (**RN-08/RN-17**). La consulta es de solo lectura; cada evento fue auditado con usuario responsable, fecha/hora y motivo al momento de producirse (**RN-08**).

### Matriz de trazabilidad CU-17 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `AuditoriaService_Consultar_FiltraYPagina` | `GET /api/auditoria_ConsultaAuditoria_Returns200` |
| 2a. Rango inválido | `400 Bad Request` | — (validación de parámetros) | `GET /api/auditoria_ConRangoInvalido_Returns400` |
| 2b. Filtro no soportado | `400 Bad Request` | — (validación de parámetros) | `GET /api/auditoria_ConFiltroInvalido_Returns400` |
| 3a. Sin autorización | `403 Forbidden` | — (se resuelve en la autorización) | `GET /api/auditoria_SinPermisoAdmin_Returns403` |
| Borde: lista vacía | `200 OK` | `AuditoriaService_ConsultaSinEventos_RetornaListaVacia` | `GET /api/auditoria_SinEventos_Returns200` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).