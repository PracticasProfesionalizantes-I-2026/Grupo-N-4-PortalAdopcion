namespace BusinessLogic.Tests;

using Moq;
using PortalAdopcion.BusinessLogic.Services;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Enums;
using PortalAdopcion.Shared.Exceptions;

public class MascotaServiceTests
{
    private static Mascota MascotaValida() => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Tom",
        Especie = EspecieEnum.Gato,
        Raza = "Comun",
        Edad = 2,
        Sexo = SexoEnum.Macho,
        Estado = EstadoMascotaEnum.Disponible,
        FechaRegistro = DateTime.UtcNow
    };

    private static MascotaCreateDTO DtoValido() => new("Luna", EspecieEnum.Perro, "Labrador", 1, SexoEnum.Hembra);

    [Fact]
    public async Task GetAll_ConDatos_RetornaListaDeResponse()
    {
        var repo = new Mock<IMascotaRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Mascota> { MascotaValida(), MascotaValida() });
        var service = new MascotaService(repo.Object);

        var resultado = await service.GetAllAsync(null, CancellationToken.None);

        Assert.Equal(2, resultado.Count());
    }

    [Fact]
    public async Task GetAll_ConFiltroDeEstado_FiltraResultados()
    {
        var repo = new Mock<IMascotaRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Mascota>
            {
                new Mascota { Id = Guid.NewGuid(), Nombre = "A", Especie = EspecieEnum.Gato, Edad = 1, Sexo = SexoEnum.Macho, Estado = EstadoMascotaEnum.Disponible },
                new Mascota { Id = Guid.NewGuid(), Nombre = "B", Especie = EspecieEnum.Perro, Edad = 3, Sexo = SexoEnum.Hembra, Estado = EstadoMascotaEnum.Adoptada }
            });
        var service = new MascotaService(repo.Object);

        var resultado = await service.GetAllAsync(EstadoMascotaEnum.Disponible, CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal("A", resultado.First().Nombre);
    }

    [Fact]
    public async Task GetById_Existente_RetornaResponse()
    {
        var repo = new Mock<IMascotaRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MascotaValida());
        var service = new MascotaService(repo.Object);

        var resultado = await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.NotNull(resultado);
        Assert.Equal("Tom", resultado.Nombre);
    }

    [Fact]
    public async Task GetById_Inexistente_LanzaNotFoundException()
    {
        var repo = new Mock<IMascotaRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mascota?)null);
        var service = new MascotaService(repo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Create_DatosValidos_RetornaResponse()
    {
        var repo = new Mock<IMascotaRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<Mascota>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mascota m, CancellationToken _ct) => m);
        var service = new MascotaService(repo.Object);

        var resultado = await service.CreateAsync(DtoValido(), CancellationToken.None);

        Assert.Equal("Luna", resultado.Nombre);
        Assert.Equal(EstadoMascotaEnum.Disponible, resultado.Estado);
        repo.Verify(r => r.CreateAsync(It.IsAny<Mascota>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_NombreVacio_LanzaValidationException()
    {
        var repo = new Mock<IMascotaRepository>();
        var service = new MascotaService(repo.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(DtoValido() with { Nombre = "   " }, CancellationToken.None));

        repo.Verify(r => r.CreateAsync(It.IsAny<Mascota>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_EdadFueraDeRango_LanzaValidationException()
    {
        var repo = new Mock<IMascotaRepository>();
        var service = new MascotaService(repo.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(DtoValido() with { Edad = 45 }, CancellationToken.None));

        repo.Verify(r => r.CreateAsync(It.IsAny<Mascota>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_Existente_RetornaResponseActualizado()
    {
        var repo = new Mock<IMascotaRepository>();
        var actualizada = MascotaValida();
        actualizada.Nombre = "Rex";
        repo.Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Mascota>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(actualizada);
        var service = new MascotaService(repo.Object);

        var dto = new MascotaUpdateDTO("Rex", EspecieEnum.Perro, "Ovejero", 4, SexoEnum.Macho, EstadoMascotaEnum.Disponible);
        var resultado = await service.UpdateAsync(actualizada.Id, dto, CancellationToken.None);

        Assert.Equal("Rex", resultado.Nombre);
    }

    [Fact]
    public async Task Update_Inexistente_LanzaNotFoundException()
    {
        var repo = new Mock<IMascotaRepository>();
        repo.Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Mascota>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mascota?)null);
        var service = new MascotaService(repo.Object);

        var dto = new MascotaUpdateDTO("Rex", EspecieEnum.Perro, "Ovejero", 4, SexoEnum.Macho, EstadoMascotaEnum.Disponible);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), dto, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_Existente_Elimina()
    {
        var repo = new Mock<IMascotaRepository>();
        repo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.HasSolicitudesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var service = new MascotaService(repo.Object);

        await service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ConSolicitudesAsociadas_LanzaConflictException()
    {
        var repo = new Mock<IMascotaRepository>();
        repo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.HasSolicitudesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new MascotaService(repo.Object);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.DeleteAsync(Guid.NewGuid(), CancellationToken.None));

        repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_Inexistente_LanzaNotFoundException()
    {
        var repo = new Mock<IMascotaRepository>();
        repo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var service = new MascotaService(repo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.DeleteAsync(Guid.NewGuid(), CancellationToken.None));

        repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}