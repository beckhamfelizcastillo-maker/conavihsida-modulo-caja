using CajaApp.Models;
using System.Threading.Tasks;

namespace CajaApp.Services.Interfaces;

public interface IComprobanteService
{
    Task<Comprobante> GenerarAsync(
        int transaccionId);

    Task<Comprobante?> ObtenerPorTransaccionAsync(
        int transaccionId);

    Task<string> ReimprimirAsync(
        int transaccionId);
}