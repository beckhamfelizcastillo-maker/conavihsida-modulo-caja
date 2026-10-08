namespace CajaApp.Services.Interfaces;

public interface ISessionService
{
    string Usuario { get; set; }

    string NombreCompleto { get; set; }

    string Rol { get; set; }

    string Token { get; set; }

    bool EstaAutenticado { get; }

    void CerrarSesion();
}