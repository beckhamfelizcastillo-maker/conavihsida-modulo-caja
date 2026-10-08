using System.Linq;

namespace CajaApp.Helpers;

/// <summary>
/// Reglas de la cédula dominicana (11 dígitos, formato 000-0000000-0).
/// Caja solo trabaja con la cédula para identificar al beneficiario;
/// en pantalla y en comprobantes se muestra enmascarada.
/// </summary>
public static class CedulaHelper
{
    private const int Digitos = 11;

    public static string SoloDigitos(string? cedula) =>
        new((cedula ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>
    /// Es válida si contiene exactamente 11 dígitos y solo usa
    /// dígitos, guiones o espacios.
    /// </summary>
    public static bool EsValida(string? cedula)
    {
        if (string.IsNullOrWhiteSpace(cedula))
        {
            return false;
        }

        bool caracteresPermitidos = cedula
            .Trim()
            .All(c => char.IsDigit(c) || c == '-' || c == ' ');

        return caracteresPermitidos && SoloDigitos(cedula).Length == Digitos;
    }

    /// <summary>Devuelve la cédula con formato 000-0000000-0.</summary>
    public static string Normalizar(string? cedula)
    {
        string digitos = SoloDigitos(cedula);

        if (digitos.Length != Digitos)
        {
            return (cedula ?? string.Empty).Trim();
        }

        return $"{digitos[..3]}-{digitos[3..10]}-{digitos[10]}";
    }

    /// <summary>Ejemplo: 001-1234567-8 → 001-XXXX567-8.</summary>
    public static string Enmascarar(string? cedula)
    {
        string digitos = SoloDigitos(cedula);

        if (digitos.Length != Digitos)
        {
            return string.IsNullOrWhiteSpace(cedula) ? "—" : "XXX-XXXXXXX-X";
        }

        return $"{digitos[..3]}-XXXX{digitos[7..10]}-{digitos[10]}";
    }

    /// <summary>Últimos cuatro dígitos, para mostrar "cédula terminada en 5678".</summary>
    public static string Terminacion(string? cedula)
    {
        string digitos = SoloDigitos(cedula);
        return digitos.Length >= 4 ? digitos[^4..] : digitos;
    }
}
