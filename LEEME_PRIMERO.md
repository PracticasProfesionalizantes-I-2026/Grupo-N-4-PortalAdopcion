# Portal de Adopción - copia de prueba

Esta copia se preparó para descargar, leer y experimentar. No se publicó en GitHub y no altera el repositorio original. Está basada en `main`, commit `708456a2252eeb1077967bce3ac80895fc107467`, del Grupo 4 (Catalina Giordano y Francisco Ledesma).

## Empezar en Windows

1. Extraer el ZIP completo en una carpeta nueva. No copiarlo encima del proyecto original.
2. Tener instalado el **SDK .NET 10**; `dotnet --version` debe empezar con `10.`. La primera ejecución requiere internet para restaurar NuGet.
3. Abrir una terminal en esta carpeta y ejecutar:

```powershell
dotnet restore PortalAdopcion.slnx
dotnet build PortalAdopcion.slnx
dotnet test PortalAdopcion.slnx
dotnet run --project API --launch-profile Demo
```

4. Con la API en ejecución, abrir **http://localhost:5104**. La documentación interactiva está en **http://localhost:5104/scalar/v1**.

También se puede abrir `PortalAdopcion.slnx` en Visual Studio con soporte de .NET 10 y seleccionar API como proyecto de inicio. Los archivos `.bat` incluidos simplifican el arranque y las pruebas. Si Windows muestra una advertencia, primero leer el contenido de los scripts.

## Cuentas ficticias

Todas usan **DemoPortal2026!**. Se crean únicamente con el perfil Development y la opción `Demo:Seed=true`.

| Rol | Email | Para qué sirve |
|---|---|---|
| Administrador | admin@demo.local | Personal, roles, blacklist, reportes, auditoría y gestión |
| Voluntario | voluntario@demo.local | Catálogo, evaluación, entrevista y seguimiento asignado |
| Veterinario | veterinario@demo.local | Historial, eventos médicos y validación de alertas |
| Adoptante | adoptante@demo.local | Postulación nueva, perfil y solicitudes propias |
| Adoptante demo 2 | adoptante2@demo.local | Solicitud Pendiente con más de 15 días |
| Adoptante demo 3 | adoptante3@demo.local | Proceso de prueba con más de 15 días e historial |

Los botones de acceso solo rellenan email y contraseña; igualmente hay que presionar Ingresar. La base `API/portaladopcion-prueba.db` se genera al arrancar mediante una migración nueva. **Esta copia usa una base diferente y no incluye una migración de actualización de la base original.**

## Qué leer

- `Informes/Informe_Estado_Repositorio_Original.pdf`: diagnóstico del repositorio antes de esta copia.
- `Informes/Implementacion_Prueba.md`: qué se agregó y qué simplificaciones tiene.
- `Informes/Guia_Recorrido.md`: escenarios para probar en la interfaz.
- `Informes/Trazabilidad_CU.md`: los 18 CU y su código.
- `Informes/Resultados_Verificacion.md`: resultados reales de compilación y pruebas.
- `Informes/Plan_Ramas.csv` y `Informes/Flujo_GitHub.md`: reparto sugerido y flujo de trabajo por caso.
- `Informes/Leer_Codigo.md`: guía para leer el código por capas.
- `Informes/Limpieza_Duplicados.md`: inventario y limpieza de copias repetidas.
- `CASOS DE USO/` y `COSAS/`: documentación original. Algunas referencias corresponden al diseño anterior; consultar primero los informes nuevos.

El material de comparación anterior (`referencia/`) y las copias de `propuestas-pr/` se retiraron en la limpieza de duplicados; el respaldo quedó fuera de esta carpeta.

## Diferencias importantes

- Aprobada ya no significa Adoptada. Deben seguir entrevista, prueba y finalización.
- Las solicitudes usan la identidad autenticada del adoptante. Administración puede postular en nombre de una cuenta existente.
- La edición de ficha de mascota no acepta el estado; los cambios de estado tienen su propio endpoint y reglas.
- Usuarios, mascotas y perfiles se dan de baja lógicamente; no se borra su historia.
- Hay índices únicos para procesos activos y control de versión para cambios concurrentes.
- Historial y eventos se conservan en la ONG; al finalizar se habilita su consulta al adoptante. No se borra ni se copia destructivamente.
- Los correos se guardan en una bandeja de salida. **Durante esta prueba no se envían correos reales.** Ver configuración SMTP en el informe.
- Los estados JSON se escriben como texto y se rechazan propiedades desconocidas.

## Cómo llevarlo a GitHub

Esta es una implementación conjunta para estudiar. No representa 18 ramas ni 18 revisiones realizadas. Primero acordar los cambios, después trasladarlos al repositorio por caso de uso según `Informes/Plan_Ramas.csv` y `Informes/Flujo_GitHub.md`. El otro integrante debe revisar cada PR. No subir este ZIP entero como sustituto de la revisión cruzada.
