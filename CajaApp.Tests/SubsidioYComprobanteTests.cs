using CajaApp.Models;
using CajaApp.Services;
using Xunit;

namespace CajaApp.Tests;

/// <summary>Validación de subsidio (RF-CAJ-06) y comprobante obligatorio (RN-05).</summary>
public class SubsidioYComprobanteTests : BaseDatosPrueba
{
    private readonly MockApiService _api = new();

    [Fact]
    public async Task Subsidio_PacienteActivo()
    {
        var r = await _api.ValidarSubsidioAsync("001-1234567-8");

        Assert.True(r.Encontrado);
        Assert.True(r.Vigente);
    }

    [Fact]
    public async Task Subsidio_AceptaCedulaSinGuiones()
    {
        var r = await _api.ValidarSubsidioAsync("00112345678");

        Assert.True(r.Encontrado);
    }

    [Fact]
    public async Task Subsidio_PacienteVencido()
    {
        var r = await _api.ValidarSubsidioAsync("001-9999999-9");

        Assert.True(r.Encontrado);
        Assert.False(r.Vigente);
    }

    [Fact]
    public async Task Subsidio_CedulaNoRegistrada()
    {
        var r = await _api.ValidarSubsidioAsync("402-0000000-1");

        Assert.False(r.Encontrado);
    }

    [Fact]
    public async Task Comprobante_CompletaLaTransaccionYCreaElPdf()
    {
        await new CajaService().AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");
        var transaccion = await new TransaccionService()
            .RegistrarAsync("001-1234567-8", "Consulta médica", 300m, "Efectivo", false, "cajero");

        var comprobante = await new ComprobanteService().GenerarAsync(transaccion.Id);

        Assert.True(File.Exists(comprobante.RutaArchivo));

        using var db = new CajaApp.Data.AppDbContext();
        var guardada = await db.Transacciones.FindAsync(transaccion.Id);
        Assert.Equal(EstadoTransaccion.Completada, guardada!.Estado);
        Assert.True(guardada.ComprobanteGenerado);
    }
}
