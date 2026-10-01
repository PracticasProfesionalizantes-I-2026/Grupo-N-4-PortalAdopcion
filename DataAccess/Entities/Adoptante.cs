namespace PortalAdopcion.DataAccess.Entities;

public class Adoptante
{
    public Guid Id { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Dni { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }

    public ICollection<SolicitudDeAdopcion> Solicitudes { get; set; } = new List<SolicitudDeAdopcion>();
}