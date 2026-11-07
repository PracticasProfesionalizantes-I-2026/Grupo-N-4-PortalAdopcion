namespace PortalAdopcion.Shared.DTOs;

using System.ComponentModel.DataAnnotations;
using PortalAdopcion.Shared.Constants;
using PortalAdopcion.Shared.Enums;

public record Actor(Guid Id, string Rol, Guid? AdoptanteId);

public record RegistroUsuarioDTO(
    [Required(ErrorMessage = "Ingresá el nombre.")]
    [StringLength(BusinessRules.MaxLongitudNombre, ErrorMessage = "El nombre no puede superar los 15 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "Ingresá el email.")]
    [EmailAddress(ErrorMessage = "El campo email tiene un formato incorrecto.")]
    [StringLength(BusinessRules.MaxLongitudEmail, ErrorMessage = "El email no puede superar los 50 caracteres.")]
    string Email,

    [Required(ErrorMessage = "Ingresá el DNI.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "El DNI debe contener solo números.")]
    string Dni,

    [Required(ErrorMessage = "Ingresá la contraseña.")]
    string Password,

    [Required(ErrorMessage = "Ingresá el teléfono.")]
    string Telefono,

    [Required(ErrorMessage = "Ingresá la dirección.")]
    string Direccion,

    [Range(BusinessRules.MinEdadAdoptante, BusinessRules.MaxEdadAdoptante, ErrorMessage = "La edad debe estar entre 18 y 70 años.")]
    int Edad,

    [Required(ErrorMessage = "Ingresá el estado civil.")]
    string EstadoCivil,

    [Required(ErrorMessage = "Ingresá los datos laborales.")]
    string DatosLaborales
);

public record LoginDTO(
    [Required(ErrorMessage = "Ingresá el email.")]
    [EmailAddress(ErrorMessage = "El campo email tiene un formato incorrecto.")]
    string Email,

    [Required(ErrorMessage = "Ingresá la contraseña.")]
    string Password
);

public record PersonalDTO(
    [Required(ErrorMessage = "Ingresá el nombre.")]
    string Nombre,

    [Required(ErrorMessage = "Ingresá el email.")]
    [EmailAddress(ErrorMessage = "El campo email tiene un formato incorrecto.")]
    string Email,

    [Required(ErrorMessage = "Ingresá el DNI.")]
    string Dni,

    [Required(ErrorMessage = "Ingresá la contraseña.")]
    string Password,

    [Required(ErrorMessage = "Ingresá el rol.")]
    string Rol,

    string? Matricula
);

public record UsuarioCambioDTO(
    [Required(ErrorMessage = "Ingresá el rol.")]
    string Rol,

    [Required(ErrorMessage = "Ingresá el estado.")]
    string Estado,

    string? Matricula
);

public record PerfilDTO(
    [Required(ErrorMessage = "Ingresá el nombre.")]
    [StringLength(BusinessRules.MaxLongitudNombre, ErrorMessage = "El nombre no puede superar los 15 caracteres.")]
    string NombreCompleto,

    [Required(ErrorMessage = "Ingresá el email.")]
    [EmailAddress(ErrorMessage = "El campo email tiene un formato incorrecto.")]
    string Email,

    [Required(ErrorMessage = "Ingresá el DNI.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "El DNI debe contener solo números.")]
    string Dni,

    [Required(ErrorMessage = "Ingresá el teléfono.")]
    string Telefono,

    [Required(ErrorMessage = "Ingresá la dirección.")]
    string Direccion,

    [Range(BusinessRules.MinEdadAdoptante, BusinessRules.MaxEdadAdoptante, ErrorMessage = "La edad debe estar entre 18 y 70 años.")]
    int Edad,

    [Required(ErrorMessage = "Ingresá el estado civil.")]
    string EstadoCivil,

    [Required(ErrorMessage = "Ingresá los datos laborales.")]
    string DatosLaborales
);

public record MascotaFichaDTO(
    [Required(ErrorMessage = "Ingresá el nombre.")]
    [StringLength(BusinessRules.MaxLongitudNombreMascota, ErrorMessage = "El nombre no puede superar los 15 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "Ingresá la especie.")]
    [EnumDataType(typeof(EspecieEnum), ErrorMessage = "Elegí una opción válida para especie.")]
    EspecieEnum Especie,

    [StringLength(BusinessRules.MaxLongitudRaza, ErrorMessage = "La raza no puede superar los 40 caracteres.")]
    string? Raza,

    [Range(BusinessRules.MinEdadMascota, BusinessRules.MaxEdadMascota, ErrorMessage = "La edad debe estar entre 0 y 20 años.")]
    int Edad,

    [Required(ErrorMessage = "Ingresá el sexo.")]
    [EnumDataType(typeof(SexoEnum), ErrorMessage = "Elegí una opción válida para sexo.")]
    SexoEnum Sexo,

    string? FotoUrl,

    [StringLength(BusinessRules.MaxLongitudChip, ErrorMessage = "El chip no puede superar los 80 caracteres.")]
    string? Chip,

    [Range(0.01, 130.0, ErrorMessage = "El peso debe ser mayor a 0 kg.")]
    decimal? Peso = null,

    [AllowedValues("Saludable", "Bien", "Excelente", "Bueno", "Regular", "Malo", "Crítico", ErrorMessage = "Elegí un estado general válido.")]
    string? EstadoGeneral = null,

    string? Vacunas = null
);

public record CambioMascotaDTO(
    [Required(ErrorMessage = "Ingresá el estado.")]
    EstadoMascotaEnum Estado,

    [Required(ErrorMessage = "Ingresá el motivo.")]
    string Motivo
);

public record PostulacionDTO(
    [Required(ErrorMessage = "Ingresá la mascota.")]
    Guid MascotaId,

    [Required(ErrorMessage = "Ingresá el motivo de adopción.")]
    string MotivoAdopcion,

    Guid? AdoptanteId
);

public record EvaluacionDTO(
    [Required(ErrorMessage = "Ingresá el estado.")]
    EstadoSolicitudEnum Estado,

    string? Motivo,

    [Required(ErrorMessage = "Ingresá la entidad autorizante.")]
    string EntidadAutorizante
);

public record MotivoDTO(
    [Required(ErrorMessage = "Ingresá el motivo.")]
    string Motivo
);

public record BlacklistDTO(
    [Required(ErrorMessage = "Ingresá el adoptante.")]
    Guid AdoptanteId,

    [Required(ErrorMessage = "Ingresá el motivo.")]
    string Motivo,

    DateTime Desde,
    DateTime Hasta
);

public record ClinicaDTO(
    [Range(0.01, 130.0, ErrorMessage = "El peso debe ser mayor a 0 kg.")]
    decimal Peso,

    [Required(ErrorMessage = "Ingresá el estado general.")]
    [AllowedValues("Saludable", "Bien", "Excelente", "Bueno", "Regular", "Malo", "Crítico", ErrorMessage = "Elegí un estado general válido.")]
    string EstadoGeneral,

    [Required(ErrorMessage = "Ingresá las vacunas.")]
    [MinLength(1, ErrorMessage = "Ingresá las vacunas.")]
    string Vacunas,

    [Required(ErrorMessage = "Ingresá los antecedentes.")]
    string Antecedentes,

    bool Esterilizada
);

public record EventoMedicoDTO(
    [Required(ErrorMessage = "Ingresá el tipo.")]
    string Tipo,

    [Required(ErrorMessage = "Ingresá la descripción.")]
    string Descripcion,

    [Required(ErrorMessage = "Ingresá el tratamiento.")]
    string Tratamiento
);

public record EntrevistaDTO(
    DateTime FechaProgramada,

    [Required(ErrorMessage = "Ingresá el voluntario.")]
    Guid VoluntarioId
);

public record ResultadoEntrevistaDTO(
    [Required(ErrorMessage = "Ingresá el resultado.")]
    string Resultado,

    string Observaciones
);

public record SeguimientoDTO(
    [Required(ErrorMessage = "Ingresá el ánimo.")]
    string Animo,

    [Required(ErrorMessage = "Ingresá la alimentación.")]
    string Alimentacion,

    [Required(ErrorMessage = "Ingresá las tareas.")]
    string Tareas,

    [Required(ErrorMessage = "Ingresá la medicación.")]
    string Medicacion,

    [Required(ErrorMessage = "Ingresá la limpieza.")]
    string Limpieza,

    bool Alerta
);

public record ValidacionClinicaDTO(
    [Required(ErrorMessage = "Ingresá el motivo.")]
    string Motivo
);

public record IncidenteDTO(
    [Required(ErrorMessage = "Ingresá la solicitud.")]
    Guid SolicitudId,

    [Required(ErrorMessage = "Ingresá la gravedad.")]
    string Gravedad,

    [Required(ErrorMessage = "Ingresá el detalle.")]
    string Detalle
);
