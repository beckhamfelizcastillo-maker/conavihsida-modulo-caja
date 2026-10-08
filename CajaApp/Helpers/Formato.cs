using System;
using System.Globalization;
using System.Linq;

namespace CajaApp.Helpers;

/// <summary>
/// Utilidades de presentación y lectura de montos, fechas y nombres.
/// Se usa una sola convención en toda la aplicación:
/// RD$ 1,234.56 (coma para miles, punto para decimales).
/// </summary>
public static class Formato
{
    private static readonly CultureInfo Cultura =
        CultureInfo.InvariantCulture;

    /// <summary>Monto máximo aceptado en una sola operación de caja.</summary>
    public const decimal MontoMaximoOperacion = 1_000_000m;

    public static string Moneda(decimal valor) =>
        "RD$ " + valor.ToString("N2", Cultura);

    public static string MonedaConSigno(decimal valor) =>
        (valor > 0 ? "+" : valor < 0 ? "−" : string.Empty)
        + Moneda(Math.Abs(valor));

    /// <summary>Texto editable para un campo de monto (sin símbolo ni separador de miles).</summary>
    public static string MontoEditable(decimal valor) =>
        valor.ToString("0.##", Cultura);

    /// <summary>
    /// Interpreta lo que el usuario escribe en un campo de monto.
    /// Acepta "5000", "5,000.50", "RD$ 5,000", "5000,5" o "  1 500 ".
    /// </summary>
    public static bool TryParseMonto(string? texto, out decimal monto)
    {
        monto = 0m;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        string limpio = texto
            .Replace("RD$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("$", string.Empty)
            .Replace(" ", string.Empty)
            .Trim();

        if (limpio.Length == 0)
        {
            return false;
        }

        // "5000,5" o "5000,50": la coma se usó como separador decimal.
        int ultimaComa = limpio.LastIndexOf(',');
        if (ultimaComa >= 0 && !limpio.Contains('.'))
        {
            int decimales = limpio.Length - ultimaComa - 1;
            if (decimales is 1 or 2 && limpio.Count(c => c == ',') == 1)
            {
                limpio = limpio.Replace(',', '.');
            }
        }

        limpio = limpio.Replace(",", string.Empty);

        return decimal.TryParse(
            limpio,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            Cultura,
            out monto);
    }

    public static string FechaHora(DateTime fecha) =>
        fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    public static string Fecha(DateTime fecha) =>
        fecha.ToString("dd/MM/yyyy", Cultura);

    public static string Hora(DateTime fecha) =>
        fecha.ToString("hh:mm tt", CultureInfo.GetCultureInfo("es-DO"));

    public static string FechaLarga(DateTime fecha)
    {
        var cultura = CultureInfo.GetCultureInfo("es-DO");
        string texto = fecha.ToString("dddd d 'de' MMMM 'de' yyyy", cultura);
        return char.ToUpper(texto[0], cultura) + texto[1..];
    }

    public static string Saludo(DateTime ahora) =>
        ahora.Hour switch
        {
            < 12 => "Buenos días",
            < 19 => "Buenas tardes",
            _ => "Buenas noches"
        };

    public static string PrimerNombre(string? nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
        {
            return string.Empty;
        }

        return nombreCompleto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    }

    public static string Iniciales(string? nombreCompleto)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
        {
            return "··";
        }

        var partes = nombreCompleto
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        string iniciales = partes.Length == 1
            ? partes[0][..Math.Min(2, partes[0].Length)]
            : $"{partes[0][0]}{partes[^1][0]}";

        return iniciales.ToUpperInvariant();
    }
}
