using CajaApp.DTOs;
using System.Threading.Tasks;

namespace CajaApp.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(string usuario, string password);

    /// <summary>
    /// true cuando appsettings.json tiene UseMockAuthentication = true
    /// (no se llama a Integration, se usa el usuario de demostración).
    /// </summary>
    bool EsModoDemo { get; }
}
