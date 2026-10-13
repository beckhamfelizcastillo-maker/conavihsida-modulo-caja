using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CajaApp.Services;

/// <summary>
/// Permite iniciar sesión cuando Integración / CORE no responden
/// (operación continua). Después de cada login correcto en línea se guarda
/// el usuario con un hash PBKDF2 de la contraseña (nunca la contraseña).
/// Sin conexión, solo entra un usuario que ya inició sesión en línea en
/// esta terminal durante los últimos días.
/// </summary>
public static class SesionOfflineStore
{
    public static int DiasValidez { get; set; } = 7;

    public static string Ruta { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "sesion-offline.json");

    private const int Iteraciones = 100_000;

    private class Registro
    {
        public string Usuario { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string Token { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }

    public record SesionGuardada(string NombreCompleto, List<string> Roles, string Token);

    public static void Guardar(string usuario, string password, string nombreCompleto, IEnumerable<string> roles, string token)
    {
        try
        {
            var registros = Leer();
            registros.RemoveAll(r => string.Equals(r.Usuario, usuario, StringComparison.OrdinalIgnoreCase));

            byte[] salt = RandomNumberGenerator.GetBytes(16);

            registros.Add(new Registro
            {
                Usuario = usuario,
                Salt = Convert.ToBase64String(salt),
                Hash = Convert.ToBase64String(Derivar(password, salt)),
                NombreCompleto = nombreCompleto,
                Roles = new List<string>(roles),
                Token = token,
                Fecha = DateTime.Now
            });

            File.WriteAllText(Ruta, JsonSerializer.Serialize(registros));
        }
        catch
        {
            // Si no se puede guardar, solo se pierde el acceso sin conexión.
        }
    }

    public static SesionGuardada? Validar(string usuario, string password)
    {
        var registro = Leer().Find(r => string.Equals(r.Usuario, usuario, StringComparison.OrdinalIgnoreCase));

        if (registro is null || registro.Fecha < DateTime.Now.AddDays(-DiasValidez))
        {
            return null;
        }

        try
        {
            byte[] esperado = Convert.FromBase64String(registro.Hash);
            byte[] calculado = Derivar(password, Convert.FromBase64String(registro.Salt));

            return CryptographicOperations.FixedTimeEquals(esperado, calculado)
                ? new SesionGuardada(registro.NombreCompleto, registro.Roles, registro.Token)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Derivar(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iteraciones, HashAlgorithmName.SHA256, 32);

    private static List<Registro> Leer()
    {
        try
        {
            return File.Exists(Ruta)
                ? JsonSerializer.Deserialize<List<Registro>>(File.ReadAllText(Ruta)) ?? new()
                : new();
        }
        catch
        {
            return new();
        }
    }
}
