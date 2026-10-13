using System.Text.Json;
using CajaApp.Data;
using CajaApp.DTOs;
using CajaApp.Helpers;
using CajaApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CajaApp.Tests;

/// <summary>Acuerdos con el CORE: receta, roles y numeración de comprobantes.</summary>
public class IntegracionCoreTests : BaseDatosPrueba
{
    private readonly CajaService _caja = new();
    private readonly TransaccionService _transacciones = new();

    [Fact]
    public async Task Medicamentos_SinReceta_EsRechazado()
    {
        await _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _transacciones.RegistrarAsync("001-1234567-8", "Medicamentos ARV", 0m, "", true, "cajero", "  "));
    }

    [Fact]
    public async Task Medicamentos_ConReceta_GuardaElNumero()
    {
        await _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");

        var t = await _transacciones.RegistrarAsync("001-1234567-8", "Medicamentos ARV", 0m, "", true, "cajero", "  RX-2002 ");

        using var db = new AppDbContext();
        var guardada = await db.Transacciones.FindAsync(t.Id);
        Assert.Equal("RX-2002", guardada!.NumeroReceta);
    }

    [Fact]
    public async Task OtroServicio_NoExigeReceta()
    {
        await _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");

        var t = await _transacciones.RegistrarAsync("001-1234567-8", "Consulta médica", 300m, "Efectivo", false, "cajero");

        Assert.Null(t.NumeroReceta);
    }

    [Fact]
    public void AsegurarEsquema_AgregaColumnaEnBaseAnterior()
    {
        using var db = new AppDbContext();
        db.Database.ExecuteSqlRaw("ALTER TABLE \"Transacciones\" DROP COLUMN \"NumeroReceta\";");

        db.AsegurarEsquema();
        db.AsegurarEsquema(); // dos veces: no debe fallar

        var conexion = db.Database.GetDbConnection();
        conexion.Open();
        using var cmd = conexion.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Transacciones') WHERE name = 'NumeroReceta';";
        Assert.Equal(1L, (long)cmd.ExecuteScalar()!);
        conexion.Close();
    }

    [Fact]
    public async Task Comprobantes_UsanPrefijoYCorrelativo()
    {
        ComprobanteService.PrefijoTerminal = "CJ01";
        await _caja.AbrirCajaAsync("cajero", "María Rosario", "Ventanilla 1", 5000m, "");
        var servicio = new ComprobanteService();

        var t1 = await _transacciones.RegistrarAsync("001-1234567-8", "Consulta médica", 300m, "Efectivo", false, "cajero");
        await Task.Delay(5);
        var t2 = await _transacciones.RegistrarAsync("001-1234567-8", "Laboratorio", 650m, "Tarjeta", false, "cajero");

        var c1 = await servicio.GenerarAsync(t1.Id);
        var c2 = await servicio.GenerarAsync(t2.Id);

        Assert.Equal("CJ01-000001", c1.NumeroComprobante);
        Assert.Equal("CJ01-000002", c2.NumeroComprobante);
    }

    [Theory]
    [InlineData("cj02", "CJ02")]
    [InlineData(" CJ-03 ", "CJ03")]
    [InlineData("", "CJ01")]
    [InlineData(null, "CJ01")]
    public void Prefijo_SeNormaliza(string? valor, string esperado)
    {
        Assert.Equal(esperado, ComprobanteService.NormalizarPrefijo(valor));
    }

    [Fact]
    public void Roles_EligeCajeroEntreRolesAjenos()
    {
        Assert.Equal("Cajero", RolesHelper.ResolverRolCaja(new[] { "Farmacia", "cajero" }));
    }

    [Fact]
    public void Roles_AdministradorTienePrioridad()
    {
        Assert.Equal("Administrador", RolesHelper.ResolverRolCaja(new[] { "Cajero", "Admin" }));
    }

    [Fact]
    public void Roles_SinRolDeCaja_EsNull()
    {
        Assert.Null(RolesHelper.ResolverRolCaja(new[] { "Medico", "Farmacia" }, null));
    }

    [Theory]
    [InlineData("{\"exito\":true,\"rol\":\"Cajero\"}", "Cajero")]
    [InlineData("{\"exito\":true,\"roles\":[\"Farmacia\",\"Cajero\"]}", "Cajero")]
    [InlineData("{\"exito\":true,\"rol\":[\"Auditor\"]}", "Auditor")]
    [InlineData("{\"exito\":true,\"roles\":null}", null)]
    public void Login_AceptaRolTextoOLista(string json, string? esperado)
    {
        var dto = JsonSerializer.Deserialize<LoginResponseDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.Equal(esperado, RolesHelper.ResolverRolCaja(dto.Rol, dto.Roles));
    }
}
