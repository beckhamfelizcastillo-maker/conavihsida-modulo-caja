using CajaApp.Models;
using System.Threading.Tasks;

namespace CajaApp.Services.Interfaces;

public interface ISyncService
{
    Task<EstadoSincronizacion>
        ObtenerEstadoAsync();

    Task<EstadoSincronizacion>
        SincronizarPendientesAsync();
}