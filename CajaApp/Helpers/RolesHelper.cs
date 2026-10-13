using System;
using System.Collections.Generic;
using System.Linq;

namespace CajaApp.Helpers;

/// <summary>
/// Elige el rol con el que el usuario entra a Caja cuando el CORE
/// devuelve varios. Prioridad: Administrador, luego Cajero, luego Auditor.
/// Cualquier otro rol (Farmacia, Médico, etc.) se ignora.
/// </summary>
public static class RolesHelper
{
    public const string Administrador = "Administrador";
    public const string Cajero = "Cajero";
    public const string Auditor = "Auditor";

    private static readonly string[] Prioridad = { Administrador, Cajero, Auditor };

    /// <summary>Devuelve el rol de Caja o null si ninguno de los roles da acceso.</summary>
    public static string? ResolverRolCaja(params IEnumerable<string>?[] listas)
    {
        var normalizados = new HashSet<string>(
            listas
                .Where(l => l is not null)
                .SelectMany(l => l!)
                .Select(Normalizar)
                .Where(r => r is not null)
                .Select(r => r!));

        return Prioridad.FirstOrDefault(normalizados.Contains);
    }

    private static string? Normalizar(string? rol)
    {
        if (string.IsNullOrWhiteSpace(rol))
        {
            return null;
        }

        return rol.Trim().ToUpperInvariant() switch
        {
            "ADMINISTRADOR" or "ADMIN" or "ADMINISTRATOR" => Administrador,
            "CAJERO" => Cajero,
            "AUDITOR" => Auditor,
            _ => null
        };
    }
}
