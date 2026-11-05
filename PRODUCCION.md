# Checklist de producción — Portal de Adopción

> Recomendaciones finales aplicadas (P-pendientes y equiparación con la base de
> referencia HelpDesk). 08/10/2026.

## Claves y secretos (JWT)

- La clave JWT de desarrollo vive en `API/appsettings.Development.json` con el
  valor marcado **`SoloParaDemoLocal-...-CambiarClave`**: es solo para demo.
- En producción **NO** existe `Jwt:Key` en `appsettings.json` → se debe inyectar
  por variable de entorno `Jwt__Key` (o user-secrets). Nunca commitear claves reales.
- El seed de demo (`Demo.Seed`) está **desactivado por defecto**; solo se activa en
  Development (`API/appsettings.Development.json`).

## Antes de publicar (checklist)

- [ ] `Jwt__Key` de al menos 32 caracteres aleatorios (env var en el host).
- [ ] Reemplazar las credenciales de demo (`admin@demo.local`), usarlas solo para CI/demo.
- [ ] SMTP: los correos están deshabilitados por defecto; habilitar solo con cuenta real.
- [ ] Verificar que `bin/`, `obj/`, `.vs/`, `*.db*` sigan en `.gitignore`.
- [ ] Regenerar el snapshot si cambió el modelo: `dotnet ef migrations add X --project DataAccess --startup-project API`.

## Verificación (evidencia en `Informes/Evidencias/`)

- Build: 0 errores / 0 warnings · Tests: 42/42 (`dotnet test PortalAdopcion.slnx`).
- Demo: `dotnet run --project API --launch-profile Demo`.