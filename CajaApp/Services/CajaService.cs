using CajaApp.Data;
using CajaApp.Helpers;
using CajaApp.Models;
using CajaApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CajaApp.Services;

public class CajaService : ICajaService
{
    // ==========================================
    // OBTENER TURNO ABIERTO
    // ==========================================

    public async Task<TurnoCaja?> ObtenerTurnoAbiertoAsync()
    {
        using var db = new AppDbContext();

        return await db.TurnosCaja
            .AsNoTracking()
            .OrderByDescending(x => x.FechaApertura)
            .FirstOrDefaultAsync(
                x => x.Estado == EstadoTurnoCaja.Abierto);
    }


    // ==========================================
    // OBTENER TURNO POR ID
    // ==========================================

    public async Task<TurnoCaja?> ObtenerTurnoPorIdAsync(
        int turnoId)
    {
        using var db = new AppDbContext();

        return await db.TurnosCaja
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == turnoId);
    }


    // ==========================================
    // ABRIR CAJA
    // ==========================================

    public async Task<TurnoCaja> AbrirCajaAsync(
        string usuario,
        string nombreCajero,
        string caja,
        decimal balanceInicial,
        string observacion)
    {
        if (balanceInicial < 0)
        {
            throw new ArgumentException(
                "El balance inicial no puede ser negativo.");
        }

        if (balanceInicial > Formato.MontoMaximoOperacion)
        {
            throw new ArgumentException(
                $"El balance inicial supera el máximo permitido ({Formato.Moneda(Formato.MontoMaximoOperacion)}).");
        }

        if (string.IsNullOrWhiteSpace(usuario))
        {
            throw new InvalidOperationException(
                "No hay un usuario autenticado para abrir la caja.");
        }

        if (string.IsNullOrWhiteSpace(caja))
        {
            throw new ArgumentException(
                "Debe seleccionar una caja o ventanilla.");
        }

        using var db = new AppDbContext();

        bool existeTurno =
            await db.TurnosCaja.AnyAsync(
                x => x.Estado == EstadoTurnoCaja.Abierto);

        if (existeTurno)
        {
            throw new InvalidOperationException(
                "Ya existe un turno de caja abierto.");
        }

        var turno = new TurnoCaja
        {
            NumeroTurno =
                $"TRN-{DateTime.Now:yyyyMMdd-HHmmss}",

            UsuarioCajero =
                usuario,

            NombreCajero =
                nombreCajero,

            Caja =
                caja,

            FechaApertura =
                DateTime.Now,

            BalanceInicial =
                balanceInicial,

            SaldoEsperado =
                balanceInicial,

            Estado =
                EstadoTurnoCaja.Abierto,

            Observacion =
                observacion ?? string.Empty,

            Sincronizado =
                false
        };

        db.TurnosCaja.Add(turno);

        await db.SaveChangesAsync();

        return turno;
    }


    // ==========================================
    // TOTAL COBROS DEL TURNO
    // ==========================================

    public async Task<decimal> ObtenerTotalCobrosTurnoAsync(
        int turnoId)
    {
        using var db = new AppDbContext();

        var montos =
            await db.MovimientosCaja
                .Where(x =>
                    x.TurnoCajaId == turnoId &&
                    x.Tipo == TipoMovimientoCaja.Cobro)
                .Select(x => x.Monto)
                .ToListAsync();

        return montos.Sum();
    }


    // ==========================================
    // TOTAL COBRADO EN EFECTIVO
    // ==========================================

    public async Task<decimal> ObtenerTotalEfectivoTurnoAsync(
        int turnoId)
    {
        using var db = new AppDbContext();

        var referencias =
            await db.MovimientosCaja
                .Where(x =>
                    x.TurnoCajaId == turnoId &&
                    x.Tipo == TipoMovimientoCaja.Cobro)
                .Select(x => x.Referencia)
                .ToListAsync();

        if (referencias.Count == 0)
        {
            return 0m;
        }

        var montos =
            await db.Transacciones
                .Where(x =>
                    referencias.Contains(x.NumeroTransaccion) &&
                    x.FormaPago == FormaPago.Efectivo &&
                    x.Estado == EstadoTransaccion.Completada)
                .Select(x => x.Monto)
                .ToListAsync();

        return montos.Sum();
    }


    // ==========================================
    // TOTAL COBRADO POR TARJETA
    // ==========================================

    public async Task<decimal> ObtenerTotalTarjetaTurnoAsync(
        int turnoId)
    {
        using var db = new AppDbContext();

        var referencias =
            await db.MovimientosCaja
                .Where(x =>
                    x.TurnoCajaId == turnoId &&
                    x.Tipo == TipoMovimientoCaja.Cobro)
                .Select(x => x.Referencia)
                .ToListAsync();

        if (referencias.Count == 0)
        {
            return 0m;
        }

        var montos =
            await db.Transacciones
                .Where(x =>
                    referencias.Contains(x.NumeroTransaccion) &&
                    x.FormaPago == FormaPago.Tarjeta &&
                    x.Estado == EstadoTransaccion.Completada)
                .Select(x => x.Monto)
                .ToListAsync();

        return montos.Sum();
    }


    // ==========================================
    // CERRAR CAJA
    // ==========================================

    public async Task<TurnoCaja> CerrarCajaAsync(
        decimal efectivoDeclarado,
        string observacionCierre)
    {
        if (efectivoDeclarado < 0)
        {
            throw new ArgumentException(
                "El efectivo declarado no puede ser negativo.");
        }

        if (efectivoDeclarado > Formato.MontoMaximoOperacion * 10)
        {
            throw new ArgumentException(
                "El efectivo declarado no es un monto razonable. Verifique la cifra.");
        }

        using var db = new AppDbContext();

        var turno =
            await db.TurnosCaja
                .FirstOrDefaultAsync(
                    x => x.Estado == EstadoTurnoCaja.Abierto);

        if (turno is null)
        {
            throw new InvalidOperationException(
                "No existe un turno abierto para cerrar.");
        }


        // ======================================
        // OBTENER REFERENCIAS DEL TURNO
        // ======================================

        var referencias =
            await db.MovimientosCaja
                .Where(x =>
                    x.TurnoCajaId == turno.Id &&
                    x.Tipo == TipoMovimientoCaja.Cobro)
                .Select(x => x.Referencia)
                .ToListAsync();


        // ======================================
        // CALCULAR EFECTIVO COBRADO
        // ======================================

        decimal efectivoCobrado = 0m;

        if (referencias.Count > 0)
        {
            var montosEfectivo =
                await db.Transacciones
                    .Where(x =>
                        referencias.Contains(x.NumeroTransaccion) &&
                        x.FormaPago == FormaPago.Efectivo &&
                        x.Estado == EstadoTransaccion.Completada)
                    .Select(x => x.Monto)
                    .ToListAsync();

            efectivoCobrado =
                montosEfectivo.Sum();
        }


        // ======================================
        // EFECTIVO ESPERADO
        // ======================================

        decimal efectivoEsperado =
            turno.BalanceInicial +
            efectivoCobrado;


        // ======================================
        // GUARDAR DATOS DEL CIERRE
        // ======================================

        turno.EfectivoDeclarado =
            efectivoDeclarado;

        turno.Diferencia =
            efectivoDeclarado -
            efectivoEsperado;

        turno.FechaCierre =
            DateTime.Now;

        turno.Estado =
            EstadoTurnoCaja.Cerrado;

        turno.Sincronizado =
            false;


        // ======================================
        // OBSERVACIÓN
        // ======================================

        if (!string.IsNullOrWhiteSpace(
                observacionCierre))
        {
            if (string.IsNullOrWhiteSpace(
                    turno.Observacion))
            {
                turno.Observacion =
                    observacionCierre.Trim();
            }
            else
            {
                turno.Observacion =
                    $"{turno.Observacion} | " +
                    $"Cierre: {observacionCierre.Trim()}";
            }
        }


        // ======================================
        // GUARDAR
        // ======================================

        await db.SaveChangesAsync();

        return turno;
    }
}
