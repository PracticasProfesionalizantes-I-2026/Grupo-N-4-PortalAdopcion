namespace PortalAdopcion.Shared.Constants;

public static class BusinessRules
{
    public const int PasswordMinLength = 10;
    public const int PasswordMaxLength = 128;

    public const int MinEdadAdoptante = 18;
    public const int MaxEdadAdoptante = 120;

    public const int MinEdadMascota = 0;
    public const int MaxEdadMascota = 30;
    public const int MinPesoMascota = 0;
    public const int MaxPesoMascota = 300;

    public const int MinDiasAntesAprobacion = 15;
    public const int MinDiasPeriodoPrueba = 15;
    public const int DiasInactividadAbandono = 7;
    public const int MaxProcesosActivosPorVoluntario = 2;
    public const int AniosSancionCancelacionPrueba = 1;

    public const int MaxLongitudTexto = 1000;
    public const int MaxLongitudNombre = 150;
    public const int MaxLongitudNombreMascota = 100;
    public const int MaxLongitudEmail = 150;
    public const int MaxLongitudDni = 20;
    public const int MaxLongitudTelefono = 30;
    public const int MaxLongitudDireccion = 200;
    public const int MaxLongitudEstadoCivil = 50;
    public const int MaxLongitudDatosLaborales = 300;
    public const int MaxLongitudMatricula = 50;
    public const int MaxLongitudChip = 80;
    public const int MaxLongitudRaza = 100;
    public const int MaxLongitudTipoEventoMedico = 100;
    public const int MaxLongitudEntidadAutorizante = 150;

    public const string ContrasenaRequisitos = "10 a 128 caracteres, mayúscula, minúscula y número.";

    public static readonly string[] EstadosCiviles =
    [
        "Soltero/a",
        "Casado/a",
        "Divorciado/a",
        "Viudo/a",
        "Conviviente/Unión de hecho",
        "Separado/a"
    ];
}

public static class ErrorMessages
{
    public const string RegistroInexistente = "Registro inexistente.";
    public const string PerfilNoHabilitado = "Perfil no habilitado o sanción vigente.";
    public const string PostulacionBloqueada = "Perfil en lista negra: no podés postular durante el año de sanción.";
    public const string MascotaNoDisponible = "Mascota no disponible.";
    public const string ProcesoActivo = "Existe un proceso activo para la mascota o adoptante.";
    public const string DebeConservarseAdministrador = "Debe conservarse un administrador activo.";
    public const string HistorialRequerido = "Se requiere historial clínico para finalizar.";
    public const string AlertaRequiereValidacion = "La alerta requiere validación veterinaria.";
    public const string EmailODNIExistente = "Ya existe ese email o DNI.";
    public const string CuentaYaExiste = "La cuenta ya existe.";
    public const string CredencialesInvalidas = "Credenciales inválidas.";
    public const string CuentaNoHabilitada = "Credenciales inválidas o cuenta no habilitada.";
}
