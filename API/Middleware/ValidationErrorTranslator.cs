namespace PortalAdopcion.API.Middleware;

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PortalAdopcion.Shared.DTOs;
using PortalAdopcion.Shared.Exceptions;

public static class ValidationErrorTranslator
{
    private static readonly Regex PathRegex = new(@"Path:\s*\$?\.?([a-zA-Z0-9_\.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RequiredFieldRegex = new(@"The\s+(\w+)\s+field is required", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PropertyColonRegex = new(@"^([a-zA-Z0-9_]+)\s*:\s*(.+)$", RegexOptions.Compiled);

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
        if (msg.Contains("estado general", StringComparison.OrdinalIgnoreCase) ||
            field.Equals("estadoGeneral", StringComparison.OrdinalIgnoreCase))
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

        if (msg.Equals("Peso inválido.", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("peso", StringComparison.OrdinalIgnoreCase))
        {
            return ("peso", "El peso debe ser mayor a 0 kg.");
        }

        if (msg.Equals("Estado general inválido.", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("estado general", StringComparison.OrdinalIgnoreCase))
        {
            return ("estadoGeneral", "Elegí un estado general válido.");
        }

        if (msg.Contains("email", StringComparison.OrdinalIgnoreCase))
        {
            return ("email", "Ingresá un email válido.");
        }

        if (msg.Contains("contraseña", StringComparison.OrdinalIgnoreCase))
        {
            return ("password", "La contraseña no cumple los requisitos.");
        }

        if (msg.Contains("edad", StringComparison.OrdinalIgnoreCase))
        {
            return ("edad", "La edad ingresada no es válida.");
        }

        if (msg.Contains("formato", StringComparison.OrdinalIgnoreCase))
        {
            return ("formato", "Elegí una opción válida para formato.");
        }

        var match = PropertyColonRegex.Match(msg);
        if (match.Success)
        {
            var rawField = match.Groups[1].Value.Trim();
            var detail = match.Groups[2].Value.Trim();
            var fieldName = JsonNamingPolicy.CamelCase.ConvertName(rawField);

            if (detail.Contains("obligatorio", StringComparison.OrdinalIgnoreCase))
                return (fieldName, $"Ingresá el {fieldName}.");

            return (fieldName, detail);
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

