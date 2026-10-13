namespace CajaApp.Services.Interfaces;

public interface ISessionService
{
    string Usuario { get; set; }

    string NombreCompleto { get; set; }

    string Rol { get; set; }

    string Token { get; set; }

    bool EstaAutenticado { get; }

    /// <summary>true cuando el Gateway rechazó el token (401): hay que volver a iniciar sesión.</summary>
    bool TokenRechazado { get; set; }

    void CerrarSesion();
}