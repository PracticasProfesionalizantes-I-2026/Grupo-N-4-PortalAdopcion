using Xunit;

// Las pruebas comparten la variable de entorno Jwt__Key para arrancar la API.
// Se ejecutan en serie para que las pruebas de configuración no interfieran con las demás.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
