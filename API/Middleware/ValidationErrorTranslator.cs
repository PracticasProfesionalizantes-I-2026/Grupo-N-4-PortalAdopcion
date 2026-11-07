namespace PortalAdopcion.API.Middleware;

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PortalAdopcion.Shared.Constants;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Exceptions;

public static class ValidationErrorTranslator
{
    private static readonly Regex PathRegex = new(@"Path:\s*\$?\.?([a-zA-Z0-9_\.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RequiredFieldRegex = new(@"The\s+(\w+)\s+field is required", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // "Campo: detalle" (acepta tildes y espacios: "Teléfono: ...", "Estado civil: ...")
    private static readonly Regex PropertyColonRegex = new(@"^(?<campo>[\p{L}0-9/ ]{1,40}?)\s*:\s*(?<detalle>.+)$", RegexOptions.Compiled);
    private static readonly Regex EdadRegex = new(@"^Edad( del adoptante)?:\s*(\d+) a (\d+) años\.?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Nombre que usa el servicio -> clave JSON y cómo nombrarlo en la oración ("Ingresá el nombre.", "Ingresá la dirección.").
    private static readonly Dictionary<string, (string Key, string Noun)> Campos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Nombre"] = ("nombre", "el nombre"),
        ["Email"] = ("email", "el email"),
        ["DNI"] = ("dni", "el DNI"),
        ["Teléfono"] = ("telefono", "el teléfono"),
        ["Dirección"] = ("direccion", "la dirección"),
        ["Datos laborales"] = ("datosLaborales", "los datos laborales"),
        ["Estado civil"] = ("estadoCivil", "el estado civil"),
        ["Matrícula"] = ("matricula", "la matrícula"),
        ["Chip"] = ("chip", "el chip"),
        ["Motivo"] = ("motivo", "el motivo"),
        ["Motivo/observaciones"] = ("motivo", "el motivo"),
        ["Motivo de adopción"] = ("motivoAdopcion", "el motivo de adopción"),
        ["Entidad autorizante"] = ("entidadAutorizante", "la entidad autorizante"),
        ["Descripción"] = ("descripcion", "la descripción"),
        ["Tipo"] = ("tipo", "el tipo"),
        ["Tratamiento"] = ("tratamiento", "el tratamiento"),
        ["Detalle"] = ("detalle", "el detalle"),
        ["Antecedentes"] = ("antecedentes", "los antecedentes"),
        ["Vacunas"] = ("vacunas", "las vacunas"),
        ["Estado general"] = ("estadoGeneral", "el estado general"),
        ["Observaciones"] = ("observaciones", "las observaciones"),
        ["Ánimo"] = ("animo", "el ánimo"),
        ["Alimentación"] = ("alimentacion", "la alimentación"),
        ["Tareas"] = ("tareas", "las tareas"),
        ["Medicación"] = ("medicacion", "la medicación"),
        ["Limpieza"] = ("limpieza", "la limpieza"),
    };

    // Mensajes fijos del servicio -> campo y oración corta para el usuario.
    private static readonly Dictionary<string, (string Field, string Message)> MensajesFijos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Peso inválido."] = ("peso", "El peso debe ser mayor a 0 kg."),
        ["Especie: valor obligatorio."] = ("especie", "Ingresá la especie."),
        ["Sexo: valor obligatorio."] = ("sexo", "Ingresá el sexo."),
        ["Edad: valor obligatorio."] = ("edad", "Ingresá la edad."),
        ["Estado general inválido."] = ("estadoGeneral", "Elegí un estado general válido."),
        ["Email inválido."] = ("email", "Ingresá un email válido."),
        ["Edad inválida."] = ("edad", "La edad ingresada no es válida."),
        ["Raza demasiado larga."] = ("raza", $"La raza no puede superar los {BusinessRules.MaxLongitudRaza} caracteres."),
        ["Foto: URL HTTPS válida."] = ("fotoUrl", "La foto debe ser una dirección https válida."),
        ["Rol de personal inválido."] = ("rol", "Elegí un rol válido."),
        ["Rol o estado inválido."] = ("rol", "Elegí un rol y un estado válidos."),
        ["La fecha final debe superar la inicial."] = ("hasta", "La fecha final debe ser posterior a la inicial."),
        ["Se requiere adoptante."] = ("adoptanteId", "Ingresá el adoptante."),
        ["Completá tu perfil."] = ("perfil", "Completá tu perfil para continuar."),
        ["Estado de evaluación inválido."] = ("estado", "Elegí aprobada, rechazada o en observación."),
        ["Voluntario no habilitado."] = ("voluntarioId", "El voluntario elegido no está habilitado."),
        ["La entrevista no puede programarse en el pasado."] = ("fechaProgramada", "La fecha de la entrevista debe ser futura."),
        ["Resultado inválido."] = ("resultado", "Elegí Favorable o Desfavorable."),
        ["Gravedad inválida."] = ("gravedad", "Elegí Normal o Urgente."),
        ["Rango inválido."] = ("desde", "Revisá el rango de fechas."),
        ["Rango de fechas inválido."] = ("desde", "Revisá el rango de fechas."),
        ["Valor de enumeración inválido."] = ("valor", "Elegí una opción válida."),
        ["Tipo: solicitudes, adopciones o mascotas."] = ("tipo", "Elegí solicitudes, adopciones o mascotas."),
        ["Formato: csv o pdf."] = ("formato", "Elegí csv o pdf."),
    };

    public static ErrorResponseDTO CreateResponse(ModelStateDictionary modelState)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        var hayOtrosErrores = modelState.Any(e =>
            e.Value!.Errors.Count > 0 &&
            !e.Key.Equals("d", StringComparison.OrdinalIgnoreCase));

        foreach (var (rawKey, entry) in modelState)
        {
            if (entry.Errors.Count == 0) continue;

            // "d" es el parámetro interno del body: es ruido si hay un error más específico
            if (rawKey.Equals("d", StringComparison.OrdinalIgnoreCase) && hayOtrosErrores)
                continue;

            var modelError = entry.Errors[0];
            var (field, message) = Translate(rawKey, modelError.ErrorMessage, modelError.Exception);

            if (field.Equals("cuerpo", StringComparison.OrdinalIgnoreCase) && hayOtrosErrores)
                continue;

            if (!errors.ContainsKey(field))
                errors[field] = [message];
        }

        if (errors.Count > 1 && errors.ContainsKey("cuerpo"))
        {
            errors.Remove("cuerpo");
        }

        if (errors.Count == 0)
            errors["cuerpo"] = ["No se recibieron datos."];

        return new ErrorResponseDTO(400, "Datos inválidos", errors);
    }

    public static ErrorResponseDTO FromValidationException(ValidationException ex)
    {
        var (field, message) = TranslateValidationException(ex.Message);
        return new ErrorResponseDTO(400, "Datos inválidos", new Dictionary<string, string[]>
        {
            [field] = [message]
        });
    }

    public static ErrorResponseDTO FromJsonException(Exception ex)
    {
        var (field, message) = Translate(string.Empty, ex.Message, ex);
        return new ErrorResponseDTO(400, "Datos inválidos", new Dictionary<string, string[]>
        {
            [field] = [message]
        });
    }

    public static (string Field, string Message) Translate(string rawKey, string? rawMessage, Exception? ex)
    {
        var field = ExtractFieldName(rawKey, rawMessage, ex);
        var message = TranslateMessage(field, rawMessage, ex);
        return (field, message);
    }

    public static string ExtractFieldName(string rawKey, string? rawMessage, Exception? ex)
    {
        var key = rawKey?.Trim() ?? string.Empty;

        // Si la clave es "d", representa el parámetro del método en el controlador
        if (string.Equals(key, "d", StringComparison.OrdinalIgnoreCase))
        {
            var innerField = ExtractFromExceptionOrMessage(rawMessage, ex);
            return string.IsNullOrEmpty(innerField) ? "cuerpo" : innerField;
        }

        if (key.StartsWith("d.", StringComparison.OrdinalIgnoreCase))
            key = key[2..];
        else if (key.StartsWith("$."))
            key = key[2..];
        else if (key.StartsWith("$"))
            key = key[1..];

        if (string.IsNullOrEmpty(key))
        {
            var innerField = ExtractFromExceptionOrMessage(rawMessage, ex);
            return string.IsNullOrEmpty(innerField) ? "cuerpo" : innerField;
        }

        return NormalizeToCamelCase(key);
    }

    private static string? ExtractFromExceptionOrMessage(string? rawMessage, Exception? ex)
    {
        if (ex is JsonException jsonEx && !string.IsNullOrEmpty(jsonEx.Path))
        {
            var p = jsonEx.Path.Trim();
            if (p.StartsWith("$.")) p = p[2..];
            else if (p.StartsWith("$")) p = p[1..];
            if (!string.IsNullOrEmpty(p))
                return NormalizeToCamelCase(p);
        }

        if (!string.IsNullOrEmpty(rawMessage))
        {
            var match = PathRegex.Match(rawMessage);
            if (match.Success)
            {
                var p = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(p))
                    return NormalizeToCamelCase(p);
            }
        }

        return null;
    }

    private static string NormalizeToCamelCase(string key)
    {
        var segments = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var camelSegments = segments.Select(s =>
        {
            var clean = Regex.Replace(s, @"\[\d+\]", "");
            if (string.IsNullOrEmpty(clean)) return s;
            return JsonNamingPolicy.CamelCase.ConvertName(clean);
        });
        var result = string.Join(".", camelSegments);
        return string.IsNullOrEmpty(result) ? "cuerpo" : result;
    }

    public static string TranslateMessage(string field, string? rawMessage, Exception? ex)
    {
        var msg = rawMessage ?? ex?.Message ?? string.Empty;

        // 1. Body vacío o nulo
        if (field.Equals("cuerpo", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("The d field is required", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("A non-empty request body is required", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("body is required", StringComparison.OrdinalIgnoreCase))
        {
            return "No se recibieron datos.";
        }

        // 2. Cero a la izquierda
        if (msg.Contains("Invalid leading zero", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("leading zero", StringComparison.OrdinalIgnoreCase))
        {
            if (field.Equals("dni", StringComparison.OrdinalIgnoreCase))
                return "El DNI debe escribirse sin ceros al inicio.";
            return $"El campo {field} debe escribirse sin ceros al inicio.";
        }

        // 3. Coma o valor mal escrito (sintaxis JSON)
        if (msg.Contains("invalid start of a value", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("is invalid after a", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("invalid character", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Expected depth to be zero", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Expected start of a", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("The JSON object is not properly closed", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(field) && !field.Equals("cuerpo", StringComparison.OrdinalIgnoreCase))
                return $"El campo {field} tiene un formato incorrecto.";
            return "El formato de la solicitud es incorrecto.";
        }

        // 4. Enum o tipo no convertible
        if (msg.Contains("could not be converted", StringComparison.OrdinalIgnoreCase))
        {
            if (msg.Contains("DateTime", StringComparison.OrdinalIgnoreCase))
                return $"El campo {field} debe ser una fecha válida.";
            if (msg.Contains("Int32", StringComparison.OrdinalIgnoreCase) || msg.Contains("Int64", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Decimal", StringComparison.OrdinalIgnoreCase) || msg.Contains("Double", StringComparison.OrdinalIgnoreCase))
                return $"El campo {field} debe ser un número.";
            if (msg.Contains("Guid", StringComparison.OrdinalIgnoreCase))
                return $"El campo {field} no es un identificador válido.";
            if (field.Equals("especie", StringComparison.OrdinalIgnoreCase))
                return "Elegí una opción válida para especie.";
            if (field.Equals("sexo", StringComparison.OrdinalIgnoreCase))
                return "Elegí una opción válida para sexo.";
            return $"Elegí una opción válida para {field}.";
        }

        // 5. Miembro no mapeado (JsonUnmappedMemberHandling.Disallow)
        if (msg.Contains("could not be mapped to any .NET member", StringComparison.OrdinalIgnoreCase))
        {
            return $"El campo {field} no está permitido.";
        }

        // 6. Mensaje en inglés por defecto: "The X field is required."
        var reqMatch = RequiredFieldRegex.Match(msg);
        if (reqMatch.Success)
        {
            var reqField = reqMatch.Groups[1].Value;
            if (string.Equals(reqField, "d", StringComparison.OrdinalIgnoreCase))
                return "No se recibieron datos.";
            if (string.Equals(reqField, "dni", StringComparison.OrdinalIgnoreCase))
                return "Ingresá el DNI.";
            return $"Ingresá el {JsonNamingPolicy.CamelCase.ConvertName(reqField)}.";
        }

        // 7. Peso inválido
        if (msg.Contains("peso inválido", StringComparison.OrdinalIgnoreCase) ||
            (field.Equals("peso", StringComparison.OrdinalIgnoreCase) && msg.Contains("mayor a 0", StringComparison.OrdinalIgnoreCase)))
        {
            return "El peso debe ser mayor a 0 kg.";
        }

        // 8. Estado general inválido
        if (IsTechnicalEnglishMessage(msg) &&
            (msg.Contains("estado general", StringComparison.OrdinalIgnoreCase) ||
             field.Equals("estadoGeneral", StringComparison.OrdinalIgnoreCase)))
        {
            return "Elegí un estado general válido.";
        }

        // 9. Si el mensaje proviene de un atributo DataAnnotations en español, preservarlo
        if (!IsTechnicalEnglishMessage(msg))
        {
            return msg;
        }

        // Fallback genérico limpio
        return $"El campo {field} tiene un formato incorrecto.";
    }

    public static (string Field, string Message) TranslateValidationException(string rawMessage)
    {
        var msg = rawMessage?.Trim() ?? string.Empty;

        if (MensajesFijos.TryGetValue(msg, out var fijo))
            return fijo;

        if (msg.StartsWith("Contraseña:", StringComparison.OrdinalIgnoreCase))
            return ("password", $"La contraseña debe tener {BusinessRules.ContrasenaRequisitos}");

        if (msg.StartsWith("Estado civil inválido", StringComparison.OrdinalIgnoreCase))
            return ("estadoCivil", "Elegí un estado civil válido.");

        var edad = EdadRegex.Match(msg);
        if (edad.Success)
            return ("edad", $"La edad debe estar entre {edad.Groups[2].Value} y {edad.Groups[3].Value} años.");

        var match = PropertyColonRegex.Match(msg);
        if (match.Success)
        {
            var campo = match.Groups["campo"].Value.Trim();
            var detalle = match.Groups["detalle"].Value.Trim();
            var (key, noun) = Campos.TryGetValue(campo, out var conocido)
                ? conocido
                : (JsonNamingPolicy.CamelCase.ConvertName(campo.Replace(" ", "")), "el campo " + campo.ToLowerInvariant());
            var sujeto = char.ToUpper(noun[0]) + noun[1..];
            var numero = Regex.Match(detalle, @"\d+").Value;

            if (detalle.StartsWith("caracteres inválidos", StringComparison.OrdinalIgnoreCase))
                return (key, $"{sujeto} solo admite números, puntos y guiones.");
            if (detalle.Contains("valores negativos", StringComparison.OrdinalIgnoreCase))
                return (key, $"{sujeto} no puede ser negativo.");
            if (detalle.Contains("obligatorio", StringComparison.OrdinalIgnoreCase) && detalle.Contains("dígitos", StringComparison.OrdinalIgnoreCase))
                return (key, $"Ingresá {noun} (solo números, hasta {numero} dígitos).");
            if (detalle.Contains("obligatorio", StringComparison.OrdinalIgnoreCase))
                return (key, $"Ingresá {noun} (hasta {numero} caracteres).");
            if (detalle.StartsWith("máximo", StringComparison.OrdinalIgnoreCase))
                return (key, $"{sujeto} no puede superar los {numero} dígitos.");
            if (detalle.StartsWith("mínimo", StringComparison.OrdinalIgnoreCase))
                return (key, $"{sujeto} debe tener al menos {numero} dígitos.");

            return (key, detalle);
        }

        return ("solicitud", msg);
    }

    private static bool IsTechnicalEnglishMessage(string message)
    {
        return message.Contains("The ", StringComparison.Ordinal) ||
               message.Contains("field is required", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("Path:", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("LineNumber:", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("JSON", StringComparison.OrdinalIgnoreCase);
    }
}

