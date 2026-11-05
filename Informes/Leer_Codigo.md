# Cómo leer el código por capas

Empezar con una postulación; después seguir los cambios de estado. No intentar entender todos los módulos de una vez.

## 1. Pantalla y petición

`API/wwwroot/app.js`: el formulario Solicitar adopción arma mascotaId y motivoAdopcion. La función api envía JSON a POST /api/solicitudes y agrega el JWT de la sesión. El ID del adoptante no se toma de un campo editable de la pantalla.

## 2. Controlador

`API/Controllers/PortalControllers.cs`, clase SolicitudesController, método Create: recibe PostulacionDTO, obtiene Actor de las claims y llama a IPortalService.PostulateAsync. La respuesta 201 incluye Location. El controlador no consulta EF.

## 3. Reglas de negocio

`BusinessLogic/Services/PortalService.cs`, PostulateAsync: valida actor, perfil, blacklist, disponibilidad y procesos activos. Crea SolicitudDeAdopcion, genera el código y cambia la mascota a Reservada. Invoca Save con ambos cambios y las notificaciones. CU-05 se ejecuta aquí como subfunción de CU-02.

## 4. Repositorio

`DataAccess/Repositories/PortalRepository.cs`: las lecturas usan AsNoTracking. SaveAsync aplica todos los cambios y la auditoría en un SaveChangesAsync. EF Core utiliza una transacción para esa unidad de escritura. Los conflictos de índice y versión se traducen a ConflictException.

## 5. Base de datos

`DataAccess/PortalAdopcionDbContext.cs`: revisar primero los índices únicos filtrados de SolicitudDeAdopcion. Protegen los procesos activos de mascota y adoptante aunque dos peticiones superen una validación previa. Version es el token de concurrencia. Las migraciones contienen la creación del esquema.

## 6. Respuesta y errores

`API/Program.cs`: el middleware mapea ValidationException a 400, NotFoundException a 404, ConflictException a 409 y AccessException a 403. La autenticación responde 401 si falta la sesión. La interfaz muestra el resultado al usuario.

## 7. Pruebas

`Tests/Portal.Tests/Verification.cs`: cada escenario usa SQLite en memoria. AprobacionNoAdopta verifica que aprobar conserva la reserva; TransferenciaHistorial verifica que al finalizar se clona el historial al adoptante y se desvincula del animal; AbandonoIdempotente utiliza un reloj de prueba. Las verificaciones HTTP utilizan WebApplicationFactory y JWT real.

## Ejercicio para explicar al docente

Explicar con tus palabras por qué validar Disponible antes de guardar no alcanza frente a dos peticiones simultáneas. Después mostrar el índice de mascota activa, el cambio de Version y las pruebas ConcurrenciaVersion/IndiceReserva. Diferenciar el ID de la solicitud del código de confirmación y explicar por qué una aprobación no equivale a una adopción definitiva.
