using CajaApp.DTOs.Sync;
using System.Threading.Tasks;

namespace CajaApp.Services.Interfaces;

public interface ISyncApiService
{
    Task<bool> EstaDisponibleAsync();

    Task<SyncResponseDto> SincronizarTurnoAsync(
        SyncTurnoCajaDto turno);

    Task<SyncResponseDto> SincronizarMovimientoAsync(
        SyncMovimientoCajaDto movimiento);

    Task<SyncResponseDto> SincronizarTransaccionAsync(
        SyncTransaccionDto transaccion);

    Task<SyncResponseDto> SincronizarComprobanteAsync(
        SyncComprobanteDto comprobante);

    Task<SyncResponseDto> SincronizarFondoAsync(
        SyncFondoConvenioDto fondo);
}