namespace CajaApp.Models;

public enum RolUsuario
{
    Cajero,
    Administrador,
    Auditor
}

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public string Token { get; set; } = string.Empty; // JWT recibido de Core/Integración
}