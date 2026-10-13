using CajaApp.Data;
using CajaApp.Models;
using CajaApp.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CajaApp.Tests;

/// <summary>
/// Requisitos del profesor: Caja sigue operando con el CORE y la Web caídos,
/// emite recibos y hace el cuadre de caja con su reporte.
/// </summary>
public class OperacionContinuaTests : BaseDatosPrueba
{
    // Puerto cerrado: simula que Integración/CORE no responden.
    private const string ServidorCaido = "http://127.0.0.1:1/api";

    private static IConfiguration Config(string baseUrl) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = baseUrl,
                ["ApiSettings:UseMockAuthentication"] = "false"
            })
            .Build();

    private static string RutaSesionTemporal() =>
        Path.Combine(Path.GetTempPath(), $"sesion-{Guid.NewGuid():N}.json");

    [Fact]
    public async Task Login_ServidorCaido_UsuarioConocido_EntraSinConexion()
    {
        SesionOfflineStore.Ruta = RutaSesionTemporal();
        SesionOfflineStore.Guardar("cajero.prueba", "Clave123", "Cajero Prueba", new[] { "Farmacia", "Cajero" }, "TOKEN");

        var auth = new AuthService(new HttpClient { Timeout = TimeSpan.FromSeconds(3) }, Config(ServidorCaido));
        var r = await auth.LoginAsync("cajero.prueba", "Clave123");

        Assert.True(r.Exito);
        Assert.True(r.SinConexion);
        Assert.Contains("Cajero", r.Roles);
    }

    [Fact]
    public async Task Login_ServidorCaido_ClaveIncorrecta_EsRechazado()
    {
        SesionOfflineStore.Ruta = RutaSesionTemporal();
        SesionOfflineStore.Guardar("cajero.prueba", "Clave123", "Cajero Prueba", new[] { "Cajero" }, "TOKEN");

        var auth = new AuthService(new HttpClient { Timeout = TimeSpan.FromSeconds(3) }, Config(ServidorCaido));
        var r = await auth.LoginAsync("cajero.prueba", "otra");

        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Login_ServidorCaido_UsuarioNuncaConectado_EsRechazado()
    {
        SesionOfflineStore.Ruta = RutaSesionTemporal();

        var auth = new AuthService(new HttpClient { Timeout = TimeSpan.FromSeconds(3) }, Config(ServidorCaido));
        var r = await auth.LoginAsync("nuevo", "Clave123");

        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Subsidio_ServidorCaido_UsaElDatoGuardadoEnLaTerminal()
    {
        var api = new ApiService(new HttpClient { Timeout = TimeSpan.FromSeconds(3) }, Config(ServidorCaido), new SessionService());

        var r = await api.ValidarSubsidioAsync("001-1234567-8");

        Assert.True(r.SinConexion);
        Assert.True(r.Encontrado);
        Assert.True(r.Vigente);
    }

    [Fact]
    public async Task Subsidio_ServidorCaido_CedulaDesconocida_NoAplicaGratuidad()
    {
        var api = new ApiService(new HttpClient { Timeout = TimeSpan.FromSeconds(3) }, Config(ServidorCaido), new SessionService());

        var r = await api.ValidarSubsidioAsync("402-0000000-1");

        Assert.True(r.SinConexion);
        Assert.False(r.Encontrado);
    }

    [Fact]
    public async Task SinConexion_RegistraCobroYEmiteRecibo()
    {
        // Nada de esto llama a la red: todo queda en SQLite pendiente de sincronizar.
        await new CajaService().AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 500m, "");
        var t = await new TransaccionService().RegistrarAsync("001-1234567-8", "Consulta médica", 300m, "Efectivo", false, "cajero");
        var c = await new ComprobanteService().GenerarAsync(t.Id);

        Assert.True(File.Exists(c.RutaArchivo));
        Assert.False(c.Sincronizado);
    }

    [Fact]
    public async Task Cuadre_ConFaltante_GeneraReportePdf()
    {
        var caja = new CajaService();
        await caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 500m, "");
        var t = await new TransaccionService().RegistrarAsync("001-1234567-8", "Consulta médica", 300m, "Efectivo", false, "cajero");
        await new ComprobanteService().GenerarAsync(t.Id);

        // Esperado: 500 + 300 = 800. Contado: 790 → faltante de 10.
        var turno = await caja.CerrarCajaAsync(790m, "Faltan diez pesos en la gaveta");
        Assert.Equal(-10m, turno.Diferencia);

        var ruta = await new ReporteCuadreService().GenerarAsync(turno.Id);

        Assert.True(File.Exists(ruta));
        Assert.True(new FileInfo(ruta).Length > 1000);
    }

    [Fact]
    public async Task Cuadre_TurnoAbierto_NoGeneraReporte()
    {
        var turno = await new CajaService().AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 500m, "");

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReporteCuadreService().GenerarAsync(turno.Id));
    }
}
