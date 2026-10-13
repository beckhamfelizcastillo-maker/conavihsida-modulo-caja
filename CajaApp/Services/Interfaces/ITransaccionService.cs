using CajaApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CajaApp.Services.Interfaces;

public interface ITransaccionService
{
    Task<Transaccion> RegistrarAsync(
        string cedulaPaciente,
        string tipoServicio,
        decimal monto,
        string formaPago,
        bool esSubsidiado,
        string usuarioCajero,
        string? numeroReceta = null);

    Task<List<Transaccion>> ObtenerHistorialAsync();

    /// <summary>Transacciones registradas entre dos fechas (por ejemplo, durante un turno).</summary>
    Task<List<Transaccion>> ObtenerEntreAsync(
        System.DateTime desde,
        System.DateTime? hasta);
}
