namespace PortalAdopcion.BusinessLogic.Services;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PortalAdopcion.BusinessLogic.Abstractions;
using PortalAdopcion.BusinessLogic.Mapping;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.Constants;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;
using PortalAdopcion.Shared.Security;

// REFACTOR: Extraído de PortalService. Responsabilidad: Registro, login, administración de usuarios/roles y perfiles de adoptantes.
public class UsuarioAuthService(IPortalRepository repository, TimeProvider timeProvider) : ServiceBase(repository, timeProvider), IUsuarioAuthService
{
    private static readonly string[] Roles = ["Administrador", "Voluntario", "Veterinario", "Adoptante"];

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
}
