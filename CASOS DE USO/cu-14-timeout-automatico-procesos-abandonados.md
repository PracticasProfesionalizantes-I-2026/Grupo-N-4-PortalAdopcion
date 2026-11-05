# Caso de Uso: Timeout Automático de Procesos Abandonados

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio referenciadas con la numeración **unificada RN-01..RN-17**.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-14 |
| **Nombre** | Timeout Automático de Procesos Abandonados |
| **Actor Principal** | Sistema (job automatizado) |
| **Alcance / Nivel** | Sistema; subfunción / proceso automático |
| **Stakeholders e intereses** | Sistema → liberar procesos inactivos en forma automática; ONG → enfocar recursos en procesos activos; Adoptante → perder la reserva al abandonar el proceso; Administración → auditoría de la corrida |
| **Disparador (Trigger)** | El job diario del Sistema procesa los procesos en estado `En Entrevista` con más de 7 días de inactividad |
| **Prioridad / Frecuencia** | Media; frecuencia diaria (job nocturno, una corrida por día) |
| **Reglas de negocio relacionadas** | RN-10 (proceso en `En Entrevista` sin actividad por más de 7 días → `Abandonada`; procesamiento automático, una vez por día y atómico, sin re-procesar); RN-04 (liberación de la reserva de la mascota); RN-08 (auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite que un job automatizado del Sistema detecte los procesos en estado
`En Entrevista` sin actividad por más de 7 días, los pase a `Abandonada` y libere
la reserva de la mascota.

### 2. PRECONDICIONES
1. El job debe estar configurado para ejecutarse una vez por día (**RN-10**).
2. El sistema debe estar operativo y con la **Capa de Persistencia** accesible.
3. Deben existir procesos en estado `En Entrevista` cuya última actividad supere los 7 días.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Sistema (job diario) selecciona los procesos en estado `En Entrevista` cuya última actividad supera los 7 días (**RN-10**).
2. Para cada proceso candidato, el Sistema verifica que siga activo y que no haya sido procesado en esta corrida (atomicidad, sin re-procesar).
3. El Sistema cambia el estado a `Abandonada` y registra la fecha del timeout.
4. La mascota libera su reserva y vuelve a aparecer como disponible en el catálogo (impacto en visibilidad — **RN-04**).
5. El adoptante pierde la reserva del proceso.
6. El Sistema registra la corrida y cada transición en auditoría (**RN-08**).
7. El Sistema devuelve un código **200 OK** con la cantidad de procesos procesados.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Proceso ya cambiado de estado (HTTP 409 Conflict):**
  1. Si al procesar un proceso candidato su estado ya no es `En Entrevista` (p.ej. fue entrevistado o cancelado durante la corrida).
  2. El Sistema omite ese proceso y continúa con el siguiente (sin re-procesar — **RN-10**).
  3. El Sistema registra el evento y continúa; no se genera respuesta de error global. Fin del caso de uso para ese proceso.

* **3b. Error de persistencia en un proceso (HTTP 500 Internal Server Error):**
  1. Si al actualizar un proceso falla la **Capa de Persistencia**.
  2. El Sistema registra el error y continúa con el siguiente proceso (procesamiento atómico por corrida).
  3. Al finalizar, el Sistema informa la cantidad procesada y los errores como no bloqueantes. Fin del caso de uso.

* **3c. Concurrencia entre corridas (HTTP 409 Conflict):**
  1. Si dos corridas del job se solapan e intentan procesar el mismo proceso.
  2. El Sistema detecta la condición de carrera y omite el proceso ya procesado (atomicidad — **RN-10**).
  3. El Sistema continúa con el siguiente proceso. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Límite de tiempo exacto:** se procesan procesos con más de 7 días de inactividad; el octavo día sin actividad declara el `Abandonada` (borde temporal documentado).
2. El job se ejecuta una vez por día y de forma **atómica** (un solo procesamiento por proceso; no re-procesar).

### 6. POSTCONDICIONES
1. Los procesos con más de 7 días de inactividad quedan en estado `Abandonada` (**RN-10**).
2. Las mascotas asociadas quedan liberadas y vuelven a estar disponibles (**RN-04**).
3. La corrida y sus transiciones quedan registradas en auditoría (**RN-08**).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Confirmación de la corrida del job (cantidad de procesos procesados). |
| `409` | Conflict | Concurrencia entre corridas o proceso ya cambiado de estado (se omite, no re-procesa — RN-10). |
| `500` | Internal Server Error | Error de persistencia no controlado (se registra y continúa). |

### Nota: Validación vs. Verificación aplicada

- El job no recibe entrada del usuario; la **verificación** ocurre en la Capa de Negocio: antigüedad superior a 7 días, estado `En Entrevista` y ausencia de re-procesamiento (**RN-10**), junto con la liberación de la reserva (**RN-04**) y la auditoría (**RN-08**). En caso de conflicto de concurrencia, el proceso en disputa se omite (→ 409 sin abortar la corrida).

### Matriz de trazabilidad CU-14 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `TimeoutService_Entrevista7Dias_Abandona` | `POST /api/jobs/timeout_EjecutaJob_Returns200` |
| 3a. Proceso ya cambiado | `409 Conflict` (omitido) | `TimeoutService_ProcesoYaCambiado_Omitir` | `POST /api/jobs/timeout_ConProcesoYaCambiado_NoReprocesa` |
| 3b. Error de persistencia | `500` (no bloqueante) | `TimeoutService_ConErrorPersistencia_Continua` | `POST /api/jobs/timeout_ConErrorPersistencia_RegistraYContinua` |
| 3c. Concurrencia entre corridas | `409 Conflict` | `TimeoutService_ConCorridaSolapada_OmiteProceso` | `POST /api/jobs/timeout_DobleCorrida_NoDuplica` |
| Borde temporal: 7 días exactos | `200 OK` (no procesa) | `TimeoutService_Con7DiasExactos_NoProcesa` | `POST /api/jobs/timeout_A7DiasExactos_NoAbandona` |

> Regla de oro: cada flujo definido debe tener al menos un test. La traza sigue la convención del proyecto (estilo Informe Técnico); los nombres son **propuestos** hasta implementar el esqueleto .NET de tres capas (`dotnet test <solucion>.slnx`).