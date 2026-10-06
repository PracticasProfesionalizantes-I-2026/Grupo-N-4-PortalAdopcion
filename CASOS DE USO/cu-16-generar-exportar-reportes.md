# Caso de Uso: Generar y Exportar Reportes y Estadísticas

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-16 |
| **Nombre** | Generar y Exportar Reportes y Estadísticas |
| **Actor Principal** | Administrador del Sistema |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Administración → obtener métricas del portal (adopciones, postulaciones, mascotas); ONG → tomar decisiones basadas en datos; Sistema → auditar la consulta de reportes |
| **Disparador (Trigger)** | El administrador solicita un reporte o estadística del sistema con un rango de fechas |
| **Prioridad / Frecuencia** | Media; frecuencia variable (consultas periódicas) |
| **Reglas de negocio relacionadas** | RN-17 (solo personal autorizado accede y genera reportes); RN-08 (auditoría de la consulta); reporte vacío tolerado |

---

### 1. BREVE DESCRIPCIÓN
Permite al administrador generar y exportar reportes y estadísticas de la
organización (p.ej. adopciones, postulaciones, catálogo) en formato CSV o PDF,
con un rango de fechas definido.

### 2. PRECONDICIONES
1. El actor debe estar autenticado (Token JWT válido) con rol **Administrador** (**RN-17**).
2. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.
3. Debe existir información registrada en el período consultado (un reporte vacío es tolerado).

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `GET /api/reportes?tipo=...&desde=...&hasta=...&formato=csv|pdf` con el tipo de reporte, el rango de fechas y el formato solicitado.
2. La **Capa de Presentación** (`ReportesController`) valida los parámetros (tipo válido, rango consistente y formato admitido).
3. La **Capa de Negocio** (`ReporteService`) verifica la autorización (**RN-17**) y genera el contenido del reporte a partir de la **Capa de Persistencia**.
4. El Sistema devuelve el archivo (CSV o PDF) con un código **200 OK**; si no hay datos en el rango, devuelve un reporte vacío válido.
5. El Sistema registra la consulta en auditoría (**RN-08**).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Rango de fechas inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 2 `desde` es posterior a `hasta`, el formato es inválido o ambos faltan.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2b. Tipo o formato no admitido (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el tipo de reporte o el formato (`csv`/`pdf`) no es reconocido.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Sin autorización de administración (HTTP 403 Forbidden):**
  1. Si el actor no posee rol de administración (**RN-17**).
  2. El Sistema rechaza la petición por autorización.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **3b. Error en la generación del archivo (HTTP 500 Internal Server Error):**
  1. Si en el Paso 3 la generación del CSV/PDF falla (p.ej. servicio de exportación no disponible).
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Formato de salida:** CSV o PDF, seleccionado por el parámetro `formato`; el contenido del reporte es el mismo en ambos.
2. **Reporte vacío:** cuando no existen datos en el período, se devuelve un archivo válido sin filas (no es un error).
3. El actor puede invocar el endpoint desde el panel web o desde un cliente HTTP; el resultado (archivo) es idéntico.

### 6. POSTCONDICIONES
1. El archivo de reporte (CSV/PDF) queda generado y descargable para el administrador.
2. La consulta del reporte queda registrada en auditoría (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Reporte/estadística generado y descargable (CSV/PDF). |
| `400` | Bad Request | Fallo en la validación de parámetros (rango, tipo o formato). |
| `403` | Forbidden | Sin autorización de administración (RN-17). |
| `500` | Internal Server Error | Error técnico no controlado en la generación del archivo. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** parámetros de la query (tipo, rango de fechas y formato).
- **Verificación (Negocio, → 403):** autorización de administración (**RN-17**). La generación del archivo ocurre en la Capa de Negocio; la auditoría (**RN-08**) se registra al completar la consulta.

### Matriz de trazabilidad CU-16 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `ReporteService_GenerarReporte_RetornaArchivo` | `GET /api/reportes_GeneraReporte_Returns200` |
| 2a. Rango inválido | `400 Bad Request` | — (validación de parámetros) | `GET /api/reportes_ConRangoInvalido_Returns400` |
| 2b. Tipo/formato no admitido | `400 Bad Request` | — (validación de parámetros) | `GET /api/reportes_ConFormatoInvalido_Returns400` |
| 3a. Sin autorización | `403 Forbidden` | — (se resuelve en la autorización) | `GET /api/reportes_SinPermisoAdmin_Returns403` |
| 3b. Error de generación | `500 Internal Server Error` | `ReporteService_ConErrorGeneracion_LanzaExcepcion` | `GET /api/reportes_ConErrorGeneracion_Returns500` |
| Borde: reporte vacío | `200 OK` | `ReporteService_ConReporteVacio_RetornaArchivoVacio` | `GET /api/reportes_SinDatos_Returns200` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).