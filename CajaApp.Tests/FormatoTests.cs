using CajaApp.Helpers;
using Xunit;

namespace CajaApp.Tests;

public class FormatoTests
{
    [Theory]
    [InlineData("5000", 5000)]
    [InlineData("5,000.50", 5000.50)]
    [InlineData("RD$ 1,250", 1250)]
    [InlineData("300,5", 300.5)]
    [InlineData(" 1 500 ", 1500)]
    [InlineData("0", 0)]
    public void TryParseMonto_AceptaFormatosComunes(string texto, double esperado)
    {
        bool ok = Formato.TryParseMonto(texto, out decimal monto);

        Assert.True(ok);
        Assert.Equal((decimal)esperado, monto);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("RD$")]
    [InlineData(null)]
    public void TryParseMonto_RechazaTextoNoNumerico(string? texto)
    {
        Assert.False(Formato.TryParseMonto(texto, out _));
    }

    [Fact]
    public void Moneda_UsaFormatoDominicano()
    {
        Assert.Equal("RD$ 1,234.50", Formato.Moneda(1234.5m));
        Assert.Equal("RD$ 0.00", Formato.Moneda(0m));
    }

    [Fact]
    public void Iniciales_TomaPrimerYUltimoNombre()
    {
        Assert.Equal("MR", Formato.Iniciales("María Rosario"));
        Assert.Equal("JP", Formato.Iniciales("Juan Carlos Pérez"));
    }
}
