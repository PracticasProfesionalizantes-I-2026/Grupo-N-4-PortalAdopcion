# Informe de diferencias, cambios y justificación

## Portal de Adopción de Mascotas para ONGs — Grupo N°4 (Giordano Catalina, Ledesma Francisco)
## Fecha: 04/09/2026

# 1. Resumen

Se compararon los tres artefactos del repositorio:
- Documento de requerimientos y negocio: BUSINESS/Portal de adopcion para mascotas (4).docx
- Mockups: BUSINESS/MOCKUPS.excalidraw
- Casos de uso: carpetas CASOS DE USO y Documentos EJEMPLO

Del análisis surgieron tres tipos de hallazgos:
1. Funcionalidades pedidas en el proyecto que no tenían caso de uso ni (en algunos casos) mockup.
2. Pantallas de los mockups que no tenían respaldo en el proyecto ni en casos de uso.
3. Inconsistencias internas entre los casos de uso existentes y el modelo de dominio.

Como resultado se generaron 13 casos de uso nuevos (CU-06 a CU-18) y un documento de BUSINESS
con el modelo de estados del workflow y las reglas de negocio unificadas (RN-01 a RN-17).
Todo cambio se documenta en este informe con su justificación.

# 2. Diferencias detectadas

## 2.1 Funcionalidades del proyecto sin mockup y sin caso de uso

- Transferencia/clonación del historial clínico al perfil del adoptante al finalizar la adopción.
- Automatización de timeout: procesos en estado "Entrevista" sin actividad por más de 7 días pasan a "Abandonado".
- Cancelación de un proceso de adopción registrando motivo y usuario responsable.
- Registro de usuarios, autenticación y gestión de roles y permisos (administrador, voluntario, veterinario, adoptante).
- Registro y actualización del catálogo de mascotas (dar de alta, baja, cambio de estado).
- Registro de eventos clínicos posteriores y administración de fichas médicas (veterinario).
- Auditoría y trazabilidad de eventos y cambios de estado.
- Reportes básicos, estadísticas y exportación CSV/PDF.

## 2.2 Mockups sin respaldo en el proyecto

- Reporte de incidentes (formulario y "notificación urgente"): presente en mockups, no está en los requerimientos funcionales.
- Seguimiento diario del voluntario (alimentación, ánimo, tareas, medicación, limpieza): no es un requisito funcional explícito.
- Asignación de tareas a voluntarios con máximo 2 tareas activas.
- Seguimiento del periodo de prueba (mockup "Día x/15") con alertas al veterinario.
- Agenda de entrevistas del día (videollamada, llamada telefónica, reprogramar).
- Código de invitación / clave de la ONG al crear usuarios de personal.
- Envío de correos electrónicos (notificación de sanción, recordatorio de pendientes, notificación urgente): contradice el alcance del proyecto que excluye notificaciones automáticas.

## 2.3 Inconsistencias internas detectadas

- Reglas de negocio duplicadas con numeración distinta: CU-02 usa RN-03/RN-04 y CU-05 usa RN-18.2/RN-18.3 para las mismas reglas. Además RN-18.1 y RN-18.5 no tienen par en los casos de uso anteriores.
- Ambigüedad entre "Solicitud" y "Proceso": CU-02 crea la solicitud y CU-05 crea el proceso con el mismo estado inicial (Pendiente) y las mismas validaciones. En el proyecto y en los mockups es una sola cosa con un único ciclo de vida.
- Estado "En Observación" (CU-04) presente en el caso de uso pero sin respaldo en mockups ni en el requerimiento.
- Estado "Abandonado" (timeout) presente en el proyecto y en los mockups pero sin ningún caso de uso.
- El mockup indica que un usuario en blacklist no puede iniciar sesión; el requerimiento solo prohíbe postularse. Se resuelve permitiendo el ingreso (con restricción para postular) para que el usuario vea su estado.
- El periodo de prueba aparece en el requerimiento sin duración; los mockups lo muestran como "Día x/15". Se fija en 15 días.
- CU-03 contiene una precondición contradictoria: "la ficha clínica debe existir" cuando el propio caso de uso es el que crea el historial clínico.

# 3. Cambios realizados y justificación

## 3.1 Nuevo documento: BUSINESS/Modelo-de-Estados-y-Reglas-de-Negocio.md

Es la referencia única de dominio para todos los casos de uso. Contiene:
- Modelo unificado: Solicitud de Adopción = Proceso de Adopción (una sola entidad con un único ciclo de vida).
- Tabla de estados de la entidad, de la mascota y de la cuenta de usuario.
- Tabla de transiciones válidas con disparador, actor y regla asociada.
- Catálogo unificado de reglas de negocio RN-01 a RN-17 con la correspondencia con la numeración anterior (RN-01..RN-06 y RN-18.1..18.5).
- Tabla de contradicciones detectadas y decisiones tomadas.
- Definición de notificaciones dentro del alcance MVP.
- Requerimientos NO funcionales relevantes para los casos de uso nuevos.

Por qué: el documento de requerimientos pide un workflow con "validación de transiciones de estado" y un diagrama de flujo, pero ningún artefacto lo definía. Sin un modelo único de estados y reglas, los casos de uso se superponen (como ocurrió con CU-02/CU-05) y la implementación sería inconsistente.

## 3.2 Casos de uso nuevos (CASOS DE USO/)

Todos siguen la plantilla de GUIA-Especificacion-Casos-de-Uso.md (precondiciones, flujo principal con código HTTP, flujos alternativos, postcondiciones, notas de validación/verificación y matriz de trazabilidad a tests).

- CU-06 Registrar Usuario en el Sistema: requerimiento FR de registro de usuarios y roles; mockups de creación de usuario para los 4 roles y clave de invitación.
- CU-07 Iniciar Sesión: requerimiento FR de autenticación; mockup de login. Aclaración: la cuenta suspendida ingresa pero no puede postular (decisión documentada).
- CU-08 Gestionar Usuarios y Roles: requerimiento FR de roles y permisos; mockup "Gestión de usuarios y roles del sistema".
- CU-09 Gestionar Catálogo de Mascotas: requerimiento FR de registro/actualización de mascotas; mockups de alta, edición, cambio de estado y baja.
- CU-10 Registrar Evento Médico en el Historial Clínico: requerimiento FR de registro de eventos clínicos; complementa a CU-03 (que solo crea el historial). Mockup "Registrar Evento Médico".
- CU-11 Programar y Realizar Entrevista: transición del workflow a "En Entrevista"; mockup "Agenda: Entrevistas del día".
- CU-12 Realizar Seguimiento del Periodo de Prueba: mockup "Seguimiento Diario" y "Monitoreo: Seguimiento de Prueba"; formaliza el periodo de prueba de 15 días.
- CU-13 Cancelar Proceso de Adopción: requerimiento FR de cancelación con motivo; aplica la regla de bloqueo de 1 año al cancelar en periodo de prueba sin justificación.
- CU-14 Generar Timeout Automático de Procesos Abandonados: requiere el FR de automatización (7 días) y cumple el NFR de ejecución diaria confiable. Es un flujo del Sistema (job).
- CU-15 Finalizar Adopción y Transferir Historial Clínico: cubre el FR de transferencia/clonación del historial al adoptante, sin mockup previo.
- CU-16 Generar y Exportar Reportes y Estadísticas: FR de reportes, estadísticas y exportación CSV/PDF; mockups de generación de reportes y métricas.
- CU-17 Consultar Auditoría (Logs): FR de auditoría y trazabilidad; mockup "Historial general de auditoría y seguridad".
- CU-18 Reportar Incidente: nace de los mockups; se especifica dentro del alcance MVP (evento de máxima prioridad + auditoría, sin correo automático). Pendiente de confirmar si el cliente quiere esta funcionalidad.

Por qué estos casos de uso: el proyecto describe estas funcionalidades como necesarias (viabilidad de negocio y requerimientos funcionales) o están presentes en los mockups ya acordados; sin su especificación, la implementación y los tests no tendrían contrato.

## 3.3 Decisiones de diseño tomadas

- Solicitud y Proceso se modelan como una sola entidad (SolicitudDeAdopcion). CU-05 pasa a interpretarse como la subfunción automática de CU-02.
- Todo proceso inicia en estado "Pendiente" (RN-07).
- Periodo de prueba fijado en 15 días por defecto.
- Cuenta con blacklist: permite ingresar a la sesión pero no postular (RN-01).
- Notificaciones por correo fuera de alcance; en su lugar, notificaciones dentro del portal y registro en auditoría.
- Incidente urgente = evento de máxima prioridad visible para administración (sin envío de correo).
- Las reglas de negocio se renumeran de forma unívoca (RN-01 a RN-17) con tabla de correspondencia con la numeración previa, para mantener trazabilidad con CU-01..CU-05.

# 4. Pendientes recomendados (no modificados en esta entrega)

- Fusionar CU-05 dentro de CU-02 como subvariación automatizada para eliminar duplicación.
- Actualizar CU-01 a CU-05 para referenciar la numeración unificada RN-01..RN-17.
- Agregar el diagrama de flujo de transiciones de estado que exige el documento fuente ("incluir diagrama de flow").
- Confirmar formalmente el alcance de la asignación de tareas a voluntarios (RN-16) y de las notificaciones.
- Definir el contrato de "dar de baja" de usuarios y el flujo de "¿Olvidó su contraseña?" del mockup (no cubierto).
- Corregir la precondición de CU-03 ("la ficha clínica debe existir") para evitar contradicción con la creación del historial.

# 5. Archivos creados o modificados

- BUSINESS/Modelo-de-Estados-y-Reglas-de-Negocio.md (nuevo)
- CASOS DE USO/cu-06-registrar-usuario.md (nuevo)
- CASOS DE USO/cu-07-iniciar-sesion.md (nuevo)
- CASOS DE USO/cu-08-gestionar-usuarios-y-roles.md (nuevo)
- CASOS DE USO/cu-09-gestionar-catalogo-de-mascotas.md (nuevo)
- CASOS DE USO/cu-10-registrar-evento-medico.md (nuevo)
- CASOS DE USO/cu-11-programar-realizar-entrevista.md (nuevo)
- CASOS DE USO/cu-12-seguimiento-periodo-de-prueba.md (nuevo)
- CASOS DE USO/cu-13-cancelar-proceso-de-adopcion.md (nuevo)
- CASOS DE USO/cu-14-timeout-automatico-procesos-abandonados.md (nuevo)
- CASOS DE USO/cu-15-finalizar-adopcion-y-transferir-historial.md (nuevo)
- CASOS DE USO/cu-16-generar-exportar-reportes.md (nuevo)
- CASOS DE USO/cu-17-consultar-auditoria.md (nuevo)
- CASOS DE USO/cu-18-reportar-incidente.md (nuevo)
- COSAS/cosas leer.docx (nuevo: este informe)