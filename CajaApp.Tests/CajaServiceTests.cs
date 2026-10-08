using CajaApp.Services;
using Xunit;

namespace CajaApp.Tests;

/// <summary>Apertura, cuadre y cierre de caja (RF-CAJ-01, RF-CAJ-02).</summary>
public class CajaServiceTests : BaseDatosPrueba
{
    private readonly CajaService _caja = new();
    private readonly TransaccionService _transacciones = new();

    [Fact]
    public async Task AbrirCaja_CreaTurnoConSaldoIgualAlBalanceInicial()
    {
        var turno = await _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");

        Assert.Equal(5000m, turno.BalanceInicial);
        Assert.Equal(5000m, turno.SaldoEsperado);
        Assert.NotNull(await _caja.ObtenerTurnoAbiertoAsync());
    }

    [Fact]
    public async Task AbrirCaja_NoPermiteDosTurnosAbiertos()
    {
        await _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 2", 1000m, ""));
    }

    [Fact]
    public async Task AbrirCaja_RechazaBalanceNegativo()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", -1m, ""));
    }

    [Fact]
    public async Task CerrarCaja_CalculaDiferenciaSoloConEfectivo()
    {
        await _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");

        var efectivo = await _transacciones.RegistrarAsync("001-1234567-8", "Consulta médica", 5000m, "Efectivo", false, "cajero");
        await Task.Delay(5);
        var tarjeta = await _transacciones.RegistrarAsync("001-1234567-8", "Laboratorio", 1000m, "Tarjeta", false, "cajero");
        await MarcarCompletadas(efectivo.Id, tarjeta.Id);

        // Esperado = 5,000 inicial + 5,000 efectivo (la tarjeta no cuenta).
        var turno = await _caja.CerrarCajaAsync(9900m, "Faltante por cambio entregado de más");

        Assert.Equal(-100m, turno.Diferencia);
        Assert.Null(await _caja.ObtenerTurnoAbiertoAsync());
    }

    private static async Task MarcarCompletadas(params int[] ids)
    {
        using var db = new CajaApp.Data.AppDbContext();
        foreach (var id in ids)
        {
            var t = await db.Transacciones.FindAsync(id);
            t!.Estado = CajaApp.Models.EstadoTransaccion.Completada;
        }
        await db.SaveChangesAsync();
    }
}
