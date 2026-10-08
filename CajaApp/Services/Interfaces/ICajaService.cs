using CajaApp.Models;
using System.Threading.Tasks;

namespace CajaApp.Services.Interfaces;

public interface ICajaService
{
    Task<TurnoCaja?> ObtenerTurnoAbiertoAsync();

    Task<TurnoCaja?> ObtenerTurnoPorIdAsync(int turnoId);

    Task<TurnoCaja> AbrirCajaAsync(
        string usuario,
        string nombreCajero,
        string caja,
        decimal balanceInicial,
        string observacion);

    Task<TurnoCaja> CerrarCajaAsync(
        decimal efectivoDeclarado,
        string observacionCierre);

    Task<decimal> ObtenerTotalCobrosTurnoAsync(int turnoId);

    Task<decimal> ObtenerTotalEfectivoTurnoAsync(int turnoId);

    Task<decimal> ObtenerTotalTarjetaTurnoAsync(int turnoId);
}