namespace BusinessLogic.Tests;

using Moq;
using PortalAdopcion.BusinessLogic.Services;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;

public class SolicitudServiceTests
{
    private static Mascota MascotaDisponible() => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Tom",
        Especie = EspecieEnum.Gato,
        Edad = 2,
        Sexo = SexoEnum.Macho,
        Estado = EstadoMascotaEnum.Disponible,
        FechaRegistro = DateTime.UtcNow
    };

    private static Adoptante AdoptanteValido() => new()
    {
        Id = Guid.NewGuid(),
        NombreCompleto = "Ana Garcia",
        Email = "ana.garcia@mail.com",
        Dni = "30111222",
        Telefono = "1144556677",
        Direccion = "Av. Corrientes 1234",
        FechaRegistro = DateTime.UtcNow
    };

    private static SolicitudDeAdopcion SolicitudPendiente() => new()
    {
        Id = Guid.NewGuid(),
        MascotaId = Guid.NewGuid(),
        AdoptanteId = Guid.NewGuid(),
        FechaSolicitud = DateTime.UtcNow,
        Estado = EstadoSolicitudEnum.Pendiente
    };

    [Fact]
    public async Task Create_MascotaInexistente_LanzaNotFoundException()
    {
        var solRepo = new Mock<ISolicitudRepository>();
        var mascRepo = new Mock<IMascotaRepository>();
        mascRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Mascota?)null);
        var adopRepo = new Mock<IAdoptanteRepository>();
        var service = new SolicitudService(solRepo.Object, mascRepo.Object, adopRepo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(new SolicitudCreateDTO(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        solRepo.Verify(r => r.CreateAsync(It.IsAny<SolicitudDeAdopcion>(), It.IsAny<EstadoMascotaEnum>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_AdoptanteInexistente_LanzaNotFoundException()
    {
        var solRepo = new Mock<ISolicitudRepository>();
        var mascRepo = new Mock<IMascotaRepository>();
        mascRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(MascotaDisponible());
        var adopRepo = new Mock<IAdoptanteRepository>();
        adopRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Adoptante?)null);
        var service = new SolicitudService(solRepo.Object, mascRepo.Object, adopRepo.Object);

        var dto = new SolicitudCreateDTO(Guid.NewGuid(), Guid.NewGuid());
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_MascotaNoDisponible_LanzaConflictException()
    {
        var solRepo = new Mock<ISolicitudRepository>();
        var mascRepo = new Mock<IMascotaRepository>();
        var reservada = MascotaDisponible();
        reservada.Estado = EstadoMascotaEnum.Reservada;
        mascRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(reservada);
        var adopRepo = new Mock<IAdoptanteRepository>();
        adopRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(AdoptanteValido());
        var service = new SolicitudService(solRepo.Object, mascRepo.Object, adopRepo.Object);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new SolicitudCreateDTO(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        solRepo.Verify(r => r.CreateAsync(It.IsAny<SolicitudDeAdopcion>(), It.IsAny<EstadoMascotaEnum>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_MascotaConSolicitudActiva_LanzaConflictException()
    {
        var solRepo = new Mock<ISolicitudRepository>();
        solRepo.Setup(r => r.GetActivaPorMascotaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SolicitudPendiente());
        var mascRepo = new Mock<IMascotaRepository>();
        mascRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(MascotaDisponible());
        var adopRepo = new Mock<IAdoptanteRepository>();
        adopRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(AdoptanteValido());
        var service = new SolicitudService(solRepo.Object, mascRepo.Object, adopRepo.Object);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new SolicitudCreateDTO(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        solRepo.Verify(r => r.CreateAsync(It.IsAny<SolicitudDeAdopcion>(), It.IsAny<EstadoMascotaEnum>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_AdoptanteConSolicitudActiva_LanzaConflictException()
    {
        var solRepo = new Mock<ISolicitudRepository>();
        solRepo.Setup(r => r.GetActivaPorMascotaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SolicitudDeAdopcion?)null);
        solRepo.Setup(r => r.TieneActivaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var mascRepo = new Mock<IMascotaRepository>();
        mascRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(MascotaDisponible());
        var adopRepo = new Mock<IAdoptanteRepository>();
        adopRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(AdoptanteValido());
        var service = new SolicitudService(solRepo.Object, mascRepo.Object, adopRepo.Object);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new SolicitudCreateDTO(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        solRepo.Verify(r => r.CreateAsync(It.IsAny<SolicitudDeAdopcion>(), It.IsAny<EstadoMascotaEnum>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ReglasOk_CreaSolicitudYReservaMascota()
    {
        var mascotaId = Guid.NewGuid();
        var adoptanteId = Guid.NewGuid();
        var solRepo = new Mock<ISolicitudRepository>();
        solRepo.Setup(r => r.GetActivaPorMascotaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SolicitudDeAdopcion?)null);
        solRepo.Setup(r => r.TieneActivaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        solRepo.Setup(r => r.CreateAsync(It.IsAny<SolicitudDeAdopcion>(), EstadoMascotaEnum.Reservada, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SolicitudDeAdopcion s, EstadoMascotaEnum _e, CancellationToken _ct) =>
            {
                s.Id = Guid.NewGuid();
                s.FechaSolicitud = DateTime.UtcNow;
                return s;
            });
        var mascRepo = new Mock<IMascotaRepository>();
        mascRepo.Setup(r => r.GetByIdAsync(mascotaId, It.IsAny<CancellationToken>())).ReturnsAsync(MascotaDisponible());
        var adopRepo = new Mock<IAdoptanteRepository>();
        adopRepo.Setup(r => r.GetByIdAsync(adoptanteId, It.IsAny<CancellationToken>())).ReturnsAsync(AdoptanteValido());
        var service = new SolicitudService(solRepo.Object, mascRepo.Object, adopRepo.Object);

        var resultado = await service.CreateAsync(new SolicitudCreateDTO(mascotaId, adoptanteId), CancellationToken.None);

        Assert.Equal(EstadoSolicitudEnum.Pendiente, resultado.Estado);
        solRepo.Verify(r => r.CreateAsync(
            It.Is<SolicitudDeAdopcion>(s => s.MascotaId == mascotaId && s.AdoptanteId == adoptanteId),
            EstadoMascotaEnum.Reservada,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_Inexistente_LanzaNotFoundException()
    {
        var solRepo = new Mock<ISolicitudRepository>();
        solRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SolicitudDeAdopcion?)null);
        var service = new SolicitudService(solRepo.Object, new Mock<IMascotaRepository>().Object, new Mock<IAdoptanteRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateEstado_Aprobar_RetornaAprobada()
    {
        var id = Guid.NewGuid();
        var solRepo = new Mock<ISolicitudRepository>();
        var pendiente = SolicitudPendiente();
        pendiente.Id = id;
        solRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(pendiente);
        solRepo.Setup(r => r.UpdateEstadoAsync(id, EstadoSolicitudEnum.Aprobada, null, EstadoMascotaEnum.Adoptada, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SolicitudDeAdopcion { Id = id, Estado = EstadoSolicitudEnum.Aprobada, FechaSolicitud = DateTime.UtcNow });
        var service = new SolicitudService(solRepo.Object, new Mock<IMascotaRepository>().Object, new Mock<IAdoptanteRepository>().Object);

        var resultado = await service.UpdateEstadoAsync(id, new SolicitudUpdateEstadoDTO(EstadoSolicitudEnum.Aprobada, null), CancellationToken.None);

        Assert.Equal(EstadoSolicitudEnum.Aprobada, resultado.Estado);
        solRepo.Verify(r => r.UpdateEstadoAsync(id, EstadoSolicitudEnum.Aprobada, null, EstadoMascotaEnum.Adoptada, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEstado_Rechazar_SinMotivo_LanzaValidationException()
    {
        var id = Guid.NewGuid();
        var solRepo = new Mock<ISolicitudRepository>();
        var pendiente = SolicitudPendiente();
        pendiente.Id = id;
        solRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(pendiente);
        var service = new SolicitudService(solRepo.Object, new Mock<IMascotaRepository>().Object, new Mock<IAdoptanteRepository>().Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateEstadoAsync(id, new SolicitudUpdateEstadoDTO(EstadoSolicitudEnum.Rechazada, null), CancellationToken.None));

        solRepo.Verify(r => r.UpdateEstadoAsync(It.IsAny<Guid>(), It.IsAny<EstadoSolicitudEnum>(), It.IsAny<string?>(), It.IsAny<EstadoMascotaEnum?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEstado_Cancelar_SinMotivo_LanzaValidationException()
    {
        var id = Guid.NewGuid();
        var solRepo = new Mock<ISolicitudRepository>();
        var pendiente = SolicitudPendiente();
        pendiente.Id = id;
        solRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(pendiente);
        var service = new SolicitudService(solRepo.Object, new Mock<IMascotaRepository>().Object, new Mock<IAdoptanteRepository>().Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateEstadoAsync(id, new SolicitudUpdateEstadoDTO(EstadoSolicitudEnum.Cancelada, "  "), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateEstado_Rechazar_ConMotivo_DevuelveRespuesta()
    {
        var id = Guid.NewGuid();
        var solRepo = new Mock<ISolicitudRepository>();
        var pendiente = SolicitudPendiente();
        pendiente.Id = id;
        solRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(pendiente);
        solRepo.Setup(r => r.UpdateEstadoAsync(id, EstadoSolicitudEnum.Rechazada, "El adoptante no cumple los requisitos", EstadoMascotaEnum.Disponible, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SolicitudDeAdopcion { Id = id, Estado = EstadoSolicitudEnum.Rechazada, Motivo = "El adoptante no cumple los requisitos", FechaSolicitud = DateTime.UtcNow });
        var service = new SolicitudService(solRepo.Object, new Mock<IMascotaRepository>().Object, new Mock<IAdoptanteRepository>().Object);

        var dto = new SolicitudUpdateEstadoDTO(EstadoSolicitudEnum.Rechazada, "El adoptante no cumple los requisitos");
        var resultado = await service.UpdateEstadoAsync(id, dto, CancellationToken.None);

        Assert.Equal(EstadoSolicitudEnum.Rechazada, resultado.Estado);
        Assert.Equal("El adoptante no cumple los requisitos", resultado.Motivo);
        solRepo.Verify(r => r.UpdateEstadoAsync(id, EstadoSolicitudEnum.Rechazada, "El adoptante no cumple los requisitos", EstadoMascotaEnum.Disponible, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEstado_SolicitudNoPendiente_LanzaValidationException()
    {
        var id = Guid.NewGuid();
        var solRepo = new Mock<ISolicitudRepository>();
        var aprobada = SolicitudPendiente();
        aprobada.Id = id;
        aprobada.Estado = EstadoSolicitudEnum.Aprobada;
        solRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(aprobada);
        var service = new SolicitudService(solRepo.Object, new Mock<IMascotaRepository>().Object, new Mock<IAdoptanteRepository>().Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateEstadoAsync(id, new SolicitudUpdateEstadoDTO(EstadoSolicitudEnum.Rechazada, "motivo"), CancellationToken.None));

        solRepo.Verify(r => r.UpdateEstadoAsync(It.IsAny<Guid>(), It.IsAny<EstadoSolicitudEnum>(), It.IsAny<string?>(), It.IsAny<EstadoMascotaEnum?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEstado_SolicitudInexistente_LanzaNotFoundException()
    {
        var solRepo = new Mock<ISolicitudRepository>();
        solRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SolicitudDeAdopcion?)null);
        var service = new SolicitudService(solRepo.Object, new Mock<IMascotaRepository>().Object, new Mock<IAdoptanteRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateEstadoAsync(Guid.NewGuid(), new SolicitudUpdateEstadoDTO(EstadoSolicitudEnum.Cancelada, "motivo"), CancellationToken.None));
    }
}