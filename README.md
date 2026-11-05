# Portal de Adopción - versión de prueba descargable

Empezar por **[LEEME_PRIMERO.md](LEEME_PRIMERO.md)**.

API .NET 10 + interfaz HTML/CSS/JavaScript + EF Core SQLite, con implementación de prueba para los 18 casos de uso documentados. Arquitectura: Controller → IPortalService/PortalService → IPortalRepository/PortalRepository → DbContext.

```powershell
dotnet restore PortalAdopcion.slnx
dotnet build PortalAdopcion.slnx
dotnet test PortalAdopcion.slnx
dotnet run --project API --launch-profile Demo
```

Interfaz: http://localhost:5104 · Scalar: http://localhost:5104/scalar/v1

No se modificó el repositorio remoto. Ver informes, límites de la prueba y plan de ramas en `Informes/`.
