using CajaApp.Data;
using QuestPDF.Infrastructure;
using Xunit;

// Las pruebas comparten la cadena de conexión estática de AppDbContext,
// por eso no se ejecutan en paralelo.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CajaApp.Tests;

/// <summary>
/// Crea una base SQLite temporal y vacía para cada clase de prueba.
/// Nunca toca el cajaapp.db real de la aplicación.
/// </summary>
public abstract class BaseDatosPrueba : IDisposable
{
    private readonly string _ruta;

    protected BaseDatosPrueba()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        _ruta = Path.Combine(
            Path.GetTempPath(),
            $"cajaapp-pruebas-{Guid.NewGuid():N}.db");

        AppDbContext.CadenaConexion = $"Data Source={_ruta};Pooling=False";

        using var db = new AppDbContext();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        try
        {
            File.Delete(_ruta);
        }
        catch
        {
            // Archivo temporal: si queda bloqueado se ignora.
        }

        GC.SuppressFinalize(this);
    }
}
