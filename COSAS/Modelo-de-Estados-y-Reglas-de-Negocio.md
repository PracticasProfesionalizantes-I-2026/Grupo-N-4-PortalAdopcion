# Modelo de Estados y Reglas de Negocio — Portal de Adopción

> **Regenerado el 10/09/2026** desde la autoría original (el `.md` se perdió del disco). Contenido consolidado: máquina de estados del workflow, estados de mascota y cuenta, catálogo unificado de reglas de negocio RN-01..RN-17 y decisiones de diseño.

---

## 1. Modelo de estados del proceso de adopción

Decisión de diseño: **"Solicitud" y "Proceso" son la misma entidad** (`SolicitudDeAdopcion`). Todo proceso inicia en `Pendiente`.

### Estados del proceso

| Estado | Significado | Tipo |
| --- | --- | --- |
| `Pendiente` | Postulación registrada, aguarda evaluación ("Pendiente de Evaluación"). | Inicial |
| `En Observación` | Evaluación en curso con información insuficiente; sigue siendo evaluable. | Intermedio |
| `Aprobada` | Evaluación favorable; habilita la entrevista. | Intermedio |
| `En Entrevista` | Entrevista programada/realizándose con el adoptante. | Intermedio |
| `En Periodo de Prueba` | Adopción condicional con seguimiento (15 días por defecto). | Intermedio |
| `Adoptada` | Adopción concluida; se transfiere el historial clínico. | Terminal |
| `Rechazada` | Evaluación o entrevista rechazada (se registra motivo). | Terminal |
| `Cancelada` | Cancelación por el adoptante o por el personal (con motivo). | Terminal |
| `Abandonada` | Timeout automático: sin actividad >7 días en "En Entrevista". | Terminal |

### Transiciones válidas

| Desde | Hasta | Disparador | Actor | Regla |
| --- | --- | --- | --- | --- |
| — | `Pendiente` | CU-02 · confirmación de postulación | Adoptante / Sistema | RN-03, RN-04, RN-07, RN-14 |
| `Pendiente` | `En Observación` | CU-04 · evaluación con información insuficiente | Personal de Adopciones | RN-05 |
| `Pendiente` | `Aprobada` | CU-04 · evaluación favorable | Personal de Adopciones | RN-05, RN-06 |
| `Pendiente` | `Rechazada` | CU-04 · evaluación desfavorable (motivo) | Personal de Adopciones | RN-05, RN-06 |
| `Pendiente` | `Cancelada` | CU-04 · la mascota dejó de estar disponible | Sistema | RN-09 (**) |
| `En Observación` | `Aprobada` / `Rechazada` | CU-04 · reevaluación | Personal de Adopciones | RN-05, RN-06 |
| `Aprobada` | `En Entrevista` | CU-11 · entrevista programada/realizada | Personal de Adopciones | RN-08 |
| `En Entrevista` | `En Periodo de Prueba` | CU-11 · entrevista favorable | Personal de Adopciones | RN-08 |
| `En Entrevista` | `Rechazada` | CU-11 · entrevista desfavorable (motivo) | Personal de Adopciones | RN-06 |
| `En Entrevista` | `Abandonada` | CU-14 · timeout 7 días sin actividad | Sistema (job) | RN-10 |
| `En Periodo de Prueba` | `Adoptada` | CU-15 · finalización exitosa | Personal de Adopciones | RN-13 |
| `En Periodo de Prueba` | `Cancelada` | CU-13 · cancelación con motivo | Adoptante / Personal | RN-11, RN-12 |
| Cualquier activo | `Rechazada` | CU-04 · adoptante entra a blacklist durante la evaluación | Sistema | RN-01 |

**(*) Nota:** RN-09 (mascota no disponible): cuando la mascota es reservada por otro proceso o deja de estar disponible, la solicitud no puede continuar.

---

## 2. Estados de la mascota

| Estado | Significado |
| --- | --- |
| `Disponible` | Visible para postularse. Estado inicial por defecto al registrar. |
| `Reservada` | Tiene una solicitud activa; deja de aparecer como disponible (RN-04). |
| `En Entrevista` | El proceso asociado está en entrevista. |
| `En Periodo de Prueba` | El proceso asociado está en seguimiento de prueba. |
| `Adoptada` | Adopción concluida; queda con su historial archivado/transferido. |
| `En Tratamiento` | Salud: no apta para adopción temporalmente. |
| `Inactiva / Baja` | Dada de baja administrativamente (no elimina el historial). |

---

## 3. Estados de la cuenta

| Estado de cuenta | Poder postular | Iniciar sesión | Gestionar | Comentario |
| --- | --- | --- | --- | --- |
| `Activa` | Sí | Sí | Sí | Estado normal. |
| `Suspendida` (blacklist) | **No** | Sí | No | Puede ingresar para ver sus solicitudes, pero no postular (RN-01). |
| `Dada de baja` | No | No | No | Cuenta desactivada administrativamente (CU-08). |
| `Baja definitiva` (soft) | No | No | No | No se elimina el historial (trazabilidad RN-08). |

---

## 4. Reglas de negocio unificadas (RN-01..RN-17)

### 4.1 Catálogo

| ID | Regla |
| --- | --- |
| RN-01 | Blacklist: un usuario en blacklist no puede iniciar un nuevo proceso de adopción; la sanción se aplica automáticamente si cancela durante el periodo de prueba (1 año). Puede iniciar sesión, pero no postular. |
| RN-02 | Las sanciones se registran con motivo, fecha y duración; son consultables y editables solo por personal autorizado; cada cambio queda auditado (RN-08). |
| RN-03 | Al confirmar una postulación se envía un correo de confirmación "en copia oculta"; el formulario incluye: correo electrónico, teléfono, dirección, estado civil, edad, motivo por el que quiere adoptar y datos laborales. |
| RN-04 | Una mascota puede tener una única postulación activa. Al evaluarse/entrevistarse ya con una mascota, el adoptante no puede volver a aplicar a la misma mascota. Al postularse, la mascota pasa a "Reservada" y deja de aparecer como disponible. |
| RN-05 | Para aprobar una solicitud deben pasar mínimo 15 días desde el inicio del proceso. Un rechazo exige motivo obligatorio. Se puede dejar "En Observación" con información insuficiente. |
| RN-06 | Toda autorización registra la entidad autorizante y la fecha. Toda decisión (evaluación, entrevista, cancelación, baja) se asocia a un evento de auditoría. |
| RN-07 | El sistema genera un código de confirmación en la postulación. El correo de confirmación se envía en copia oculta (BCC). |
| RN-08 | Auditoría: cada evento y cambio de estado registra usuario responsable, fecha/hora y motivo. La consulta de auditoría es solo lectura, filtrable y solo para administradores. |
| RN-09 | Mascota reservada / no disponible: una solicitud sobre una mascota reservada o dada de baja no puede continuar (mascotaId no admite procesos inactivos). |
| RN-10 | Timeout: un proceso en "En Entrevista" sin actividad durante más de 7 días pasa a "Abandonada" (procesamiento automático, un por día). El adoptante pierde la reserva. |
| RN-11 | La cancelación de un proceso registra motivo y usuario responsable. El adoptante mantiene su cuenta pero puede quedar sancionado según RN-12. |
| RN-12 | Cancelación durante el periodo de prueba: se registra motivo y se genera sanción de 1 año de blacklist (no puede volver a postular). |
| RN-13 | Periodo de prueba: duración por defecto 15 días. Si el seguimiento está en "Alerta", no se puede finalizar la adopción sin validación del veterinario. |
| RN-14 | La postulación requiere que el adoptante haya iniciado sesión con datos correctos (validación contra el perfil). |
| RN-15 | La sanción queda ligada al perfil del adoptante (dni/email) y a su historial; no se puede eludir creando una cuenta nueva. |
| RN-16 | Seguimiento diario: el voluntario asociado registra una entrada por día (ánimo, alimentación, tareas, medicación, limpieza). Solo 2 tareas activas como máximo por voluntario. Si el estado está en alerta, se notifica al veterinario. Pendiente de confirmación de alcance. |
| RN-17 | Unicidad de email, DNI y matrícula del veterinario; contraseña segura (longitud mínima + complejidad). Solo personal autorizado registra usuarios y mascotas y accede a reportes. |

### 4.2 Correspondencia con las RN antiguas

| RN antigua | RN unificada | Nota |
| --- | --- | --- |
| RN-01 (CU-01 Blacklist) | RN-01, RN-02 | — |
| RN-02 (CU-01 modificar/eliminar sanción) | RN-02, RN-15 | — |
| RN-03 (CU-02 confirmación) | RN-03, RN-07 | — |
| RN-04 (CU-02 unicidad activa) | RN-04 | — |
| RN-05 (CU-04 aprobación) | RN-05, RN-06 | — |
| RN-06 (CU-04 registro tiempo/autorización) | RN-06 | — |
| RN-18.1 (código de confirmación) | RN-07 | — |
| RN-18.2 (mascota reservada/no disponible) | RN-09 | — |
| RN-18.3 (confirmación en copia oculta) | RN-03 / RN-07 | — |
| RN-18.4 (auditoría) | RN-08 | — |
| RN-18.5 (timeout 7 días) | RN-10 | — |

---

## 5. Decisiones de diseño (contradicciones resueltas)

| Contradicción / vacío | Decisión | Justificación |
| --- | --- | --- |
| ¿"Solicitud" o "Proceso"? | Entidad única `SolicitudDeAdopcion` | Evita duplicar lógica y endpoints; CU-05 es subfunción de CU-02. |
| Blacklist: ¿se puede iniciar sesión? | Sí, con restricción para postular | Mockup y spec diferían; se prioriza que el usuario pueda ver su estado. |
| Periodo de prueba: ¿duración? | 15 días por defecto | Requerimiento no especificaba duración. |
| Notificaciones: ¿correos automáticos? | No en el MVP; notificaciones in-app + auditoría | Spec declara "no se enviarán correos"; los mockups son solo sugerencias de UI. |
| Estado "En Observación" | Modelado como estado intermedio evaluable | Solo lo mencionaba CU-04; ahora tiene transiciones y tests. |
| CU-03 precondición contradictoria | Pendiente de corrección (ver `Pendientes-Alta-Prioridad.md` P4) | — |

---

## 6. Requerimientos no funcionales (referencia)

- Consulta/lectura de catálogo y proceso en menos de 2 segundos.
- Las decisiones de evaluación/entrevista quedan auditadas con su responsable.
- El job de timeout debe ejecutarse una vez por día y de forma atómica (sin re-procesar).
- Se recomienda: expiración de JWT, hashing/criptografía de contraseña, y validación server-side de toda entrada.