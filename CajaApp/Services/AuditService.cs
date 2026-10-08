using CajaApp.Services.Interfaces;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace CajaApp.Services;

/// <summary>
/// Implementación de la bitácora con Serilog.
/// Escribe en Logs/caja-AAAAMMDD.log (un archivo por día, se conservan 30).
/// No requiere paquetes adicionales: el archivo lo escribe ArchivoDiarioSink.
/// </summary>
public class AuditService : IAuditService
{
    private readonly ISessionService _sessionService;

    private readonly ILogger _logger;

    private const string Plantilla =
        "[{Evento:l}] {Mensaje:l} | Usuario: {Usuario:l} | Terminal: {Terminal:l}";

    public string CarpetaLogs { get; }

    public AuditService(ISessionService sessionService)
        : this(sessionService, Path.Combine(AppContext.BaseDirectory, "Logs"))
    {
    }

    public AuditService(ISessionService sessionService, string carpetaLogs)
    {
        _sessionService = sessionService;

        CarpetaLogs = carpetaLogs;

        _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Sink(new ArchivoDiarioSink(carpetaLogs, diasRetencion: 30))
            .CreateLogger();
    }

    public void Info(string evento, string mensaje) =>
        Escribir(LogEventLevel.Information, evento, mensaje, null);

    public void Advertencia(string evento, string mensaje) =>
        Escribir(LogEventLevel.Warning, evento, mensaje, null);

    public void Error(string evento, string mensaje, Exception? excepcion = null) =>
        Escribir(LogEventLevel.Error, evento, mensaje, excepcion);

    private void Escribir(
        LogEventLevel nivel,
        string evento,
        string mensaje,
        Exception? excepcion)
    {
        try
        {
            string usuario = string.IsNullOrWhiteSpace(_sessionService.Usuario)
                ? "(sin sesión)"
                : _sessionService.Usuario;

            _logger.Write(
                nivel,
                excepcion,
                Plantilla,
                evento,
                mensaje,
                usuario,
                Environment.MachineName);
        }
        catch
        {
            // La bitácora nunca debe detener la operación de caja.
        }
    }
}

/// <summary>
/// Destino de Serilog que escribe un archivo de texto por día
/// y elimina los archivos más antiguos que el período de retención.
/// </summary>
public sealed class ArchivoDiarioSink : ILogEventSink
{
    private readonly string _carpeta;

    private readonly int _diasRetencion;

    private readonly object _bloqueo = new();

    private DateTime _ultimaLimpieza = DateTime.MinValue;

    public ArchivoDiarioSink(string carpeta, int diasRetencion)
    {
        _carpeta = carpeta;
        _diasRetencion = diasRetencion;
    }

    public void Emit(LogEvent logEvent)
    {
        var fecha = logEvent.Timestamp.LocalDateTime;

        var linea = new StringBuilder()
            .Append(fecha.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(' ')
            .Append(Nivel(logEvent.Level))
            .Append(' ')
            .Append(logEvent.RenderMessage());

        if (logEvent.Exception is not null)
        {
            linea.AppendLine().Append(logEvent.Exception);
        }

        lock (_bloqueo)
        {
            Directory.CreateDirectory(_carpeta);

            string archivo = Path.Combine(
                _carpeta,
                $"caja-{fecha:yyyyMMdd}.log");

            File.AppendAllText(
                archivo,
                linea.AppendLine().ToString(),
                Encoding.UTF8);

            LimpiarArchivosAntiguos();
        }
    }

    private void LimpiarArchivosAntiguos()
    {
        if ((DateTime.Now - _ultimaLimpieza).TotalHours < 12)
        {
            return;
        }

        _ultimaLimpieza = DateTime.Now;

        try
        {
            var antiguos = new DirectoryInfo(_carpeta)
                .GetFiles("caja-*.log")
                .OrderByDescending(f => f.Name)
                .Skip(_diasRetencion);

            foreach (var archivo in antiguos)
            {
                archivo.Delete();
            }
        }
        catch
        {
            // Si no se puede limpiar, se intenta en el próximo ciclo.
        }
    }

    private static string Nivel(LogEventLevel nivel) => nivel switch
    {
        LogEventLevel.Warning => "[WRN]",
        LogEventLevel.Error => "[ERR]",
        LogEventLevel.Fatal => "[FTL]",
        LogEventLevel.Debug => "[DBG]",
        LogEventLevel.Verbose => "[VRB]",
        _ => "[INF]"
    };
}
