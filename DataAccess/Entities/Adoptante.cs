namespace PortalAdopcion.DataAccess.Entities;
public class Adoptante : Registro
{
    public string NombreCompleto { get; set; } = "";
    public string Email { get; set; } = "";
    public string Dni { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Direccion { get; set; } = "";
    public int Edad { get; set; }
    public string EstadoCivil { get; set; } = "";
    public string DatosLaborales { get; set; } = "";
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public bool Baja { get; set; }
}
