using CajaApp.Data;
using CajaApp.DTOs.Sync;
using CajaApp.Models;
using CajaApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CajaApp.Services;

public class SyncService : ISyncService
{
    private readonly ISyncApiService _syncApiService;

    // Evita que dos sincronizaciones se ejecuten al mismo tiempo.
    private readonly SemaphoreSlim _syncLock =
        new(1, 1);


    public SyncService(
        ISyncApiService syncApiService)
    {
        _syncApiService =
            syncApiService;
    }


    // ==========================================
    // ESTADO DE SINCRONIZACIÓN
    // ==========================================

    public async Task<EstadoSincronizacion>
        ObtenerEstadoAsync()
    {
        using var db =
            new AppDbContext();


        return new EstadoSincronizacion
        {
            EnLinea =
                await _syncApiService
                    .EstaDisponibleAsync(),

            TurnosPendientes =
                await db.TurnosCaja
                    .CountAsync(
                        x => !x.Sincronizado),

            MovimientosPendientes =
                await db.MovimientosCaja
                    .CountAsync(
                        x => !x.Sincronizado),

            TransaccionesPendientes =
                await db.Transacciones
                    .CountAsync(
                        x => !x.Sincronizado),

            ComprobantesPendientes =
                await db.Comprobantes
                    .CountAsync(
                        x => !x.Sincronizado),

            FondosPendientes =
                await db.FondosConvenios
                    .CountAsync(
                        x => !x.Sincronizado)
        };
    }


    // ==========================================
    // SINCRONIZAR TODOS LOS PENDIENTES
    // ==========================================

    public async Task<EstadoSincronizacion>
        SincronizarPendientesAsync()
    {
        // Si ya hay una sincronización corriendo,
        // esta llamada espera hasta que termine.
        await _syncLock.WaitAsync();

        try
        {
            // --------------------------------------
            // VERIFICAR DISPONIBILIDAD
            // --------------------------------------

            bool disponible =
                await _syncApiService
                    .EstaDisponibleAsync();


            if (!disponible)
            {
                return await ObtenerEstadoAsync();
            }


            // --------------------------------------
            // ORDEN DE SINCRONIZACIÓN
            // --------------------------------------
            //
            // Primero enviamos Turnos porque
            // Movimientos dependen de ellos.
            //
            // Después Movimientos.
            //
            // Después Transacciones.
            //
            // Después Comprobantes.
            //
            // Por último Fondos.

            await SincronizarTurnosAsync();

            await SincronizarMovimientosAsync();

            await SincronizarTransaccionesAsync();

            await SincronizarComprobantesAsync();

            await SincronizarFondosAsync();


            return await ObtenerEstadoAsync();
        }
        finally
        {
            _syncLock.Release();
        }
    }


    // ==========================================
    // SINCRONIZAR TURNOS
    // ==========================================

    private async Task SincronizarTurnosAsync()
    {
        using var db =
            new AppDbContext();


        var pendientes =
            await db.TurnosCaja
                .Where(x =>
                    !x.Sincronizado)
                .OrderBy(x =>
                    x.FechaApertura)
                .ToListAsync();


        foreach (var item in pendientes)
        {
            var dto =
                new SyncTurnoCajaDto
                {
                    IdLocal =
                        item.Id,

                    NumeroTurno =
                        item.NumeroTurno,

                    UsuarioCajero =
                        item.UsuarioCajero,

                    NombreCajero =
                        item.NombreCajero,

                    Caja =
                        item.Caja,

                    FechaApertura =
                        item.FechaApertura,

                    FechaCierre =
                        item.FechaCierre,

                    BalanceInicial =
                        item.BalanceInicial,

                    SaldoEsperado =
                        item.SaldoEsperado,

                    EfectivoDeclarado =
                        item.EfectivoDeclarado,

                    Diferencia =
                        item.Diferencia,

                    Estado =
                        item.Estado
                            .ToString(),

                    Observacion =
                        item.Observacion
                };


            var resultado =
                await _syncApiService
                    .SincronizarTurnoAsync(
                        dto);


            if (resultado.Exito)
            {
                item.Sincronizado =
                    true;
            }
        }


        await db.SaveChangesAsync();
    }


    // ==========================================
    // SINCRONIZAR MOVIMIENTOS
    // ==========================================

    private async Task SincronizarMovimientosAsync()
    {
        using var db =
            new AppDbContext();


        var pendientes =
            await db.MovimientosCaja
                .Where(x =>
                    !x.Sincronizado)
                .OrderBy(x =>
                    x.Fecha)
                .ToListAsync();


        foreach (var item in pendientes)
        {
            var dto =
                new SyncMovimientoCajaDto
                {
                    IdLocal =
                        item.Id,

                    TurnoCajaIdLocal =
                        item.TurnoCajaId,

                    NumeroMovimiento =
                        item.NumeroMovimiento,

                    Fecha =
                        item.Fecha,

                    Tipo =
                        item.Tipo
                            .ToString(),

                    Monto =
                        item.Monto,

                    Concepto =
                        item.Concepto,

                    Referencia =
                        item.Referencia,

                    UsuarioCajero =
                        item.UsuarioCajero
                };


            var resultado =
                await _syncApiService
                    .SincronizarMovimientoAsync(
                        dto);


            if (resultado.Exito)
            {
                item.Sincronizado =
                    true;
            }
        }


        await db.SaveChangesAsync();
    }


    // ==========================================
    // SINCRONIZAR TRANSACCIONES
    // ==========================================

    private async Task SincronizarTransaccionesAsync()
    {
        using var db =
            new AppDbContext();


        var pendientes =
            await db.Transacciones
                .Where(x =>
                    !x.Sincronizado)
                .OrderBy(x =>
                    x.Fecha)
                .ToListAsync();


        foreach (var item in pendientes)
        {
            var dto =
                new SyncTransaccionDto
                {
                    IdLocal =
                        item.Id,

                    NumeroTransaccion =
                        item.NumeroTransaccion,

                    Fecha =
                        item.Fecha,

                    CedulaPaciente =
                        item.CedulaPaciente,

                    TipoServicio =
                        item.TipoServicio,

                    Monto =
                        item.Monto,

                    FormaPago =
                        item.FormaPago
                            .ToString(),

                    EsSubsidiado =
                        item.EsSubsidiado,

                    Estado =
                        item.Estado
                            .ToString(),

                    ComprobanteGenerado =
                        item.ComprobanteGenerado,

                    UsuarioCajero =
                        item.UsuarioCajero,

                    NumeroReceta =
                        item.NumeroReceta
                };


            var resultado =
                await _syncApiService
                    .SincronizarTransaccionAsync(
                        dto);


            if (resultado.Exito)
            {
                item.Sincronizado =
                    true;
            }
        }


        await db.SaveChangesAsync();
    }


    // ==========================================
    // SINCRONIZAR COMPROBANTES
    // ==========================================

    private async Task SincronizarComprobantesAsync()
    {
        using var db =
            new AppDbContext();


        var pendientes =
            await db.Comprobantes
                .Where(x =>
                    !x.Sincronizado)
                .OrderBy(x =>
                    x.FechaGeneracion)
                .ToListAsync();


        foreach (var item in pendientes)
        {
            var dto =
                new SyncComprobanteDto
                {
                    IdLocal =
                        item.Id,

                    TransaccionIdLocal =
                        item.TransaccionId,

                    NumeroComprobante =
                        item.NumeroComprobante,

                    FechaGeneracion =
                        item.FechaGeneracion,

                    TipoDocumento =
                        item.TipoDocumento,

                    EsReimpresion =
                        item.EsReimpresion,

                    CantidadReimpresiones =
                        item.CantidadReimpresiones
                };


            var resultado =
                await _syncApiService
                    .SincronizarComprobanteAsync(
                        dto);


            if (resultado.Exito)
            {
                item.Sincronizado =
                    true;
            }
        }


        await db.SaveChangesAsync();
    }


    // ==========================================
    // SINCRONIZAR FONDOS Y CONVENIOS
    // ==========================================

    private async Task SincronizarFondosAsync()
    {
        using var db =
            new AppDbContext();


        var pendientes =
            await db.FondosConvenios
                .Where(x =>
                    !x.Sincronizado)
                .OrderBy(x =>
                    x.Id)
                .ToListAsync();


        foreach (var item in pendientes)
        {
            var dto =
                new SyncFondoConvenioDto
                {
                    IdLocal =
                        item.Id,

                    Codigo =
                        item.Codigo,

                    Nombre =
                        item.Nombre,

                    TipoFuente =
                        item.TipoFuente
                            .ToString(),

                    Organismo =
                        item.Organismo,

                    MontoAsignado =
                        item.MontoAsignado,

                    MontoUtilizado =
                        item.MontoUtilizado,

                    FechaInicio =
                        item.FechaInicio,

                    FechaFin =
                        item.FechaFin,

                    Activo =
                        item.Activo
                };


            var resultado =
                await _syncApiService
                    .SincronizarFondoAsync(
                        dto);


            if (resultado.Exito)
            {
                item.Sincronizado =
                    true;
            }
        }


        await db.SaveChangesAsync();
    }
}