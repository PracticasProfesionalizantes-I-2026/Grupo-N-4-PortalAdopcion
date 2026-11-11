namespace PortalAdopcion.API;

// SECURITY: Reglas de arranque extraídas de Program.cs para poder probarlas con tests reales.
public static class StartupGuards
{
    /// <summary>
    /// La limpieza de locks de migración huérfanos solo se permite en Development con SQLite: en entornos
    /// productivos/multinstancia borrar el lock podría provocar migraciones concurrentes y corrupción.
    /// </summary>
    public static bool CanCleanMigrationLock(bool isDevelopment, bool isSqlite) => isDevelopment && isSqlite;
}
