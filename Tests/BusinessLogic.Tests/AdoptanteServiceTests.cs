namespace BusinessLogic.Tests;

using Moq;
using PortalAdopcion.BusinessLogic.Services;
using PortalAdopcion.DataAccess.Entities;
using PortalAdopcion.DataAccess.Repositories;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Exceptions;

public class AdoptanteServiceTests
{
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

    private static AdoptanteCreateDTO DtoValido() => new(
        "Ana Garcia",
        "ana.garcia@mail.com",
        "30111222",
        "1144556677",
        "Av. Corrientes 1234");

    [Fact]
    public async Task GetAll_ConDatos_RetornaListaDeResponse()
    {
        var repo = new Mock<IAdoptanteRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Adoptante> { AdoptanteValido(), AdoptanteValido() });
        var service = new AdoptanteService(repo.Object);

        var resultado = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, resultado.Count());
    }

    [Fact]
    public async Task GetById_Existente_RetornaResponse()
    {
        var repo = new Mock<IAdoptanteRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdoptanteValido());
        var service = new AdoptanteService(repo.Object);

        var resultado = await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal("Ana Garcia", resultado.NombreCompleto);
    }

    [Fact]
    public async Task GetById_Inexistente_LanzaNotFoundException()
    {
        var repo = new Mock<IAdoptanteRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Adoptante?)null);
        var service = new AdoptanteService(repo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Create_DatosValidos_RetornaResponse()
    {
        var repo = new Mock<IAdoptanteRepository>();
        repo.Setup(r => r.EmailExisteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.DniExisteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.CreateAsync(It.IsAny<Adoptante>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Adoptante a, CancellationToken _ct) => a);
        var service = new AdoptanteService(repo.Object);

        var resultado = await service.CreateAsync(DtoValido(), CancellationToken.None);

        Assert.Equal("ana.garcia@mail.com", resultado.Email);
        Assert.Equal("30111222", resultado.Dni);
    }

    [Fact]
    public async Task Create_EmailDuplicado_LanzaConflictException()
    {
        var repo = new Mock<IAdoptanteRepository>();
        repo.Setup(r => r.EmailExisteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new AdoptanteService(repo.Object);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(DtoValido(), CancellationToken.None));

        repo.Verify(r => r.CreateAsync(It.IsAny<Adoptante>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_DniDuplicado_LanzaConflictException()
    {
        var repo = new Mock<IAdoptanteRepository>();
        repo.Setup(r => r.EmailExisteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.DniExisteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new AdoptanteService(repo.Object);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(DtoValido(), CancellationToken.None));

        repo.Verify(r => r.CreateAsync(It.IsAny<Adoptante>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_EmailInvalido_LanzaValidationException()
    {
        var repo = new Mock<IAdoptanteRepository>();
        var service = new AdoptanteService(repo.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(DtoValido() with { Email = "email-sin-arroba" }, CancellationToken.None));

        repo.Verify(r => r.CreateAsync(It.IsAny<Adoptante>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_NombreVacio_LanzaValidationException()
    {
        var repo = new Mock<IAdoptanteRepository>();
        var service = new AdoptanteService(repo.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(DtoValido() with { NombreCompleto = " " }, CancellationToken.None));

        repo.Verify(r => r.CreateAsync(It.IsAny<Adoptante>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_Existente_RetornaResponseActualizado()
    {
        var repo = new Mock<IAdoptanteRepository>();
        var actualizado = AdoptanteValido();
        actualizado.Telefono = "1199887766";
        repo.Setup(r => r.EmailExisteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.DniExisteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Adoptante>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(actualizado);
        var service = new AdoptanteService(repo.Object);

        var dto = new AdoptanteUpdateDTO("Ana Garcia", "ana.garcia@mail.com", "30111222", "1199887766", "Av. Corrientes 1234");
        var resultado = await service.UpdateAsync(actualizado.Id, dto, CancellationToken.None);

        Assert.Equal("1199887766", resultado.Telefono);
    }

    [Fact]
    public async Task Update_EmailYaUsadoPorOtro_LanzaConflictException()
    {
        var repo = new Mock<IAdoptanteRepository>();
        repo.Setup(r => r.EmailExisteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new AdoptanteService(repo.Object);

        var dto = new AdoptanteUpdateDTO("Ana Garcia", "otro@mail.com", "30111222", "1199887766", "Av. Corrientes 1234");
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(Guid.NewGuid(), dto, CancellationToken.None));

        repo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Adoptante>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_Inexistente_LanzaNotFoundException()
    {
        var repo = new Mock<IAdoptanteRepository>();
        repo.Setup(r => r.EmailExisteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.DniExisteAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Adoptante>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Adoptante?)null);
        var service = new AdoptanteService(repo.Object);

        var dto = new AdoptanteUpdateDTO("Ana Garcia", "ana.garcia@mail.com", "30111222", "1199887766", "Av. Corrientes 1234");
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), dto, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_ConSolicitudesAsociadas_LanzaConflictException()
    {
        var repo = new Mock<IAdoptanteRepository>();
        repo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.HasSolicitudesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new AdoptanteService(repo.Object);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.DeleteAsync(Guid.NewGuid(), CancellationToken.None));

        repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}