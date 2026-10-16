namespace Portal.Tests;
using System.Linq.Expressions;
using Moq;
using PortalAdopcion.BusinessLogic.Services;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;
using Xunit;

public class BusinessRulesUnitTests
{
    private sealed class Harness
    {
        public readonly FakeClock Clock = new(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));
        public readonly List<Registro> Store = new();
        public readonly List<EventoAuditoria> Audits = new();
        public readonly Mock<IPortalRepository> Repo = new();
        public readonly PortalService Service;

        public Harness()
        {
            Setup<Usuario>();
            Setup<Adoptante>();
            Setup<Mascota>();
            Setup<SolicitudDeAdopcion>();
            Setup<SancionBlacklist>();
            Setup<Entrevista>();
            Setup<Seguimiento>();
            Setup<HistorialClinico>();
            Setup<EventoMedico>();
            Setup<Incidente>();
            Setup<EventoAuditoria>();
            Setup<Notificacion>();

            Repo.Setup(r => r.SaveAsync(It.IsAny<IEnumerable<Cambio>>(), It.IsAny<EventoAuditoria>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<Cambio> changes, EventoAuditoria audit, CancellationToken _) =>
                {
                    Audits.Add(audit);
                    foreach (var change in changes.ToList())
                        if (change.Nuevo && !Store.Contains(change.Entidad))
                            Store.Add(change.Entidad);
                })
                .Returns(Task.CompletedTask);

            Service = new PortalService(Repo.Object, Clock);
        }

        private void Setup<T>()
            where T : Registro
        {
            Repo.Setup(r => r.ListAsync<T>(It.IsAny<CancellationToken>())).ReturnsAsync(() => Store.OfType<T>().ToList());
            Repo.Setup(r => r.ListAsync<T>(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<T, bool>> p, CancellationToken _) => Store.OfType<T>().AsQueryable().Where(p).ToList());
            Repo.Setup(r => r.AnyAsync<T>(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<T, bool>> p, CancellationToken _) => Store.OfType<T>().AsQueryable().Any(p));
            Repo.Setup(r => r.CountAsync<T>(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<T, bool>> p, CancellationToken _) => Store.OfType<T>().AsQueryable().Count(p));
            Repo.Setup(r => r.FirstAsync<T>(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<T, bool>> p, CancellationToken _) => Store.OfType<T>().AsQueryable().FirstOrDefault(p));
            Repo.Setup(r => r.PageAsync<T>(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<T, bool>> p, int page, int size, CancellationToken _) =>
                {
                    var all = Store.OfType<T>().AsQueryable().Where(p).ToList();
                    return new PagedResult<T>(all.OrderBy(x => x.Id).Skip((page - 1) * size).Take(size).ToList(), all.Count, page, size);
                });
            Repo.Setup(r => r.FindAsync<T>(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid id, CancellationToken _) => Store.OfType<T>().FirstOrDefault(x => x.Id == id));
        }

        public Actor AddUser(string rol, Guid? adoptanteId = null, string estado = "Activa")
        {
            var u = new Usuario { Nombre = rol, Email = $"{Guid.NewGuid():N}@demo.local", Dni = Guid.NewGuid().ToString("N")[..8], Rol = rol, Estado = estado, AdoptanteId = adoptanteId };
            Store.Add(u);
            return new Actor(u.Id, u.Rol, u.AdoptanteId);
        }

        public Adoptante AddAdoptante(bool baja = false, int edad = 30)
        {
            var a = new Adoptante { NombreCompleto = "Ana", Email = $"{Guid.NewGuid():N}@demo.local", Dni = Guid.NewGuid().ToString("N")[..8], Telefono = "1", Direccion = "Casa", Edad = edad, EstadoCivil = "Soltero/a", DatosLaborales = "Empleado", Baja = baja };
            Store.Add(a);
            return a;
        }

        public Mascota AddMascota(EstadoMascotaEnum estado = EstadoMascotaEnum.Disponible)
        {
            var m = new Mascota { Nombre = "Firulais", Especie = EspecieEnum.Perro, Edad = 2, Sexo = SexoEnum.Macho, Estado = estado };
            Store.Add(m);
            return m;
        }

        public SolicitudDeAdopcion AddSolicitud(Guid mascotaId, Guid adoptanteId, EstadoSolicitudEnum estado, Guid? voluntarioId = null, DateTime? inicioPrueba = null, bool alerta = false)
        {
            var s = new SolicitudDeAdopcion { MascotaId = mascotaId, AdoptanteId = adoptanteId, Estado = estado, VoluntarioId = voluntarioId, InicioPrueba = inicioPrueba, Alerta = alerta, FechaSolicitud = Clock.Now, UltimaActividad = Clock.Now, MotivoAdopcion = "Quiero adoptar" };
            Store.Add(s);
            return s;
        }

        public (Adoptante Perfil, Actor Actor) ValidAdopter(bool baja = false)
        {
            var p = AddAdoptante(baja);
            var a = AddUser("Adoptante", p.Id);
            return (p, a);
        }
    }

    // ---------- Registro / seguridad ----------

    [Fact]
    public async Task Register_WhenPasswordIsWeak_ThrowsValidation()
    {
        // Arrange
        var h = new Harness();
        var request = new RegistroUsuarioDTO("Ana", "ana@demo.local", "12345678", "abc", "1", "Casa", 30, "Soltero/a", "Empleado");

        // Act
        var action = () => h.Service.RegisterAsync(request);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(action);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ThrowsConflict()
    {
        // Arrange
        var h = new Harness();
        var existing = h.AddAdoptante();
        h.AddUser("Adoptante", existing.Id);
        var request = new RegistroUsuarioDTO("Ana", existing.Email, "99999999", "ClaveSegura2026!", "1", "Casa", 30, "Soltero/a", "Empleado");

        // Act
        var action = () => h.Service.RegisterAsync(request);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
    }

    // ---------- Validaciones de identidad (DNI / Teléfono / Estado civil) ----------

    [Fact]
    public async Task Register_WhenDniHasLetters_ThrowsValidation()
    {
        var h = new Harness();
        var request = new RegistroUsuarioDTO("Ana", "ana@demo.local", "12AB34", "ClaveSegura2026!", "3493000000", "Casa", 30, "Soltero/a", "Empleado");

        var action = () => h.Service.RegisterAsync(request);

        await Assert.ThrowsAsync<ValidationException>(action);
    }

    [Fact]
    public async Task Register_WhenTelefonoHasLetters_ThrowsValidation()
    {
        var h = new Harness();
        var request = new RegistroUsuarioDTO("Ana", "ana@demo.local", "30111222", "ClaveSegura2026!", "abc123", "Casa", 30, "Soltero/a", "Empleado");

        var action = () => h.Service.RegisterAsync(request);

        await Assert.ThrowsAsync<ValidationException>(action);
    }

    [Fact]
    public async Task Register_WhenEstadoCivilIsNotInTheAllowedSet_ThrowsValidation()
    {
        var h = new Harness();
        var request = new RegistroUsuarioDTO("Ana", "ana@demo.local", "30111222", "ClaveSegura2026!", "3493000000", "Casa", 30, "Viudo", "Empleado");

        var action = () => h.Service.RegisterAsync(request);

        await Assert.ThrowsAsync<ValidationException>(action);
    }

    [Fact]
    public async Task CreateProfile_WhenTelefonoHasLetters_ThrowsValidation()
    {
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var dto = new PerfilDTO("Ana", "ana.perfil@demo.local", "30111222", "abc123", "Casa", 30, "Soltero/a", "Empleado");

        var action = () => h.Service.CreateProfileAsync(admin, dto);

        await Assert.ThrowsAsync<ValidationException>(action);
    }

    // ---------- Postulación ----------

    [Fact]
    public async Task Postulate_WhenPetNotAvailable_ThrowsConflict()
    {
        // Arrange
        var h = new Harness();
        var (profile, actor) = h.ValidAdopter();
        var pet = h.AddMascota(EstadoMascotaEnum.Adoptada);

        // Act
        var action = () => h.Service.PostulateAsync(actor, new PostulacionDTO(pet.Id, "Dar un hogar", null));

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task Postulate_WhenProfileIsBlacklisted_ThrowsAccess()
    {
        // Arrange
        var h = new Harness();
        var (profile, actor) = h.ValidAdopter();
        var pet = h.AddMascota();
        h.Store.Add(new SancionBlacklist { AdoptanteId = profile.Id, Motivo = "Sanción", Desde = h.Clock.Now.AddDays(-1), Hasta = h.Clock.Now.AddDays(30), Revocada = false });

        // Act
        var action = () => h.Service.PostulateAsync(actor, new PostulacionDTO(pet.Id, "Dar un hogar", null));

        // Assert
        await Assert.ThrowsAsync<AccessException>(action);
    }

    [Fact]
    public async Task Postulate_WhenAdopterHasActiveProcess_ThrowsConflict()
    {
        // Arrange
        var h = new Harness();
        var (profile, actor) = h.ValidAdopter();
        var pet = h.AddMascota();
        var otherPet = h.AddMascota(EstadoMascotaEnum.Reservada);
        h.AddSolicitud(otherPet.Id, profile.Id, EstadoSolicitudEnum.Pendiente);

        // Act
        var action = () => h.Service.PostulateAsync(actor, new PostulacionDTO(pet.Id, "Dar un hogar", null));

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task Postulate_WhenValid_ReservesPetAndQueuesNotifications()
    {
        // Arrange
        var h = new Harness();
        var (profile, actor) = h.ValidAdopter();
        var pet = h.AddMascota();

        // Act
        var result = await h.Service.PostulateAsync(actor, new PostulacionDTO(pet.Id, "Dar un hogar", null));

        // Assert
        Assert.Equal(EstadoSolicitudEnum.Pendiente, result.Estado);
        Assert.False(string.IsNullOrWhiteSpace(result.CodigoConfirmacion));
        Assert.Equal(EstadoMascotaEnum.Reservada, h.Store.OfType<Mascota>().Single(x => x.Id == pet.Id).Estado);
        Assert.Contains(h.Store.OfType<Notificacion>(), n => n.Canal == "CorreoBccPendiente" && n.EntidadId == result.Id);
    }

    // ---------- Evaluación ----------

    [Fact]
    public async Task Evaluate_WhenApprovingBeforeFifteenDays_ThrowsConflict()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var pet = h.AddMascota(EstadoMascotaEnum.Reservada);
        var request = h.AddSolicitud(pet.Id, Guid.NewGuid(), EstadoSolicitudEnum.Pendiente);

        // Act
        var action = () => h.Service.EvaluateAsync(admin, request.Id, new EvaluacionDTO(EstadoSolicitudEnum.Aprobada, null, "ONG"));

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task Evaluate_WhenStateIsNotAllowed_ThrowsValidation()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var pet = h.AddMascota(EstadoMascotaEnum.Reservada);
        var request = h.AddSolicitud(pet.Id, Guid.NewGuid(), EstadoSolicitudEnum.Pendiente);

        // Act
        var action = () => h.Service.EvaluateAsync(admin, request.Id, new EvaluacionDTO(EstadoSolicitudEnum.Pendiente, "x", "ONG"));

        // Assert
        await Assert.ThrowsAsync<ValidationException>(action);
    }

    [Fact]
    public async Task Evaluate_WhenRejected_ReleasesPet()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var pet = h.AddMascota(EstadoMascotaEnum.Reservada);
        var request = h.AddSolicitud(pet.Id, Guid.NewGuid(), EstadoSolicitudEnum.Pendiente);

        // Act
        var result = await h.Service.EvaluateAsync(admin, request.Id, new EvaluacionDTO(EstadoSolicitudEnum.Rechazada, "No cumple", "ONG"));

        // Assert
        Assert.Equal(EstadoSolicitudEnum.Rechazada, result.Estado);
        Assert.Equal(EstadoMascotaEnum.Disponible, h.Store.OfType<Mascota>().Single(x => x.Id == pet.Id).Estado);
    }

    // ---------- Finalización ----------

    [Fact]
    public async Task Finalize_WhenTrialShorterThanFifteenDays_ThrowsConflict()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var pet = h.AddMascota(EstadoMascotaEnum.EnPeriodoDePrueba);
        var request = h.AddSolicitud(pet.Id, Guid.NewGuid(), EstadoSolicitudEnum.EnPeriodoDePrueba, inicioPrueba: h.Clock.Now.AddDays(-5));

        // Act
        var action = () => h.Service.FinalizeAsync(admin, request.Id, "Finalizar");

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task Finalize_WhenAlertWithoutVetValidation_ThrowsConflict()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var pet = h.AddMascota(EstadoMascotaEnum.EnPeriodoDePrueba);
        var request = h.AddSolicitud(pet.Id, Guid.NewGuid(), EstadoSolicitudEnum.EnPeriodoDePrueba, inicioPrueba: h.Clock.Now.AddDays(-16), alerta: true);

        // Act
        var action = () => h.Service.FinalizeAsync(admin, request.Id, "Finalizar");

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task Finalize_WhenClinicalHistoryMissing_ThrowsConflict()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var pet = h.AddMascota(EstadoMascotaEnum.EnPeriodoDePrueba);
        var request = h.AddSolicitud(pet.Id, Guid.NewGuid(), EstadoSolicitudEnum.EnPeriodoDePrueba, inicioPrueba: h.Clock.Now.AddDays(-16));

        // Act
        var action = () => h.Service.FinalizeAsync(admin, request.Id, "Finalizar");

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task Finalize_WhenValid_MarksAdoptionAndActivatesPet()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var adopter = h.AddAdoptante();
        h.AddUser("Adoptante", adopter.Id);
        var pet = h.AddMascota(EstadoMascotaEnum.EnPeriodoDePrueba);
        var request = h.AddSolicitud(pet.Id, adopter.Id, EstadoSolicitudEnum.EnPeriodoDePrueba, inicioPrueba: h.Clock.Now.AddDays(-16));
        h.Store.Add(new HistorialClinico { MascotaId = pet.Id, Fecha = h.Clock.Now, EstadoGeneral = "Bien", Vacunas = "Al día", Antecedentes = "Ninguno" });

        // Act
        var result = await h.Service.FinalizeAsync(admin, request.Id, "Fin exitoso");

        // Assert
        Assert.Equal(EstadoSolicitudEnum.Adoptada, result.Estado);
        Assert.Equal(EstadoMascotaEnum.Adoptada, h.Store.OfType<Mascota>().Single(x => x.Id == pet.Id).Estado);
    }

    [Fact]
    public async Task Finalize_WhenValid_TransfersClinicalHistoryToAdopter()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var adopter = h.AddAdoptante();
        h.AddUser("Adoptante", adopter.Id);
        var pet = h.AddMascota(EstadoMascotaEnum.EnPeriodoDePrueba);
        var request = h.AddSolicitud(pet.Id, adopter.Id, EstadoSolicitudEnum.EnPeriodoDePrueba, inicioPrueba: h.Clock.Now.AddDays(-16));
        h.Store.Add(new HistorialClinico { MascotaId = pet.Id, Fecha = h.Clock.Now, EstadoGeneral = "Bien", Vacunas = "Al día", Antecedentes = "Ninguno" });
        h.Store.Add(new EventoMedico { HistorialClinicoId = h.Store.OfType<HistorialClinico>().Single().Id, Fecha = h.Clock.Now, Tipo = "Inicial", Descripcion = "Revisión", Tratamiento = "Ninguno" });

        // Act
        await h.Service.FinalizeAsync(admin, request.Id, "Fin exitoso");

        // Assert
        var historiales = h.Store.OfType<HistorialClinico>().ToList();
        Assert.DoesNotContain(historiales, x => x.MascotaId == pet.Id);
        var transferred = historiales.Single(x => x.AdoptanteId == adopter.Id && x.OrigenMascotaId == pet.Id);
        Assert.Null(transferred.MascotaId);
        Assert.Contains(h.Store.OfType<EventoMedico>(), e => e.HistorialClinicoId == transferred.Id);
    }

    // ---------- Cancelación / permisos ----------

    [Fact]
    public async Task Cancel_WhenRequestBelongsToAnotherAdopter_ThrowsAccess()
    {
        // Arrange
        var h = new Harness();
        var owner = h.AddAdoptante();
        var pet = h.AddMascota(EstadoMascotaEnum.Reservada);
        var request = h.AddSolicitud(pet.Id, owner.Id, EstadoSolicitudEnum.Pendiente);
        var (_, otherAdopter) = h.ValidAdopter();

        // Act
        var action = () => h.Service.CancelAsync(otherAdopter, request.Id, "No corresponde");

        // Assert
        await Assert.ThrowsAsync<AccessException>(action);
    }

    [Fact]
    public async Task CreatePet_WhenActorIsAdopter_ThrowsAccess()
    {
        // Arrange
        var h = new Harness();
        var (_, actor) = h.ValidAdopter();
        var request = new MascotaFichaDTO("Pepe", EspecieEnum.Perro, null, 2, SexoEnum.Macho, null, null);

        // Act
        var action = () => h.Service.CreatePetAsync(actor, request);

        // Assert
        await Assert.ThrowsAsync<AccessException>(action);
    }

    [Fact]
    public async Task ValidateClinical_WhenActorIsNotVet_ThrowsAccess()
    {
        // Arrange
        var h = new Harness();
        var volunteer = h.AddUser("Voluntario");

        // Act
        var action = () => h.Service.ValidateClinicalAsync(volunteer, Guid.NewGuid(), "Apto");

        // Assert
        await Assert.ThrowsAsync<AccessException>(action);
    }

    // ---------- Usuarios / sanciones ----------

    [Fact]
    public async Task CreateStaff_WhenMatriculaDuplicated_ThrowsConflict()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        h.Store.Add(new Usuario { Nombre = "Vet", Email = "vet@demo.local", Dni = "11111111", Rol = "Veterinario", Estado = "Activa", Matricula = "MAT-1" });
        var request = new PersonalDTO("Otro Vet", "otro@demo.local", "22222222", "ClavePrueba2026!", "Veterinario", "MAT-1");

        // Act
        var action = () => h.Service.CreateStaffAsync(admin, request);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task ChangeUser_WhenRemovingLastAdministrator_ThrowsConflict()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var request = new UsuarioCambioDTO("Voluntario", "Activa", null);

        // Act
        var action = () => h.Service.ChangeUserAsync(admin, admin.Id, request);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task SaveBlacklist_WhenDateRangeInvalid_ThrowsValidation()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var adopter = h.AddAdoptante();
        var request = new BlacklistDTO(adopter.Id, "Motivo", h.Clock.Now, h.Clock.Now);

        // Act
        var action = () => h.Service.SaveBlacklistAsync(admin, null, request);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(action);
    }

    // ---------- Agendamiento ----------

    [Fact]
    public async Task Schedule_WhenVolunteerAtProcessLimit_ThrowsConflict()
    {
        // Arrange
        var h = new Harness();
        var admin = h.AddUser("Administrador");
        var volunteer = h.AddUser("Voluntario");
        var pet = h.AddMascota(EstadoMascotaEnum.Reservada);
        var request = h.AddSolicitud(pet.Id, Guid.NewGuid(), EstadoSolicitudEnum.Aprobada);
        h.AddSolicitud(h.AddMascota(EstadoMascotaEnum.EnEntrevista).Id, Guid.NewGuid(), EstadoSolicitudEnum.EnEntrevista, voluntarioId: volunteer.Id);
        h.AddSolicitud(h.AddMascota(EstadoMascotaEnum.EnEntrevista).Id, Guid.NewGuid(), EstadoSolicitudEnum.EnEntrevista, voluntarioId: volunteer.Id);
        var request2 = new EntrevistaDTO(h.Clock.Now.AddDays(1), volunteer.Id);

        // Act
        var action = () => h.Service.ScheduleAsync(admin, request.Id, request2);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
    }
}
