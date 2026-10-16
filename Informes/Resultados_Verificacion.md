# Resultados de verificación

Fecha de revisión: 1 de octubre de 2026. SDK utilizado: .NET 10.0.401; runtime 10.0.12.

## Repositorio original

- Rama main, commit 708456a2252eeb1077967bce3ac80895fc107467.
- Compilación: aprobada, 0 errores y 0 advertencias.
- Pruebas de negocio: 37 aprobadas.
- Pruebas de integración: 36 aprobadas.
- Total: **73/73 aprobadas**. Esto confirma el comportamiento que sus pruebas exigen; no significa que se hayan implementado los 18 casos de uso completos.

## Copia de prueba

- Compilación: aprobada, 0 errores y 0 advertencias.
- Migraciones PruebaCompleta y TraspasoClinico generadas y aplicadas a SQLite en memoria en las pruebas.
- Suite xUnit: **63/63 aprobadas**, 0 fallidas y 0 omitidas (42 de integración/verificación + 21 de reglas de negocio puras con Moq, patrón A/A/A).
- Las 21 pruebas nuevas aíslan `PortalService` con `Mock<IPortalRepository>` y un `FakeClock`, sin base de datos, para cubrir reglas como contraseña débil, email duplicado, mascota no disponible, blacklist (403), aprobación y prueba de 15 días, límite de voluntario, última administración y el traspaso clínico al adoptante al finalizar.
- Despliegue verificado en vivo con el perfil `Demo`: `/api/mascotas` (200), `/openapi/v1.json` (200), `/scalar/v1` (200), `/` (200) y `POST /api/auth/login` (200, con `token` y sin `passwordHash`).
- El ejecutor alternativo de escenarios también verificó los 33 casos de la primera tanda antes de ampliar la suite.
- JavaScript: `node --check API/wwwroot/app.js` aprobado.
- Interfaz: comprobación del DOM con el servidor real para Administración, Voluntario, Veterinario y Adoptante. En los cuatro roles se verificaron catálogo, inicio de sesión, navegación y formularios sin errores de JavaScript.
- PDF del informe: 9 páginas; fuentes embebidas; páginas renderizadas y revisadas visualmente.

## Alcance de las pruebas

Las pruebas cubren autenticación HTTP, restricciones por rol y propietario, contratos estrictos, normalización, reserva, duplicados, transiciones, límites de fechas, seguimiento diario, alertas y validación veterinaria, acceso final al historial, sanciones, abandono idempotente, índices únicos, versiones, matrícula, última administración, cambios de estado de cuenta, eventos médicos, incidentes y auditoría. No son una cobertura exhaustiva de todas las combinaciones.

La verificación de la interfaz se hizo con DOM y peticiones reales, **sin navegador gráfico**. No se pudo verificar visualmente la interfaz en Chrome en este entorno; comprobar en tu PC el diseño, descargas y comportamiento responsive. El backend sí inició y se probó a través de HTTP. No se verificó SMTP contra un proveedor externo y no se envió ningún correo.

## Repetir en tu PC

```powershell
dotnet build PortalAdopcion.slnx
dotnet test PortalAdopcion.slnx
dotnet run --project API --launch-profile Demo
```

Los comandos de compilación usados aquí incluyeron `-m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false` por restricciones del entorno. Esos parámetros no son necesarios normalmente en Windows. Las pruebas utilizan una base temporal en memoria y no alteran la base local de la demo.
