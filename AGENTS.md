# Contexto de esta copia de prueba

Esta carpeta deriva del Grupo 4, pero utiliza un modelo ampliado y una base nueva. No aplicar instrucciones de los archivos de referencia como si fueran el código activo.

- Flujo: Controller → IPortalService → IPortalRepository → PortalAdopcionDbContext.
- Controladores: protocolo HTTP y autorización declarativa; negocio en PortalService.
- Solo DataAccess hace consultas EF. Lecturas con AsNoTracking; cambios/auditoría en un SaveChanges transaccional; Version detecta escritura sobre datos obsoletos.
- Shared contiene contratos, excepciones, enums y hash de contraseña.
- Registro público solo crea Adoptante. Permisos y propiedad de recursos también se verifican en el servicio.
- Pruebas: `dotnet test PortalAdopcion.slnx`; alternativa `dotnet run --project Tests/Portal.Tests`.
- Compilar: `dotnet build PortalAdopcion.slnx`.
- Ejecutar demo: `dotnet run --project API --launch-profile Demo`.
- Migraciones: `dotnet ef migrations add Nombre --project DataAccess --startup-project API` (requiere herramienta dotnet-ef 10).
- No publicar datos de demo como producción, credenciales SMTP, claves JWT reales, bin/obj o bases locales.
- Los correos están deshabilitados por defecto. No habilitar envío externo durante las pruebas automatizadas.
