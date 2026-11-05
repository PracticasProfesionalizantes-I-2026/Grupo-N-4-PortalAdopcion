# Implementación y mejoras de esta copia

## Código que se agregó

Se ampliaron las tres entidades originales y se incorporaron Usuario, SancionBlacklist, HistorialClinico, EventoMedico, Entrevista, Seguimiento, Incidente, EventoAuditoria y Notificacion. La migración se encuentra en `DataAccess/Migrations/`; el código anterior queda en `referencia/`.

### Identidad y permisos

Registro público de Adoptante y alta administrativa de personal. Contraseñas PBKDF2-SHA256 con sal aleatoria y 210.000 iteraciones. JWT firmado, con emisor, audiencia, vencimiento de dos horas y validación de estado/version de cuenta en cada petición. Cambiar rol/estado invalida los tokens anteriores. El registro público rechaza un campo rol agregado al JSON. No se expone el hash en respuestas de usuario. Se conserva al menos un administrador activo.

La matrícula veterinaria es única. Las cuentas Adoptante y personal se mantienen separadas. Email y DNI se normalizan antes de consultar duplicados. No se permite cambiar la identidad del perfil en esta prueba; el resto de los datos sí se puede editar. La edad mínima de 18 años es una decisión de esta copia que el grupo debe acordar con el docente.

### Postulación y proceso

Estados activos: Pendiente, EnObservacion, Aprobada, EnEntrevista, EnPeriodoDePrueba. Una mascota y un adoptante tienen como máximo un proceso activo, con índices únicos filtrados. La reserva y la solicitud se guardan atómicamente. CU-05 es interno a CU-02; no existe una segunda entidad de proceso.

La postulación requiere perfil completo, cuenta habilitada y ausencia de sanción vigente (un perfil en lista negra recibe 403). Se genera un código de confirmación y notificaciones internas. Aprobar exige 15 días, entidad autorizante y responsable; no adopta todavía. La entrevista favorable inicia prueba. La prueba dura al menos 15 días y una alerta requiere validación veterinaria. Finalizar exige historial clínico y ejecuta el traspaso clínico al adoptante (ver más abajo).

La cancelación durante prueba genera una sanción de un año que bloquea (403) nuevas postulaciones. Agregar una sanción vigente rechaza procesos activos y libera su mascota. Abandono: más de siete días sin actividad en entrevista; el job ejecuta al iniciar y después cada 24 horas. El botón de ejecución manual solo existe en Development.

### Traspaso clínico al finalizar (Nivel 4 del catálogo)

Al finalizar la adopción, el servicio clona el historial clínico y sus eventos médicos en un nuevo registro asociado permanentemente al perfil del adoptante (`AdoptanteId`), desvinculado del animal (`MascotaId` nulo) y conservando la trazabilidad (`OrigenHistorialId`/`OrigenMascotaId`). El registro original queda sin vínculo al animal. El adoptante consulta el historial transferido por la mascota; el historial original no se destruye.

### Salud, seguimiento y control

Historial único por mascota, creado junto con el evento inicial; eventos posteriores con fecha y responsable. Una entrada de seguimiento por proceso por día UTC; un voluntario puede estar asignado a dos procesos activos como máximo. Un incidente urgente se ordena con prioridad y notifica a administración. Auditoría de operaciones de cambio y reportes; consulta solo administrativa. Reportes de solicitudes, adopciones y mascotas por rango, CSV o PDF; CSV neutraliza celdas que podrían interpretarse como fórmulas.

### Interfaz

Pantallas de catálogo, solicitudes, perfil, salud, administración y seguimiento/control. Los botones y formularios visibles dependen del rol. Las reglas también se verifican en el backend: ocultar un botón no es la protección. Las respuestas se muestran usando nodos de texto para evitar interpretar datos como HTML. El token se conserva únicamente en sessionStorage de la pestaña.

### Respuestas mediante DTOs

La API no devuelve entidades de base de datos. Cada respuesta se arma con un DTO de `Shared/DTOs/PortalResponses.cs`, mapeado desde la entidad en `BusinessLogic/Mapping/PortalMapper.cs`, de modo que la capa de presentación no depende de EF ni expone campos de navegación ni de auditoría interna. El inicio de sesión devuelve `SesionResponseDTO` (token, vencimiento y usuario) en lugar de un objeto anónimo. Los nombres de campo JSON se conservan para no romper la interfaz existente.

## Pruebas automáticas

Dos conjuntos sobre xUnit. El de integración usa la base SQLite del servidor real; el segundo es de reglas de negocio puras, con `Mock<IPortalRepository>` (Moq) y un reloj de prueba, sin base de datos, con patrón Arrange/Act/Assert explícito. Cubren autenticación, permisos y propiedad de recursos, contratos y transiciones, reglas de negocio, bloqueo 403 por sanción y el traspaso clínico al finalizar. Se ejecutan con `dotnet test PortalAdopcion.slnx` (63/63 al cierre de esta revisión).

## Envío de correo opcional

Por defecto solo se registra `CorreoBccPendiente` en Notificacion. `CorreoJob` está implementado pero no envía nada sin `Smtp:Enabled=true` y datos válidos. Para ensayar, utilizar un servidor local de captura de correo; para producción configurar un proveedor.

Variables de entorno requeridas si se habilita SMTP: `Smtp__Enabled`, `Smtp__Host`, `Smtp__Port`, `Smtp__From`, `Smtp__Username`, `Smtp__Password`, `Smtp__EnableSsl`. La confirmación se envía en BCC al adoptante; el remitente es el destinatario visible. No incluir credenciales reales en appsettings ni en Git. El envío y marcado de entrega son de tipo al menos una vez: un fallo entre ambos puede repetir el correo; una solución productiva debe agregar idempotencia de envío y control de reintentos.

## Simplificaciones que deben acordarse antes de entregar

- Esta es una versión de prueba con un servicio de aplicación general. Conviene dividir PortalService por módulo si el grupo sigue ampliándolo; el flujo por capas ya está separado.
- El límite de RN-16 se interpreta como dos procesos de seguimiento activos por voluntario. No se implementó un planificador independiente con múltiples tareas por proceso.
- El día de seguimiento se determina en UTC. Antes de usarlo en Argentina, acordar calendario local y zona horaria.
- La transferencia del historial clona los registros médicos y los asocia al adoptante, desvinculándolos del animal (Nivel 4 del catálogo). El registro original no se borra. No hay archivos adjuntos ni transferencia externa.
- Las fotos se guardan como URL HTTPS; no hay carga de archivos ni almacenamiento de imágenes propio. Chip es una referencia opcional única.
- No incluye recuperación de contraseña, MFA, refresh tokens, revocación individual de sesiones, paginación ni búsqueda optimizada; son mejoras para una versión productiva, no simulaciones de funcionalidades presentes.
- Se valida formato de datos, pero no se comprueba identidad/DNI contra un organismo externo.
- La cuenta suspendida de Adoptante puede iniciar sesión y consultar; no puede postular. Personal suspendido no puede iniciar sesión ni operar.
- Los datos y claves de Development son públicos y solo sirven en local. En Production se requiere `Jwt__Key` propia (al menos 32 bytes), SQLite durable, HTTPS, backups y configuración de correo si corresponde. La opción de seed no se aplica fuera de Development.
- No hay migración incremental desde la base original: se usa una base nueva, vacía, de prueba.
- La concurrencia de reservas se refuerza con índices y versión; el límite de dos asignaciones de voluntario tiene verificación de servicio y no una restricción independiente de base. Requiere refuerzo si varias instancias asignan simultáneamente.

## Trabajo que no se realizó en GitHub

No se crearon ramas, PR, asignaciones ni aprobaciones remotas. El reparto y los textos en propuestas-pr son materiales para que el grupo aplique la consigna después de leer y aceptar esta implementación. Ninguna revisión cruzada fue simulada.
