using CajaApp.Helpers;
using CajaApp.Services.Interfaces;
using CajaApp.ViewModels.Items;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CajaApp.ViewModels;

/// <summary>Historial de operaciones con búsqueda y reimpresión de comprobantes.</summary>
public partial class HistorialViewModel : ViewModelBase
{
    private readonly ITransaccionService _transaccionService;
    private readonly IComprobanteService _comprobanteService;
    private readonly ICajaService _cajaService;
    private readonly IAuditService _auditService;

    private List<TransaccionItem> _todas = new();
    private string _filtro = string.Empty;
    private bool _soloTurnoActual;
    private bool _hayTurnoAbierto;
    private DateTime? _aperturaTurno;
    private string _mensaje = string.Empty;
    private bool _mensajeEsError;
    private string _resumen = string.Empty;
    private string _totalCobrado = Formato.Moneda(0);
    private int _cantidadSubsidiadas;
    private int _cantidadPendientes;

    public ObservableCollection<TransaccionItem> Transacciones { get; } = new();

    public string Filtro
    {
        get => _filtro;
        set
        {
            if (SetProperty(ref _filtro, value))
            {
                AplicarFiltro();
            }
        }
    }

    public bool SoloTurnoActual
    {
        get => _soloTurnoActual;
        set
        {
            if (SetProperty(ref _soloTurnoActual, value))
            {
                OnPropertyChanged(nameof(TodasActivas));
                AplicarFiltro();
            }
        }
    }

    public bool TodasActivas => !SoloTurnoActual;

    public bool HayTurnoAbierto
    {
        get => _hayTurnoAbierto;
        set => SetProperty(ref _hayTurnoAbierto, value);
    }

    public string Resumen
    {
        get => _resumen;
        set => SetProperty(ref _resumen, value);
    }

    public string TotalCobrado
    {
        get => _totalCobrado;
        set => SetProperty(ref _totalCobrado, value);
    }

    public int CantidadSubsidiadas
    {
        get => _cantidadSubsidiadas;
        set => SetProperty(ref _cantidadSubsidiadas, value);
    }

    public int CantidadPendientes
    {
        get => _cantidadPendientes;
        set => SetProperty(ref _cantidadPendientes, value);
    }

    public int CantidadMostradas => Transacciones.Count;

    public bool SinResultados => Transacciones.Count == 0;

    public string Mensaje
    {
        get => _mensaje;
        set
        {
            if (SetProperty(ref _mensaje, value))
            {
                OnPropertyChanged(nameof(MensajeExito));
                OnPropertyChanged(nameof(MensajeError));
            }
        }
    }

    public bool MensajeExito => !string.IsNullOrWhiteSpace(Mensaje) && !_mensajeEsError;
    public bool MensajeError => !string.IsNullOrWhiteSpace(Mensaje) && _mensajeEsError;

    public IAsyncRelayCommand ActualizarCommand { get; }
    public IAsyncRelayCommand<TransaccionItem> ReimprimirCommand { get; }
    public IRelayCommand VerTodasCommand { get; }
    public IRelayCommand VerTurnoCommand { get; }

    public HistorialViewModel(
        ITransaccionService transaccionService,
        IComprobanteService comprobanteService,
        ICajaService cajaService,
        IAuditService auditService)
    {
        _transaccionService = transaccionService;
        _comprobanteService = comprobanteService;
        _cajaService = cajaService;
        _auditService = auditService;

        ActualizarCommand = new AsyncRelayCommand(CargarAsync);
        ReimprimirCommand = new AsyncRelayCommand<TransaccionItem>(ReimprimirAsync);
        VerTodasCommand = new RelayCommand(() => SoloTurnoActual = false);
        VerTurnoCommand = new RelayCommand(() => SoloTurnoActual = true);
    }

    public async Task CargarAsync()
    {
        try
        {
            var turno = await _cajaService.ObtenerTurnoAbiertoAsync();
            HayTurnoAbierto = turno is not null;
            _aperturaTurno = turno?.FechaApertura;

            if (!HayTurnoAbierto && SoloTurnoActual)
            {
                SoloTurnoActual = false;
            }

            var datos = await _transaccionService.ObtenerHistorialAsync();
            _todas = datos.Select(t => new TransaccionItem(t)).ToList();

            Mensaje = string.Empty;
            AplicarFiltro();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"No se pudo cargar el historial: {ex.Message}", true);
        }
    }

    private void AplicarFiltro()
    {
        IEnumerable<TransaccionItem> consulta = _todas;

        if (SoloTurnoActual && _aperturaTurno.HasValue)
        {
            consulta = consulta.Where(t => t.Transaccion.Fecha >= _aperturaTurno.Value);
        }

        string texto = (Filtro ?? string.Empty).Trim().ToLowerInvariant();

        if (texto.Length > 0)
        {
            consulta = consulta.Where(t => t.TextoBusqueda.Contains(texto));
        }

        var lista = consulta.ToList();

        Transacciones.Clear();
        foreach (var item in lista)
        {
            Transacciones.Add(item);
        }

        TotalCobrado = Formato.Moneda(lista.Where(t => t.EsCobrado).Sum(t => t.Transaccion.Monto));
        CantidadSubsidiadas = lista.Count(t => t.EsSubsidiado);
        CantidadPendientes = lista.Count(t => t.Pendiente);
        Resumen = lista.Count == _todas.Count
            ? $"{lista.Count} operación(es) registradas"
            : $"{lista.Count} de {_todas.Count} operación(es)";

        OnPropertyChanged(nameof(CantidadMostradas));
        OnPropertyChanged(nameof(SinResultados));
    }

    private async Task ReimprimirAsync(TransaccionItem? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            string ruta = await _comprobanteService.ReimprimirAsync(item.Id);

            Process.Start(new ProcessStartInfo
            {
                FileName = ruta,
                UseShellExecute = true
            });

            _auditService.Info(
                EventosAuditoria.Reimpresion,
                $"Reimpresión del comprobante de {item.Numero}.");

            MostrarMensaje($"Comprobante de {item.Numero} abierto (queda registrada la reimpresión).", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje($"No se pudo abrir el comprobante: {ex.Message}", true);
            _auditService.Error(EventosAuditoria.Reimpresion, $"Error al reimprimir {item.Numero}.", ex);
        }
    }

    private void MostrarMensaje(string texto, bool esError)
    {
        _mensajeEsError = esError;
        Mensaje = string.Empty;
        Mensaje = texto;
    }
}
