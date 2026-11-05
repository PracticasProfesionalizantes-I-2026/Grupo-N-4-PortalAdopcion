# Recorridos para probar desde el navegador

Abrir http://localhost:5104 con la demo en ejecución. Si se desea empezar de cero, detener la API y borrar únicamente `API/portaladopcion-prueba.db` y sus archivos `-wal`/`-shm` si existen. Nunca borrar la base del proyecto original.

## 1. Postulación nueva

Ingresar con adoptante@demo.local. En Catálogo elegir una mascota Disponible y presionar Quiero adoptar. Completar el motivo y confirmar. Ver en Solicitudes el estado Pendiente y el código de confirmación. La mascota debe aparecer Reservada. Intentar postular nuevamente: debe rechazarse por proceso activo. Cancelar con motivo debe liberar la reserva.

## 2. Evaluar sin esperar días reales

Ingresar con admin@demo.local. En Solicitudes seleccionar el proceso Pendiente de Tom del adoptante demo 2: fue creado hace 40 días como dato sintético. Aprobar e indicar entidad autorizante ONG de prueba. Debe quedar Aprobada y la mascota seguir Reservada. Con una solicitud recién creada, el mismo intento debe devolver conflicto por menos de 15 días.

Programar entrevista para un minuto después, asignando al Voluntario demo. Esperar la fecha o usar las pruebas automatizadas con reloj controlado; ingresar como voluntario, registrar resultado Favorable y observaciones. Debe empezar EnPeriodoDePrueba. La demo no altera el reloj del sistema ni permite cambiar fechas de solicitud desde un endpoint.

## 3. Seguimiento y cierre

Ingresar como Voluntario demo. El proceso de Mora del adoptante demo 3 ya lleva 16 días de prueba y tiene historial. Registrar seguimiento. Un segundo registro el mismo día debe fallar. Si marcar Alerta, intentar finalizar: debe bloquearse. Ingresar como veterinario y registrar validación con motivo. Volver como voluntario y finalizar con motivo. Debe quedar Adoptada y la mascota también.

Ingresar con adoptante3@demo.local y consultar el historial de Mora. Antes de finalizar no puede verlo; después sí. Se conserva en la ONG y se habilita al nuevo adoptante.

## 4. Cancelación en prueba y blacklist

Hacer este recorrido sobre una base demo recién generada, antes de finalizar Mora. Cancelar el proceso en prueba con motivo. Como administrador, verificar que existe una sanción de un año. El adoptante sancionado puede entrar y consultar, pero no postular. Revocar la sanción con motivo desde Administración permite volver a postular si la cuenta y mascota cumplen las condiciones.

## 5. Historia clínica

Como veterinario, crear el historial de una mascota que no lo tenga: peso, estado general, vacunas y antecedentes (usar Ninguno si corresponde). Un segundo historial debe fallar. Agregar evento médico, consultar historial y comprobar fecha/responsable. El voluntario no tiene permiso de crear historia médica.

## 6. Incidentes y auditoría

Como voluntario asignado a un proceso en prueba, registrar un incidente Urgente. Como administrador, verificar la notificación, el incidente prioritario y su evento de auditoría. Consultar por fechas y acción. Exportar reportes CSV y PDF de solicitudes o mascotas con un rango que incluya los datos demo.

## 7. Usuarios y permisos

Como administrador registrar personal; para Veterinario incluir matrícula única. Cambiar rol/estado de un integrante invalida su token anterior. No se puede dar de baja al último administrador activo. Como adoptante intentar consultar usuarios administrativos mediante Scalar: debe devolver 403. Sin sesión: 401.

## 8. Abandono automático

El botón administrativo ejecuta el mismo control del job, pero no envejece los procesos. Si todavía no transcurrieron más de 7 días, el resultado correcto es cero. La prueba AbandonoIdempotente verifica el límite con reloj controlado, libera la reserva y asegura que una segunda ejecución no repite el cambio.

## Pruebas repetibles

```powershell
dotnet test PortalAdopcion.slnx
# Alternativa que imprime cada escenario:
dotnet run --project Tests/Portal.Tests
```

Cada prueba usa SQLite en memoria y no modifica la base de la demo ni envía correo real.
