using CajaApp.Helpers;
using Xunit;

namespace CajaApp.Tests;

public class CedulaHelperTests
{
    [Theory]
    [InlineData("001-1234567-8")]
    [InlineData("00112345678")]
    [InlineData("001 1234567 8")]
    public void EsValida_AceptaOnceDigitos(string cedula)
    {
        Assert.True(CedulaHelper.EsValida(cedula));
    }

    [Theory]
    [InlineData("")]
    [InlineData("001-123456-8")]
    [InlineData("001-1234567-89")]
    [InlineData("ABC-1234567-8")]
    [InlineData(null)]
    public void EsValida_RechazaFormatosIncorrectos(string? cedula)
    {
        Assert.False(CedulaHelper.EsValida(cedula));
    }

    [Fact]
    public void Normalizar_AgregaGuiones()
    {
        Assert.Equal("001-1234567-8", CedulaHelper.Normalizar("00112345678"));
    }

    [Fact]
    public void Enmascarar_OcultaLaParteCentral()
    {
        Assert.Equal("001-XXXX567-8", CedulaHelper.Enmascarar("001-1234567-8"));
        Assert.Equal("5678", CedulaHelper.Terminacion("001-1234567-8"));
    }
}
