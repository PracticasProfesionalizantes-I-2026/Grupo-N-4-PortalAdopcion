namespace PortalAdopcion.Shared.Enums;

/// <summary>Nombres visibles, opciones y conversión de texto para <see cref="EstadoGeneralEnum"/>.</summary>
public static class EstadoGeneralCatalogo
{
    public static readonly EstadoGeneralEnum[] Valores = Enum.GetValues<EstadoGeneralEnum>();

    /// <summary>Nombre tal como lo ve el usuario y como se guarda en el historial ("Crítico" con tilde).</summary>
    public static string Nombre(EstadoGeneralEnum valor) =>
        valor == EstadoGeneralEnum.Critico ? "Crítico" : valor.ToString();

    public static readonly string[] Nombres = Valores.Select(Nombre).ToArray();

    public static string Opciones => string.Join(", ", Nombres);

    public static bool EstaDefinido(EstadoGeneralEnum valor) => Enum.IsDefined(valor);

    /// <summary>
    /// Convierte texto a enum comparando solo con los nombres permitidos (sin importar mayúsculas).
    /// No usa Enum.TryParse a propósito: aceptaría números como "3" o "99".
    /// </summary>
    public static bool TryParse(string? texto, out EstadoGeneralEnum valor)
    {
        valor = default;
        var limpio = texto?.Trim();
        if (string.IsNullOrEmpty(limpio))
            return false;
        foreach (var candidato in Valores)
        {
            if (string.Equals(limpio, Nombre(candidato), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(limpio, candidato.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                valor = candidato;
                return true;
            }
        }
        return false;
    }
}

/// <summary>Mensajes del campo estadoGeneral, uno por cada caso distinto.</summary>
public static class EstadoGeneralMensajes
{
    public const string Faltante = "Ingresá el estado general.";
    public const string FueraDeRango = "El estado general recibido no es válido.";

    private const int MaxLongitudValorMostrado = 30;

    public static string ValorNoPermitido(string valorRecibido)
    {
        var valor = valorRecibido.Length > MaxLongitudValorMostrado
            ? valorRecibido[..MaxLongitudValorMostrado] + "…"
            : valorRecibido;
        return $"El estado general '{valor}' no es válido. Elegí una opción: {EstadoGeneralCatalogo.Opciones}.";
    }
}
