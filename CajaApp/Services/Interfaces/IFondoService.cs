using CajaApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CajaApp.Services.Interfaces;

public interface IFondoService
{
    Task<List<FondoConvenio>> ObtenerTodosAsync();

    Task<List<FondoConvenio>> ObtenerActivosAsync();

    Task<FondoConvenio> CrearAsync(
        string codigo,
        string nombre,
        TipoFuenteFondo tipoFuente,
        string organismo,
        decimal montoAsignado,
        System.DateTime fechaInicio,
        System.DateTime? fechaFin);

    Task CambiarEstadoAsync(
        int fondoId,
        bool activo);
}