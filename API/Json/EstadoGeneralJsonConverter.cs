namespace PortalAdopcion.API.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using PortalAdopcion.Shared.Enums;

public enum TipoErrorEstadoGeneral
{
    /// <summary>Vino vacío ("") o sin contenido.</summary>
    Faltante,

    /// <summary>Vino un texto que no está entre las opciones.</summary>
    ValorNoPermitido,

    /// <summary>Vino un número u otro tipo de valor que no corresponde.</summary>
    FueraDeRango
}

/// <summary>
/// Error de lectura de estadoGeneral. Conserva el valor crudo que mandó el cliente,
/// que System.Text.Json normalmente pierde al fallar la conversión.
/// </summary>
public sealed class EstadoGeneralJsonException(TipoErrorEstadoGeneral tipo, string? valorRecibido = null)
    : JsonException(Mensaje(tipo, valorRecibido))
{
    public TipoErrorEstadoGeneral Tipo { get; } = tipo;
    public string? ValorRecibido { get; } = valorRecibido;

    public static string Mensaje(TipoErrorEstadoGeneral tipo, string? valorRecibido) => tipo switch
    {
        TipoErrorEstadoGeneral.Faltante => EstadoGeneralMensajes.Faltante,
        TipoErrorEstadoGeneral.ValorNoPermitido => EstadoGeneralMensajes.ValorNoPermitido(valorRecibido ?? string.Empty),
        _ => EstadoGeneralMensajes.FueraDeRango
    };
}

/// <summary>
/// Lee estadoGeneral solo por nombre ("Saludable", "Crítico"...). Funciona también para EstadoGeneralEnum?:
/// un null explícito no pasa por acá y queda en null, que [Required] informa como faltante.
/// </summary>
public sealed class EstadoGeneralJsonConverter : JsonConverter<EstadoGeneralEnum>
{
    public override EstadoGeneralEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new EstadoGeneralJsonException(TipoErrorEstadoGeneral.FueraDeRango);

        var texto = reader.GetString();
        if (string.IsNullOrWhiteSpace(texto))
            throw new EstadoGeneralJsonException(TipoErrorEstadoGeneral.Faltante);

        if (EstadoGeneralCatalogo.TryParse(texto, out var valor))
            return valor;

        throw new EstadoGeneralJsonException(TipoErrorEstadoGeneral.ValorNoPermitido, texto.Trim());
    }

    public override void Write(Utf8JsonWriter writer, EstadoGeneralEnum value, JsonSerializerOptions options) =>
        writer.WriteStringValue(EstadoGeneralCatalogo.Nombre(value));
}
