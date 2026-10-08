using CajaApp.Data;
using CajaApp.Helpers;
using CajaApp.Models;
using CajaApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CajaApp.Services;

public class TransaccionService : ITransaccionService
{
    // ==========================================
    // REGISTRAR TRANSACCIÓN
    // ==========================================

    public async Task<Transaccion> RegistrarAsync(
        string cedulaPaciente,
        string tipoServicio,
        decimal monto,
        string formaPago,
        bool esSubsidiado,
        string usuarioCajero)
    {
        // ======================================
        // VALIDACIONES
        // ======================================

        if (string.IsNullOrWhiteSpace(cedulaPaciente))
        {
            throw new ArgumentException(
                "Debe ingresar la cédula del paciente.");
        }

        if (!CedulaHelper.EsValida(cedulaPaciente))
        {
            throw new ArgumentException(
                "La cédula debe tener 11 dígitos (formato 000-0000000-0).");
        }

        cedulaPaciente =
            CedulaHelper.Normalizar(cedulaPaciente);

        if (monto > Formato.MontoMaximoOperacion)
        {
            throw new ArgumentException(
                $"El monto supera el máximo permitido por operación ({Formato.Moneda(Formato.MontoMaximoOperacion)}).");
        }

        if (string.IsNullOrWhiteSpace(usuarioCajero))
        {
            throw new InvalidOperationException(
                "No hay un usuario autenticado para registrar la operación.");
        }

        if (string.IsNullOrWhiteSpace(tipoServicio))
        {
            throw new ArgumentException(
                "Debe seleccionar el tipo de servicio.");
        }

        if (!esSubsidiado &&
            string.IsNullOrWhiteSpace(formaPago))
        {
            throw new ArgumentException(
                "Debe seleccionar una forma de pago.");
        }

        if (monto < 0)
        {
            throw new ArgumentException(
                "El monto no puede ser negativo.");
        }

        if (!esSubsidiado &&
            monto <= 0)
        {
            throw new ArgumentException(
                "El monto debe ser mayor que cero para una transacción no subsidiada.");
        }


        using var db =
            new AppDbContext();


        // ======================================
        // VALIDAR TURNO ABIERTO
        // ======================================

        var turno =
            await db.TurnosCaja
                .FirstOrDefaultAsync(
                    x =>
                        x.Estado ==
                        EstadoTurnoCaja.Abierto);

        if (turno is null)
        {
            throw new InvalidOperationException(
                "No existe una caja abierta. Debe realizar la apertura antes de registrar transacciones.");
        }


        // ======================================
        // MONTO FINAL
        // ======================================

        decimal montoFinal =
            esSubsidiado
                ? 0m
                : monto;


        // ======================================
        // FORMA DE PAGO
        // ======================================

        FormaPago formaPagoEnum;

        if (esSubsidiado)
        {
            formaPagoEnum =
                FormaPago.Subsidiado;
        }
        else
        {
            formaPagoEnum =
                formaPago switch
                {
                    "Efectivo" =>
                        FormaPago.Efectivo,

                    "Tarjeta" =>
                        FormaPago.Tarjeta,

                    _ =>
                        throw new ArgumentException(
                            "La forma de pago seleccionada no es válida.")
                };
        }


        // ======================================
        // GENERAR NÚMERO DE TRANSACCIÓN
        // ======================================

        string numeroTransaccion =
            $"TX-{DateTime.Now:yyyyMMdd-HHmmssfff}";


        // ======================================
        // CREAR TRANSACCIÓN
        // ======================================

        var transaccion =
            new Transaccion
            {
                NumeroTransaccion =
                    numeroTransaccion,

                Fecha =
                    DateTime.Now,

                CedulaPaciente =
                    cedulaPaciente.Trim(),

                TipoServicio =
                    tipoServicio,

                Monto =
                    montoFinal,

                FormaPago =
                    formaPagoEnum,

                EsSubsidiado =
                    esSubsidiado,

                Estado =
                    EstadoTransaccion.Pendiente,

                ComprobanteGenerado =
                    false,

                Sincronizado =
                    false,

                UsuarioCajero =
                    usuarioCajero
            };


        // ======================================
        // TRANSACCIÓN SQL
        // ======================================

        await using var dbTransaction =
            await db.Database
                .BeginTransactionAsync();

        try
        {
            // ==================================
            // GUARDAR TRANSACCIÓN
            // ==================================

            db.Transacciones.Add(
                transaccion);


            // ==================================
            // REGISTRAR MOVIMIENTO DE CAJA
            // ==================================

            if (montoFinal > 0)
            {
                var movimiento =
                    new MovimientoCaja
                    {
                        TurnoCajaId =
                            turno.Id,

                        NumeroMovimiento =
                            $"MOV-{DateTime.Now:yyyyMMdd-HHmmssfff}",

                        Fecha =
                            DateTime.Now,

                        Tipo =
                            TipoMovimientoCaja.Cobro,

                        Monto =
                            montoFinal,

                        Concepto =
                            $"Cobro por {tipoServicio}",

                        Referencia =
                            numeroTransaccion,

                        UsuarioCajero =
                            usuarioCajero,

                        Sincronizado =
                            false
                    };


                db.MovimientosCaja.Add(
                    movimiento);


                // ==================================
                // ACTUALIZAR SALDO ESPERADO
                // SOLO SI ES EFECTIVO
                // ==================================

                if (formaPagoEnum ==
                    FormaPago.Efectivo)
                {
                    turno.SaldoEsperado +=
                        montoFinal;
                }
            }


            // ==================================
            // GUARDAR
            // ==================================

            await db.SaveChangesAsync();

            await dbTransaction
                .CommitAsync();

            return transaccion;
        }
        catch
        {
            await dbTransaction
                .RollbackAsync();

            throw;
        }
    }


    // ==========================================
    // HISTORIAL
    // ==========================================

    public async Task<List<Transaccion>>
        ObtenerHistorialAsync()
    {
        using var db =
            new AppDbContext();

        return await db.Transacciones
            .AsNoTracking()
            .OrderByDescending(
                x => x.Fecha)
            .ToListAsync();
    }

    // ==========================================
    // TRANSACCIONES DE UN PERÍODO (TURNO)
    // ==========================================

    public async Task<List<Transaccion>> ObtenerEntreAsync(
        DateTime desde,
        DateTime? hasta)
    {
        using var db =
            new AppDbContext();

        var query =
            db.Transacciones
                .AsNoTracking()
                .Where(x => x.Fecha >= desde);

        if (hasta.HasValue)
        {
            query = query.Where(x => x.Fecha <= hasta.Value);
        }

        return await query
            .OrderByDescending(x => x.Fecha)
            .ToListAsync();
    }
}
