using CajaApp.Data;
using CajaApp.Models;
using CajaApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CajaApp.Tests;

/// <summary>Reglas de cobro, subsidio e integridad (RF-CAJ-04, RF-CAJ-07, RN-02).</summary>
public class TransaccionServiceTests : BaseDatosPrueba
{
    private readonly CajaService _caja = new();
    private readonly TransaccionService _transacciones = new();

    [Fact]
    public async Task Registrar_SinTurnoAbierto_EsRechazada()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transacciones.RegistrarAsync("001-1234567-8", "Consulta médica", 300m, "Efectivo", false, "cajero"));
    }

    [Fact]
    public async Task Registrar_Subsidiada_DejaMontoEnCeroYSinMovimiento()
    {
        await _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");

        var t = await _transacciones.RegistrarAsync("00112345678", "Medicamentos ARV", 1500m, "Efectivo", true, "cajero");

        Assert.Equal(0m, t.Monto);
        Assert.Equal(FormaPago.Subsidiado, t.FormaPago);
        Assert.Equal("001-1234567-8", t.CedulaPaciente);

        using var db = new AppDbContext();
        Assert.Equal(0, await db.MovimientosCaja.CountAsync());
    }

    [Fact]
    public async Task Registrar_EfectivoAumentaSaldoYTarjetaNo()
    {
        await _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");

        await _transacciones.RegistrarAsync("001-1234567-8", "Consulta médica", 300m, "Efectivo", false, "cajero");
        await Task.Delay(5);
        await _transacciones.RegistrarAsync("001-1234567-8", "Laboratorio", 650m, "Tarjeta", false, "cajero");

        var turno = await _caja.ObtenerTurnoAbiertoAsync();
        Assert.Equal(5300m, turno!.SaldoEsperado);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("001-ABCDEFG-8")]
    public async Task Registrar_CedulaInvalida_EsRechazada(string cedula)
    {
        await _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transacciones.RegistrarAsync(cedula, "Consulta médica", 300m, "Efectivo", false, "cajero"));
    }

    [Fact]
    public async Task Registrar_CobroSinMonto_EsRechazado()
    {
        await _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transacciones.RegistrarAsync("001-1234567-8", "Consulta médica", 0m, "Efectivo", false, "cajero"));
    }

    [Fact]
    public async Task BaseDeDatos_RestriccionCheck_ImpideMontoNegativo()
    {
        using var db = new AppDbContext();

        db.Transacciones.Add(new Transaccion
        {
            NumeroTransaccion = "TX-PRUEBA-NEGATIVO",
            CedulaPaciente = "001-1234567-8",
            TipoServicio = "Consulta médica",
            Monto = -50m,
            FormaPago = FormaPago.Efectivo,
            UsuarioCajero = "cajero"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task BaseDeDatos_RestriccionCheck_SubsidiadaDebeSerCero()
    {
        using var db = new AppDbContext();

        db.Transacciones.Add(new Transaccion
        {
            NumeroTransaccion = "TX-PRUEBA-SUBSIDIO",
            CedulaPaciente = "001-1234567-8",
            TipoServicio = "Medicamentos ARV",
            Monto = 100m,
            FormaPago = FormaPago.Subsidiado,
            EsSubsidiado = true,
            UsuarioCajero = "cajero"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
