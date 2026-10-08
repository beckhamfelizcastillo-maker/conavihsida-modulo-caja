namespace CajaApp.DTOs;

public class LoginResponseDto
{
    public bool Exito { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}