using CajaApp.Services.Interfaces;

namespace CajaApp.Services;

public class SessionService : ISessionService
{
    public string Usuario { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public bool EstaAutenticado =>
        !string.IsNullOrWhiteSpace(Token);

    public void CerrarSesion()
    {
        Usuario = string.Empty;

        NombreCompleto = string.Empty;

        Rol = string.Empty;

        Token = string.Empty;
    }
}