namespace PortalAdopcion.BusinessLogic.Services;
using System.Net.Mail;
using System.Security.Cryptography;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.BusinessLogic.Mapping;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.Constants;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;
using PortalAdopcion.Shared.Security;

public class PortalService(IPortalRepository repo, TimeProvider clock) : IPortalService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public static bool Activa(EstadoSolicitudEnum e) => e is EstadoSolicitudEnum.Pendiente or EstadoSolicitudEnum.EnObservacion or EstadoSolicitudEnum.Aprobada or EstadoSolicitudEnum.EnEntrevista or EstadoSolicitudEnum.EnPeriodoDePrueba;
    private static readonly string[] Roles = ["Administrador", "Voluntario", "Veterinario", "Adoptante"];
    // Se traduce a un IN (...) en SQL en lugar de filtrar en memoria (evita leer toda la tabla).
    private static readonly EstadoSolicitudEnum[] EstadosActivos = [EstadoSolicitudEnum.Pendiente, EstadoSolicitudEnum.EnObservacion, EstadoSolicitudEnum.Aprobada, EstadoSolicitudEnum.EnEntrevista, EstadoSolicitudEnum.EnPeriodoDePrueba];
    private static void Require(Actor a, params string[] roles)
    {
        if (!roles.Contains(a.Rol))
            throw new AccessException("Tu rol no permite esta operación.");
    }

    private static string Text(string? s, string campo, int max = BusinessRules.MaxLongitudTexto)
    {
        if (string.IsNullOrWhiteSpace(s) || s.Trim().Length > max)
            throw new ValidationException($"{campo}: valor obligatorio de hasta {max} caracteres.");
        return s.Trim();
    }

    private static string Numeric(string? s, string campo, int max)
    {
        var v = Text(s, campo, max);
        // Normalizar separadores comunes que el usuario puede introducir (espacio NBSP, guiones largos, puntos medios).
        v = v.Replace('\u00A0', ' ').Replace('·', '.').Replace('•', '.').Replace('–', '-').Replace('—', '-');
        // Permitir que el usuario pase separadores (puntos, guiones, espacios).
        // Rechazar otros caracteres (letras, símbolos) y normalizar a solo dígitos ASCII.
        var invalidChars = v.Where(c => !((c >= '0' && c <= '9') || c == '.' || c == '-' || char.IsWhiteSpace(c))).Distinct().ToArray();
        if (invalidChars.Length > 0)
            throw new ValidationException($"{campo}: caracteres inválidos: '{new string(invalidChars)}'. Solo se permiten dígitos (0-9), puntos, guiones y espacios.");
        var digits = new string(v.Where(c => c >= '0' && c <= '9').ToArray());
        if (string.IsNullOrEmpty(digits))
            throw new ValidationException($"{campo}: valor obligatorio de hasta {max} dígitos.");
        if (digits.Length > max)
            throw new ValidationException($"{campo}: máximo {max} dígitos permitidos.");
        return digits;
    }

    private static string Civil(string? s)
    {
        var v = Text(s, "Estado civil", BusinessRules.MaxLongitudEstadoCivil);
        if (!BusinessRules.EstadosCiviles.Contains(v))
            throw new ValidationException("Estado civil inválido. Opciones: " + string.Join(", ", BusinessRules.EstadosCiviles) + ".");
        return v;
    }

    private static string Email(string s)
    {
        s = Text(s, "Email", BusinessRules.MaxLongitudEmail).ToLowerInvariant();
        if (!MailAddress.TryCreate(s, out var a) || a.Address != s)
            throw new ValidationException("Email inválido.");
        return s;
    }

    private static void Password(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length < BusinessRules.PasswordMinLength || s.Length > BusinessRules.PasswordMaxLength || !s.Any(char.IsUpper) || !s.Any(char.IsLower) || !s.Any(char.IsDigit))
            throw new ValidationException("Contraseña: " + BusinessRules.ContrasenaRequisitos);
    }

    private static void EnumValid<T>(T value)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ValidationException("Valor de enumeración inválido.");
    }

    private async Task<T> Find<T>(Guid id)
        where T : Registro => await repo.FindAsync<T>(id) ?? throw new NotFoundException(ErrorMessages.RegistroInexistente);
    private Task Save(Actor a, string action, Guid id, string reason, params Cambio[] changes) => repo.SaveAsync(changes, new EventoAuditoria { UsuarioId = a.Id, Fecha = Now, Accion = action, EntidadId = id, Motivo = reason });
    private Cambio Notice(Guid id, string msg, string? role = null, Guid? user = null, string channel = "Interna", string? destination = null) => new(new Notificacion { EntidadId = id, Mensaje = msg, Rol = role, UsuarioId = user, Fecha = Now, Canal = channel, Destino = destination }, true);
    private async Task<Adoptante> Perfil(PerfilDTO d)
    {
        if (d.Edad < BusinessRules.MinEdadAdoptante || d.Edad > BusinessRules.MaxEdadAdoptante)
            throw new ValidationException($"Edad del adoptante: {BusinessRules.MinEdadAdoptante} a {BusinessRules.MaxEdadAdoptante} años.");
        var a = new Adoptante
        {
            NombreCompleto = Text(d.NombreCompleto, "Nombre", BusinessRules.MaxLongitudNombre),
            Email = Email(d.Email),
            Dni = Numeric(d.Dni, "DNI", BusinessRules.MaxLongitudDni),
            Telefono = Numeric(d.Telefono, "Teléfono", BusinessRules.MaxLongitudTelefono),
            Direccion = Text(d.Direccion, "Dirección", BusinessRules.MaxLongitudDireccion),
            Edad = d.Edad,
            EstadoCivil = Civil(d.EstadoCivil),
            DatosLaborales = Text(d.DatosLaborales, "Datos laborales", BusinessRules.MaxLongitudDatosLaborales),
            FechaRegistro = Now
        };
        var duplicado = await repo.AnyAsync<Adoptante>(x => x.Email == a.Email || x.Dni == a.Dni);
        if (duplicado)
            throw new ConflictException(ErrorMessages.EmailODNIExistente);
        return a;
    }

    public async Task<UsuarioResponseDTO> RegisterAsync(RegistroUsuarioDTO d)
    {
        Password(d.Password);
        var profile = await Perfil(new(d.Nombre, d.Email, d.Dni, d.Telefono, d.Direccion, d.Edad, d.EstadoCivil, d.DatosLaborales));
        if (await repo.AnyAsync<Usuario>(u => u.Email == profile.Email || u.Dni == profile.Dni))
            throw new ConflictException(ErrorMessages.CuentaYaExiste);
        var u = new Usuario
        {
            Nombre = profile.NombreCompleto,
            Email = profile.Email,
            Dni = profile.Dni,
            PasswordHash = PasswordSecurity.Hash(d.Password),
            AdoptanteId = profile.Id,
            FechaRegistro = Now
        };
        await Save(new(u.Id, u.Rol, profile.Id), "CU-06 Registro", u.Id, "Cuenta creada", new Cambio(profile, true), new Cambio(u, true));
        return u.ToDto();
    }

    public async Task<Usuario> LoginAsync(LoginDTO d)
    {
        if (string.IsNullOrEmpty(d.Password))
            throw new AuthenticationException(ErrorMessages.CredencialesInvalidas);
        var email = Email(d.Email);
        var u = await repo.FirstAsync<Usuario>(x => x.Email == email);
        if (u is null || !PasswordSecurity.Verify(d.Password, u.PasswordHash) || (u.Estado == "Baja" || (u.Estado == "Suspendida" && u.Rol != "Adoptante")))
            throw new AuthenticationException(ErrorMessages.CuentaNoHabilitada);
        await Save(new(u.Id, u.Rol, u.AdoptanteId), "CU-07 Login", u.Id, "Inicio de sesión");
        return u;
    }

    public async Task<UsuarioResponseDTO> MeAsync(Actor a) => (await Find<Usuario>(a.Id)).ToDto();
    public async Task<List<UsuarioResponseDTO>> UsersAsync(Actor a)
    {
        Require(a, "Administrador");
        return (await repo.ListAsync<Usuario>()).Select(x => x.ToDto()).ToList();
    }

    public async Task<UsuarioResponseDTO> CreateStaffAsync(Actor a, PersonalDTO d)
    {
        Require(a, "Administrador");
        Password(d.Password);
        if (!Roles.Contains(d.Rol) || d.Rol == "Adoptante")
            throw new ValidationException("Rol de personal inválido.");
        var email = Email(d.Email);
        var dni = Numeric(d.Dni, "DNI", BusinessRules.MaxLongitudDni);
        var matricula = d.Rol == "Veterinario" ? Text(d.Matricula, "Matrícula", BusinessRules.MaxLongitudMatricula) : null;
        if (await repo.AnyAsync<Usuario>(x => x.Email == email || x.Dni == dni || (matricula != null && x.Matricula == matricula)))
            throw new ConflictException("Email, DNI o matrícula duplicados.");
        var u = new Usuario
        {
            Nombre = Text(d.Nombre, "Nombre", BusinessRules.MaxLongitudNombre),
            Email = email,
            Dni = dni,
            Rol = d.Rol,
            Matricula = matricula,
            PasswordHash = PasswordSecurity.Hash(d.Password),
            FechaRegistro = Now
        };
        await Save(a, "CU-06 Personal", u.Id, "Alta de personal", new Cambio(u, true));
        return u.ToDto();
    }

    public async Task<UsuarioResponseDTO> ChangeUserAsync(Actor a, Guid id, UsuarioCambioDTO d)
    {
        Require(a, "Administrador");
        if (!Roles.Contains(d.Rol) || !new[]
        {
            "Activa",
            "Suspendida",
            "Baja"
        }.Contains(d.Estado))
            throw new ValidationException("Rol o estado inválido.");
        var u = await Find<Usuario>(id);
        if ((u.Rol == "Adoptante") != (d.Rol == "Adoptante"))
            throw new ConflictException("Las cuentas de adoptante y de personal se mantienen separadas.");
        if (u.Rol == "Administrador" && (d.Rol != "Administrador" || d.Estado != "Activa") && await repo.CountAsync<Usuario>(x => x.Rol == "Administrador" && x.Estado == "Activa") <= 1)
            throw new ConflictException(ErrorMessages.DebeConservarseAdministrador);
        u.Rol = d.Rol;
        u.Estado = d.Estado;
        u.Matricula = d.Rol == "Veterinario" ? Text(d.Matricula, "Matrícula", BusinessRules.MaxLongitudMatricula) : null;
        await Save(a, "CU-08 Cuenta", id, "Cambio de rol/estado", new Cambio(u));
        return u.ToDto();
    }

    public async Task<List<AdoptanteResponseDTO>> ProfilesAsync(Actor a)
    {
        if (a.Rol == "Adoptante")
        {
            if (a.AdoptanteId is null)
                return [];
            return (await repo.ListAsync<Adoptante>(x => x.Id == a.AdoptanteId!.Value)).Select(x => x.ToDto()).ToList();
        }

        if (a.Rol is "Administrador" or "Voluntario")
            return (await repo.ListAsync<Adoptante>()).Select(x => x.ToDto()).ToList();
        throw new AccessException("Acceso restringido.");
    }

    public async Task<AdoptanteResponseDTO> ProfileAsync(Actor a, Guid id)
    {
        Require(a, "Administrador", "Voluntario", "Adoptante");
        if (a.Rol == "Adoptante" && a.AdoptanteId != id)
            throw new AccessException("Solo podés consultar tu perfil.");
        return (await Find<Adoptante>(id)).ToDto();
    }

    public async Task<AdoptanteResponseDTO> CreateProfileAsync(Actor a, PerfilDTO d)
    {
        Require(a, "Administrador", "Voluntario");
        var p = await Perfil(d);
        await Save(a, "Perfil Alta", p.Id, "Alta administrativa", new Cambio(p, true));
        return p.ToDto();
    }

    public async Task<AdoptanteResponseDTO> UpdateProfileAsync(Actor a, Guid id, PerfilDTO d)
    {
        Require(a, "Administrador", "Voluntario", "Adoptante");
        if (a.Rol == "Adoptante" && a.AdoptanteId != id)
            throw new AccessException("Solo podés editar tu perfil.");
        var p = await Find<Adoptante>(id);
        if (Email(d.Email) != p.Email || Numeric(d.Dni, "DNI", BusinessRules.MaxLongitudDni) != p.Dni)
            throw new ConflictException("Email y DNI de identidad no se modifican en esta versión.");
        if (d.Edad < BusinessRules.MinEdadAdoptante || d.Edad > BusinessRules.MaxEdadAdoptante)
            throw new ValidationException("Edad inválida.");
        p.NombreCompleto = Text(d.NombreCompleto, "Nombre", BusinessRules.MaxLongitudNombre);
        p.Telefono = Numeric(d.Telefono, "Teléfono", BusinessRules.MaxLongitudTelefono);
        p.Direccion = Text(d.Direccion, "Dirección", BusinessRules.MaxLongitudDireccion);
        p.Edad = d.Edad;
        p.EstadoCivil = Civil(d.EstadoCivil);
        p.DatosLaborales = Text(d.DatosLaborales, "Datos laborales", BusinessRules.MaxLongitudDatosLaborales);
        await Save(a, "Perfil Edición", id, "Datos de perfil", new Cambio(p));
        return p.ToDto();
    }

    public async Task DeleteProfileAsync(Actor a, Guid id)
    {
        Require(a, "Administrador");
        var p = await Find<Adoptante>(id);
        if (await repo.AnyAsync<SolicitudDeAdopcion>(x => x.AdoptanteId == id && EstadosActivos.Contains(x.Estado)))
            throw new ConflictException("Tiene un proceso activo.");
        p.Baja = true;
        var changes = new List<Cambio>
        {
            new Cambio(p)
        };
        var u = await repo.FirstAsync<Usuario>(x => x.AdoptanteId == id);
        if (u != null)
        {
            u.Estado = "Baja";
            changes.Add(new Cambio(u));
        }

        await Save(a, "Perfil Baja", id, "Baja lógica", changes.ToArray());
    }

    public async Task<List<MascotaResponseDTO>> PetsAsync(EstadoMascotaEnum? estado)
    {
        if (estado.HasValue)
            EnumValid(estado.Value);
        var pets = estado.HasValue
            ? await repo.ListAsync<Mascota>(x => x.Estado != EstadoMascotaEnum.Inactiva && x.Estado == estado.Value)
            : await repo.ListAsync<Mascota>(x => x.Estado != EstadoMascotaEnum.Inactiva);
        return pets.OrderBy(x => x.Nombre).Select(x => x.ToDto()).ToList();
    }

    public async Task<MascotaResponseDTO> PetAsync(Guid id) => (await Find<Mascota>(id)).ToDto();
    private static void PetData(Mascota m, MascotaFichaDTO d)
    {
        m.Nombre = Text(d.Nombre, "Nombre", BusinessRules.MaxLongitudNombreMascota);
        EnumValid(d.Especie);
        EnumValid(d.Sexo);
        if (d.Edad < BusinessRules.MinEdadMascota || d.Edad > BusinessRules.MaxEdadMascota)
            throw new ValidationException($"Edad: {BusinessRules.MinEdadMascota} a {BusinessRules.MaxEdadMascota} años.");
        if (d.FotoUrl != null && d.FotoUrl != "" && (!Uri.TryCreate(d.FotoUrl, UriKind.Absolute, out var url) || url.Scheme != "https"))
            throw new ValidationException("Foto: URL HTTPS válida.");
        m.Especie = d.Especie;
        m.Sexo = d.Sexo;
        m.Edad = d.Edad;
        m.Raza = d.Raza?.Trim();
        if (m.Raza?.Length > BusinessRules.MaxLongitudRaza)
            throw new ValidationException("Raza demasiado larga.");
        m.FotoUrl = string.IsNullOrWhiteSpace(d.FotoUrl) ? null : d.FotoUrl.Trim();
        m.Chip = string.IsNullOrWhiteSpace(d.Chip) ? null : Text(d.Chip, "Chip", BusinessRules.MaxLongitudChip);
    }

    public async Task<MascotaResponseDTO> CreatePetAsync(Actor a, MascotaFichaDTO d)
    {
        Require(a, "Administrador", "Voluntario");
        var m = new Mascota
        {
            FechaRegistro = Now
        };
        PetData(m, d);
        await Save(a, "CU-09 Alta", m.Id, "Mascota registrada", new Cambio(m, true));
        return m.ToDto();
    }

    public async Task<MascotaResponseDTO> UpdatePetAsync(Actor a, Guid id, MascotaFichaDTO d)
    {
        Require(a, "Administrador", "Voluntario");
        var m = await Find<Mascota>(id);
        PetData(m, d);
        await Save(a, "CU-09 Edición", id, "Ficha actualizada", new Cambio(m));
        return m.ToDto();
    }

    public async Task<MascotaResponseDTO> ChangePetAsync(Actor a, Guid id, CambioMascotaDTO d)
    {
        Require(a, "Administrador", "Voluntario");
        EnumValid(d.Estado);
        var reason = Text(d.Motivo, "Motivo");
        var m = await Find<Mascota>(id);
        if (m.Estado == EstadoMascotaEnum.Adoptada)
            throw new ConflictException("Una mascota adoptada no cambia de estado administrativamente.");
        if (d.Estado is not (EstadoMascotaEnum.Disponible or EstadoMascotaEnum.EnTratamiento or EstadoMascotaEnum.Inactiva))
            throw new ConflictException("Este estado solo se cambia desde el proceso de adopción.");
        if (await repo.AnyAsync<SolicitudDeAdopcion>(x => x.MascotaId == id && EstadosActivos.Contains(x.Estado)))
            throw new ConflictException("La mascota tiene un proceso activo.");
        m.Estado = d.Estado;
        await Save(a, "CU-09 Estado", id, reason, new Cambio(m));
        return m.ToDto();
    }

    public async Task<List<SancionResponseDTO>> BlacklistAsync(Actor a)
    {
        Require(a, "Administrador");
        return (await repo.ListAsync<SancionBlacklist>()).Select(x => x.ToDto()).ToList();
    }

    private Task<bool> Blocked(Guid profile) => repo.AnyAsync<SancionBlacklist>(x => x.AdoptanteId == profile && !x.Revocada && x.Desde <= Now && x.Hasta > Now);
    public async Task<SancionResponseDTO> SaveBlacklistAsync(Actor a, Guid? id, BlacklistDTO d)
    {
        Require(a, "Administrador");
        await Find<Adoptante>(d.AdoptanteId);
        if (d.Desde >= d.Hasta)
            throw new ValidationException("La fecha final debe superar la inicial.");
        var s = id.HasValue ? await Find<SancionBlacklist>(id.Value) : new SancionBlacklist();
        if (id.HasValue && s.AdoptanteId != d.AdoptanteId)
            throw new ConflictException("No se cambia la persona de una sanción.");
        s.AdoptanteId = d.AdoptanteId;
        s.Motivo = Text(d.Motivo, "Motivo");
        s.Desde = d.Desde.ToUniversalTime();
        s.Hasta = d.Hasta.ToUniversalTime();
        s.Revocada = false;
        var changes = new List<Cambio>
        {
            new(s, !id.HasValue)
        };
        if (s.Desde <= Now && s.Hasta > Now)
        {
            var activas = await repo.ListAsync<SolicitudDeAdopcion>(x => x.AdoptanteId == s.AdoptanteId && EstadosActivos.Contains(x.Estado));
            var mascotaIds = activas.Select(r => r.MascotaId).ToList();
            var pets = (await repo.ListAsync<Mascota>(m => mascotaIds.Contains(m.Id))).ToDictionary(m => m.Id);
            foreach (var request in activas)
            {
                request.Estado = EstadoSolicitudEnum.Rechazada;
                request.Motivo = "Sanción vigente: " + s.Motivo;
                request.UltimaActividad = Now;
                var pet = pets[request.MascotaId];
                pet.Estado = EstadoMascotaEnum.Disponible;
                changes.Add(new Cambio(request));
                changes.Add(new Cambio(pet));
            }
        }

        await Save(a, "CU-01 Sanción", s.Id, s.Motivo, changes.ToArray());
        return s.ToDto();
    }

    public async Task RevokeBlacklistAsync(Actor a, Guid id, string reason)
    {
        Require(a, "Administrador");
        var s = await Find<SancionBlacklist>(id);
        s.Revocada = true;
        await Save(a, "CU-01 Revocación", id, Text(reason, "Motivo"), new Cambio(s));
    }

    private static void Own(Actor a, SolicitudDeAdopcion s)
    {
        if (a.Rol == "Adoptante" && a.AdoptanteId != s.AdoptanteId)
            throw new AccessException("La solicitud pertenece a otro adoptante.");
    }

    public async Task<List<SolicitudResponseDTO>> RequestsAsync(Actor a)
    {
        if (a.Rol == "Adoptante")
        {
            if (a.AdoptanteId is null)
                return [];
            return (await repo.ListAsync<SolicitudDeAdopcion>(x => x.AdoptanteId == a.AdoptanteId!.Value))
                .OrderByDescending(x => x.FechaSolicitud).Select(x => x.ToDto()).ToList();
        }

        return (await repo.ListAsync<SolicitudDeAdopcion>())
            .OrderByDescending(x => x.FechaSolicitud).Select(x => x.ToDto()).ToList();
    }

    public async Task<SolicitudResponseDTO> RequestAsync(Actor a, Guid id)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Own(a, s);
        return s.ToDto();
    }

    public async Task<SolicitudResponseDTO> PostulateAsync(Actor a, PostulacionDTO d)
    {
        Require(a, "Adoptante", "Administrador");
        var profile = a.Rol == "Adoptante" ? a.AdoptanteId : d.AdoptanteId;
        if (!profile.HasValue)
            throw new ValidationException("Se requiere adoptante.");
        var p = await Find<Adoptante>(profile.Value);
        var m = await Find<Mascota>(d.MascotaId);
        var user = await repo.FirstAsync<Usuario>(x => x.AdoptanteId == p.Id);
        if (await Blocked(p.Id))
            throw new AccessException(ErrorMessages.PostulacionBloqueada);
        if (p.Baja || user is null || user.Estado != "Activa")
            throw new ConflictException(ErrorMessages.PerfilNoHabilitado);
        if (p.Edad < BusinessRules.MinEdadAdoptante || string.IsNullOrWhiteSpace(p.DatosLaborales) || string.IsNullOrWhiteSpace(p.EstadoCivil))
            throw new ValidationException("Completá tu perfil.");
        if (m.Estado != EstadoMascotaEnum.Disponible)
            throw new ConflictException(ErrorMessages.MascotaNoDisponible);
        if (await repo.AnyAsync<SolicitudDeAdopcion>(x => EstadosActivos.Contains(x.Estado) && (x.MascotaId == m.Id || x.AdoptanteId == p.Id)))
            throw new ConflictException(ErrorMessages.ProcesoActivo);
        var s = new SolicitudDeAdopcion
        {
            MascotaId = m.Id,
            AdoptanteId = p.Id,
            FechaSolicitud = Now,
            UltimaActividad = Now,
            MotivoAdopcion = Text(d.MotivoAdopcion, "Motivo de adopción"),
            CodigoConfirmacion = Convert.ToHexString(RandomNumberGenerator.GetBytes(8))
        };
        m.Estado = EstadoMascotaEnum.Reservada;
        await Save(a, "CU-02/CU-05 Postulación", s.Id, "Proceso creado", new Cambio(s, true), new Cambio(m), Notice(s.Id, "Nueva postulación", "Administrador"), Notice(s.Id, "Confirmación: " + s.CodigoConfirmacion, user: user.Id), Notice(s.Id, "Confirmación: " + s.CodigoConfirmacion, user: user.Id, channel: "CorreoBccPendiente", destination: p.Email));
        return s.ToDto();
    }

    public async Task<SolicitudResponseDTO> EvaluateAsync(Actor a, Guid id, EvaluacionDTO d)
    {
        Require(a, "Administrador", "Voluntario");
        var s = await Find<SolicitudDeAdopcion>(id);
        if (s.Estado is not (EstadoSolicitudEnum.Pendiente or EstadoSolicitudEnum.EnObservacion))
            throw new ConflictException("No se puede evaluar en este estado.");
        if (d.Estado is not (EstadoSolicitudEnum.Aprobada or EstadoSolicitudEnum.Rechazada or EstadoSolicitudEnum.EnObservacion))
            throw new ValidationException("Estado de evaluación inválido.");
        if (d.Estado == EstadoSolicitudEnum.Aprobada && Now < s.FechaSolicitud.AddDays(BusinessRules.MinDiasAntesAprobacion))
            throw new ConflictException($"Deben transcurrir al menos {BusinessRules.MinDiasAntesAprobacion} días.");
        var m = await Find<Mascota>(s.MascotaId);
        var changes = new List<Cambio>();
        var bloqueado = await Blocked(s.AdoptanteId);
        if (bloqueado || m.Estado != EstadoMascotaEnum.Reservada)
        {
            s.Estado = bloqueado ? EstadoSolicitudEnum.Rechazada : EstadoSolicitudEnum.Cancelada;
            s.Motivo = "Persona sancionada o mascota no disponible";
            s.UltimaActividad = Now;
            if (m.Estado == EstadoMascotaEnum.Reservada)
            {
                m.Estado = EstadoMascotaEnum.Disponible;
                changes.Add(new Cambio(m));
            }

            changes.Add(new Cambio(s));
            await Save(a, "CU-04 Bloqueo", s.Id, s.Motivo, changes.ToArray());
            throw new ConflictException(s.Motivo);
        }

        s.Motivo = d.Estado == EstadoSolicitudEnum.Aprobada ? d.Motivo?.Trim() : Text(d.Motivo, "Motivo/observaciones");
        s.EntidadAutorizante = Text(d.EntidadAutorizante, "Entidad autorizante", BusinessRules.MaxLongitudEntidadAutorizante);
        s.EvaluadorId = a.Id;
        s.FechaEvaluacion = Now;
        s.UltimaActividad = Now;
        s.Estado = d.Estado;
        if (d.Estado == EstadoSolicitudEnum.Rechazada)
            m.Estado = EstadoMascotaEnum.Disponible;
        await Save(a, "CU-04 Evaluación", s.Id, s.Motivo ?? "Aprobada", new Cambio(s), new Cambio(m));
        return s.ToDto();
    }

    public async Task<SolicitudResponseDTO> CancelAsync(Actor a, Guid id, string reason)
    {
        Require(a, "Administrador", "Voluntario", "Adoptante");
        var s = await Find<SolicitudDeAdopcion>(id);
        Own(a, s);
        if (!Activa(s.Estado))
            throw new ConflictException("El proceso ya terminó.");
        reason = Text(reason, "Motivo");
        var m = await Find<Mascota>(s.MascotaId);
        var changes = new List<Cambio>();
        if (s.Estado == EstadoSolicitudEnum.EnPeriodoDePrueba)
            changes.Add(new(new SancionBlacklist { AdoptanteId = s.AdoptanteId, Motivo = "Cancelación durante prueba: " + reason, Desde = Now, Hasta = Now.AddYears(BusinessRules.AniosSancionCancelacionPrueba) }, true));
        s.Estado = EstadoSolicitudEnum.Cancelada;
        s.Motivo = reason;
        s.UltimaActividad = Now;
        m.Estado = EstadoMascotaEnum.Disponible;
        changes.Add(new Cambio(s));
        changes.Add(new Cambio(m));
        await Save(a, "CU-13 Cancelación", id, reason, changes.ToArray());
        return s.ToDto();
    }

    public async Task<EntrevistaResponseDTO> ScheduleAsync(Actor a, Guid id, EntrevistaDTO d)
    {
        Require(a, "Administrador", "Voluntario");
        var s = await Find<SolicitudDeAdopcion>(id);
        if (s.Estado != EstadoSolicitudEnum.Aprobada)
            throw new ConflictException("La solicitud debe estar aprobada.");
        var volunteer = await Find<Usuario>(d.VoluntarioId);
        if (volunteer.Rol != "Voluntario" || volunteer.Estado != "Activa")
            throw new ValidationException("Voluntario no habilitado.");
        if (d.FechaProgramada.ToUniversalTime() < Now)
            throw new ValidationException("La entrevista no puede programarse en el pasado.");
        if (await repo.CountAsync<SolicitudDeAdopcion>(x => x.VoluntarioId == d.VoluntarioId && EstadosActivos.Contains(x.Estado)) >= BusinessRules.MaxProcesosActivosPorVoluntario)
            throw new ConflictException("El voluntario ya tiene dos procesos activos asignados.");
        var e = new Entrevista
        {
            SolicitudId = id,
            VoluntarioId = d.VoluntarioId,
            FechaProgramada = d.FechaProgramada.ToUniversalTime()
        };
        s.Estado = EstadoSolicitudEnum.EnEntrevista;
        s.VoluntarioId = d.VoluntarioId;
        s.UltimaActividad = Now;
        var m = await Find<Mascota>(s.MascotaId);
        m.Estado = EstadoMascotaEnum.EnEntrevista;
        await Save(a, "CU-11 Programar", id, "Entrevista programada", new Cambio(e, true), new Cambio(s), new Cambio(m), Notice(id, "Entrevista asignada", user: d.VoluntarioId));
        return e.ToDto();
    }

    public async Task<List<EntrevistaResponseDTO>> InterviewsAsync(Actor a, Guid id)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Own(a, s);
        return (await repo.ListAsync<Entrevista>(x => x.SolicitudId == id)).Select(x => x.ToDto()).ToList();
    }

    private static void Assigned(Actor a, SolicitudDeAdopcion s)
    {
        Require(a, "Administrador", "Voluntario");
        if (a.Rol == "Voluntario" && s.VoluntarioId != a.Id)
            throw new AccessException("El proceso está asignado a otro voluntario.");
    }

    public async Task<SolicitudResponseDTO> InterviewResultAsync(Actor a, Guid id, ResultadoEntrevistaDTO d)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Assigned(a, s);
        if (s.Estado != EstadoSolicitudEnum.EnEntrevista)
            throw new ConflictException("No está en entrevista.");
        if (d.Resultado is not ("Favorable" or "Desfavorable"))
            throw new ValidationException("Resultado inválido.");
        var e = await repo.FirstAsync<Entrevista>(x => x.SolicitudId == id) ?? throw new NotFoundException("Entrevista inexistente.");
        if (Now < e.FechaProgramada)
            throw new ConflictException("La entrevista todavía no llegó a su fecha programada.");
        e.Resultado = d.Resultado;
        e.Observaciones = Text(d.Observaciones, "Observaciones");
        e.FechaRealizada = Now;
        s.UltimaActividad = Now;
        var m = await Find<Mascota>(s.MascotaId);
        if (d.Resultado == "Favorable")
        {
            s.Estado = EstadoSolicitudEnum.EnPeriodoDePrueba;
            s.InicioPrueba = Now;
            m.Estado = EstadoMascotaEnum.EnPeriodoDePrueba;
        }
        else
        {
            s.Estado = EstadoSolicitudEnum.Rechazada;
            s.Motivo = e.Observaciones;
            m.Estado = EstadoMascotaEnum.Disponible;
        }

        await Save(a, "CU-11 Resultado", id, e.Observaciones, new Cambio(e), new Cambio(s), new Cambio(m));
        return s.ToDto();
    }

    public async Task<List<SeguimientoResponseDTO>> FollowupsAsync(Actor a, Guid id)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Own(a, s);
        return (await repo.ListAsync<Seguimiento>(x => x.SolicitudId == id)).OrderBy(x => x.Fecha).Select(x => x.ToDto()).ToList();
    }

    public async Task<SeguimientoResponseDTO> FollowupAsync(Actor a, Guid id, SeguimientoDTO d)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Assigned(a, s);
        if (s.Estado != EstadoSolicitudEnum.EnPeriodoDePrueba)
            throw new ConflictException("No está en período de prueba.");
        var entry = new Seguimiento
        {
            SolicitudId = id,
            VoluntarioId = a.Id,
            Fecha = Now.Date,
            Animo = Text(d.Animo, "Ánimo"),
            Alimentacion = Text(d.Alimentacion, "Alimentación"),
            Tareas = Text(d.Tareas, "Tareas"),
            Medicacion = Text(d.Medicacion, "Medicación"),
            Limpieza = Text(d.Limpieza, "Limpieza"),
            Alerta = d.Alerta
        };
        s.UltimaActividad = Now;
        if (d.Alerta)
        {
            s.Alerta = true;
            s.ValidacionVeterinaria = false;
            s.VeterinarioValidadorId = null;
        }

        var changes = new List<Cambio>
        {
            new Cambio(entry, true),
            new Cambio(s)
        };
        if (d.Alerta)
            changes.Add(Notice(id, "Alerta en seguimiento", "Veterinario"));
        await Save(a, "CU-12 Seguimiento", id, d.Alerta ? "Alerta" : "Registro diario", changes.ToArray());
        return entry.ToDto();
    }

    public async Task<SolicitudResponseDTO> ValidateClinicalAsync(Actor a, Guid id, string reason)
    {
        Require(a, "Veterinario");
        var s = await Find<SolicitudDeAdopcion>(id);
        if (s.Estado != EstadoSolicitudEnum.EnPeriodoDePrueba)
            throw new ConflictException("No está en prueba.");
        s.ValidacionVeterinaria = true;
        s.VeterinarioValidadorId = a.Id;
        s.UltimaActividad = Now;
        await Save(a, "CU-12 Validación veterinaria", id, Text(reason, "Motivo"), new Cambio(s));
        return s.ToDto();
    }

    public async Task<SolicitudResponseDTO> FinalizeAsync(Actor a, Guid id, string reason)
    {
        var s = await Find<SolicitudDeAdopcion>(id);
        Assigned(a, s);
        if (s.Estado != EstadoSolicitudEnum.EnPeriodoDePrueba || s.InicioPrueba is null)
            throw new ConflictException("No está en prueba.");
        if (Now < s.InicioPrueba.Value.AddDays(BusinessRules.MinDiasPeriodoPrueba))
            throw new ConflictException($"El período de prueba debe durar al menos {BusinessRules.MinDiasPeriodoPrueba} días.");
        if (s.Alerta && !s.ValidacionVeterinaria)
            throw new ConflictException(ErrorMessages.AlertaRequiereValidacion);
        var history = await repo.FirstAsync<HistorialClinico>(x => x.MascotaId == s.MascotaId)
            ?? throw new ConflictException(ErrorMessages.HistorialRequerido);
        if (await Blocked(s.AdoptanteId))
            throw new ConflictException("Adoptante sancionado.");
        s.Estado = EstadoSolicitudEnum.Adoptada;
        s.FechaFinalizacion = Now;
        s.UltimaActividad = Now;
        var m = await Find<Mascota>(s.MascotaId);
        m.Estado = EstadoMascotaEnum.Adoptada;
        var owner = await repo.FirstAsync<Usuario>(x => x.AdoptanteId == s.AdoptanteId) ?? throw new NotFoundException(ErrorMessages.RegistroInexistente);
        var clone = new HistorialClinico
        {
            MascotaId = null,
            AdoptanteId = s.AdoptanteId,
            OrigenHistorialId = history.Id,
            OrigenMascotaId = s.MascotaId,
            Fecha = history.Fecha,
            VeterinarioId = history.VeterinarioId,
            Peso = history.Peso,
            EstadoGeneral = history.EstadoGeneral,
            Vacunas = history.Vacunas,
            Antecedentes = history.Antecedentes,
            Esterilizada = history.Esterilizada
        };
        var cloneEvents = (await repo.ListAsync<EventoMedico>(e => e.HistorialClinicoId == history.Id))
            .Select(e => new EventoMedico
            {
                HistorialClinicoId = clone.Id,
                VeterinarioId = e.VeterinarioId,
                Fecha = e.Fecha,
                Tipo = e.Tipo,
                Descripcion = e.Descripcion,
                Tratamiento = e.Tratamiento
            })
            .ToList();
        history.MascotaId = null;
        var changes = new List<Cambio>
        {
            new Cambio(s),
            new Cambio(m),
            new Cambio(history),
            new Cambio(clone, true)
        };
        changes.AddRange(cloneEvents.Select(e => new Cambio(e, true)));
        changes.Add(Notice(id, "Adopción finalizada. El historial clínico fue transferido a tu perfil.", user: owner.Id));
        await Save(a, "CU-15 Finalizar", id, Text(reason, "Motivo"), changes.ToArray());
        return s.ToDto();
    }

    public async Task<int> AbandonAsync()
    {
        var corte = Now.AddDays(-BusinessRules.DiasInactividadAbandono);
        var candidatas = await repo.ListAsync<SolicitudDeAdopcion>(x => x.Estado == EstadoSolicitudEnum.EnEntrevista && x.UltimaActividad < corte);
        var mascotaIds = candidatas.Select(x => x.MascotaId).ToList();
        var pets = (await repo.ListAsync<Mascota>(m => mascotaIds.Contains(m.Id))).ToDictionary(m => m.Id);
        var count = 0;
        foreach (var s in candidatas)
        {
            var m = pets[s.MascotaId];
            s.Estado = EstadoSolicitudEnum.Abandonada;
            s.Motivo = $"Más de {BusinessRules.DiasInactividadAbandono} días sin actividad";
            s.UltimaActividad = Now;
            m.Estado = EstadoMascotaEnum.Disponible;
            try
            {
                await Save(new(Guid.Empty, "Sistema", null), "CU-14 Abandono", s.Id, s.Motivo, new Cambio(s), new Cambio(m));
                count++;
            }
            catch (ConflictException)
            { /* Otra ejecución resolvió este proceso. */
            }
        }

        return count;
    }

    public async Task<HistorialResponseDTO> CreateHistoryAsync(Actor a, Guid pet, ClinicaDTO d)
    {
        Require(a, "Veterinario");
        await Find<Mascota>(pet);
        if (d.Peso <= BusinessRules.MinPesoMascota || d.Peso > BusinessRules.MaxPesoMascota)
            throw new ValidationException("Peso inválido.");
        if (await repo.AnyAsync<HistorialClinico>(x => x.MascotaId == pet))
            throw new ConflictException("El historial ya existe.");
        var h = new HistorialClinico
        {
            MascotaId = pet,
            Fecha = Now,
            VeterinarioId = a.Id,
            Peso = d.Peso,
            EstadoGeneral = Text(d.EstadoGeneral, "Estado general"),
            Vacunas = Text(d.Vacunas, "Vacunas"),
            Antecedentes = Text(d.Antecedentes, "Antecedentes"),
            Esterilizada = d.Esterilizada
        };
        var e = new EventoMedico
        {
            HistorialClinicoId = h.Id,
            VeterinarioId = a.Id,
            Fecha = Now,
            Tipo = "Inicial",
            Descripcion = h.EstadoGeneral,
            Tratamiento = h.Antecedentes
        };
        await Save(a, "CU-03 Historial", h.Id, "Registro inicial", new Cambio(h, true), new Cambio(e, true));
        return h.ToDto();
    }

    private async Task ClinicalAccess(Actor a, Guid pet)
    {
        if (a.Rol is "Administrador" or "Veterinario")
            return;
        if (a.Rol == "Adoptante" && await repo.AnyAsync<SolicitudDeAdopcion>(x => x.MascotaId == pet && x.AdoptanteId == a.AdoptanteId && x.Estado == EstadoSolicitudEnum.Adoptada))
            return;
        throw new AccessException("El historial es accesible al veterinario, administración y adoptante al finalizar.");
    }

    public async Task<HistorialDetalleResponseDTO> HistoryAsync(Actor a, Guid pet)
    {
        await ClinicalAccess(a, pet);
        var h = await repo.FirstAsync<HistorialClinico>(x => x.MascotaId == pet)
            ?? await repo.FirstAsync<HistorialClinico>(x => x.OrigenMascotaId == pet)
            ?? throw new NotFoundException("Historial inexistente.");
        return new HistorialDetalleResponseDTO(h.ToDto(), (await repo.ListAsync<EventoMedico>(x => x.HistorialClinicoId == h.Id)).OrderBy(x => x.Fecha).Select(x => x.ToDto()).ToList());
    }

    public async Task<EventoMedicoResponseDTO> MedicalEventAsync(Actor a, Guid pet, EventoMedicoDTO d)
    {
        Require(a, "Veterinario");
        var h = await repo.FirstAsync<HistorialClinico>(x => x.MascotaId == pet) ?? throw new NotFoundException("Creá primero el historial inicial.");
        var e = new EventoMedico
        {
            HistorialClinicoId = h.Id,
            VeterinarioId = a.Id,
            Fecha = Now,
            Tipo = Text(d.Tipo, "Tipo", BusinessRules.MaxLongitudTipoEventoMedico),
            Descripcion = Text(d.Descripcion, "Descripción"),
            Tratamiento = Text(d.Tratamiento, "Tratamiento")
        };
        await Save(a, "CU-10 Evento médico", e.Id, e.Tipo, new Cambio(e, true));
        return e.ToDto();
    }

    public async Task<IncidenteResponseDTO> IncidentAsync(Actor a, IncidenteDTO d)
    {
        var s = await Find<SolicitudDeAdopcion>(d.SolicitudId);
        Assigned(a, s);
        if (s.Estado != EstadoSolicitudEnum.EnPeriodoDePrueba)
            throw new ConflictException("El incidente requiere un proceso en prueba.");
        if (d.Gravedad is not ("Normal" or "Urgente"))
            throw new ValidationException("Gravedad inválida.");
        var i = new Incidente
        {
            SolicitudId = s.Id,
            VoluntarioId = a.Id,
            Fecha = Now,
            Gravedad = d.Gravedad,
            Detalle = Text(d.Detalle, "Detalle")
        };
        s.UltimaActividad = Now;
        await Save(a, "CU-18 Incidente", i.Id, i.Detalle, new Cambio(i, true), new Cambio(s), Notice(i.Id, d.Gravedad + ": " + i.Detalle, "Administrador"));
        return i.ToDto();
    }

    public async Task<List<IncidenteResponseDTO>> IncidentsAsync(Actor a)
    {
        Require(a, "Administrador", "Voluntario");
        var incidents = a.Rol == "Administrador"
            ? await repo.ListAsync<Incidente>()
            : await repo.ListAsync<Incidente>(x => x.VoluntarioId == a.Id);
        return incidents.OrderByDescending(x => x.Gravedad == "Urgente").ThenByDescending(x => x.Fecha).Select(x => x.ToDto()).ToList();
    }

    public async Task<List<AuditoriaResponseDTO>> AuditAsync(Actor a, DateTime? desde, DateTime? hasta, string? accion, Guid? usuario)
    {
        Require(a, "Administrador");
        if (desde > hasta)
            throw new ValidationException("Rango inválido.");
        var eventos = await repo.ListAsync<EventoAuditoria>(x =>
            (desde == null || x.Fecha >= desde) &&
            (hasta == null || x.Fecha <= hasta) &&
            (usuario == null || x.UsuarioId == usuario));
        return eventos
            .Where(x => string.IsNullOrWhiteSpace(accion) || x.Accion.Contains(accion, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Fecha).Select(x => x.ToDto()).ToList();
    }

    public async Task<List<NotificacionResponseDTO>> NotificationsAsync(Actor a) => (await repo.ListAsync<Notificacion>(x => x.Canal == "Interna" && (x.UsuarioId == a.Id || x.Rol == a.Rol))).OrderByDescending(x => x.Fecha).Select(x => x.ToDto()).ToList();
    public async Task<List<string[]>> ReportAsync(Actor a, string tipo, DateTime desde, DateTime hasta)
    {
        Require(a, "Administrador");
        if (desde > hasta)
            throw new ValidationException("Rango de fechas inválido.");
        var rows = new List<string[]>();
        if (tipo is "solicitudes" or "adopciones")
        {
            rows.Add(["ID", "Estado", "Mascota", "Adoptante", "Fecha"]);
            if (tipo == "adopciones")
            {
                foreach (var s in await repo.ListAsync<SolicitudDeAdopcion>(x => x.Estado == EstadoSolicitudEnum.Adoptada && x.FechaFinalizacion >= desde && x.FechaFinalizacion <= hasta))
                    rows.Add([s.Id.ToString(), s.Estado.ToString(), s.MascotaId.ToString(), s.AdoptanteId.ToString(), s.FechaFinalizacion!.Value.ToString("O")]);
            }
            else
            {
                foreach (var s in await repo.ListAsync<SolicitudDeAdopcion>(x => x.FechaSolicitud >= desde && x.FechaSolicitud <= hasta))
                    rows.Add([s.Id.ToString(), s.Estado.ToString(), s.MascotaId.ToString(), s.AdoptanteId.ToString(), s.FechaSolicitud.ToString("O")]);
            }
        }
        else if (tipo == "mascotas")
        {
            rows.Add(["ID", "Nombre", "Estado", "Especie", "Fecha"]);
            foreach (var m in await repo.ListAsync<Mascota>(x => x.FechaRegistro >= desde && x.FechaRegistro <= hasta))
                rows.Add([m.Id.ToString(), m.Nombre, m.Estado.ToString(), m.Especie.ToString(), m.FechaRegistro.ToString("O")]);
        }
        else
            throw new ValidationException("Tipo: solicitudes, adopciones o mascotas.");
        await Save(a, "CU-16 Reporte", Guid.Empty, tipo);
        return rows;
    }
}
