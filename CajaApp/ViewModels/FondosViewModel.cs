using CajaApp.Helpers;
using CajaApp.Models;
using CajaApp.Services.Interfaces;
using CajaApp.ViewModels.Items;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CajaApp.ViewModels;

/// <summary>Fondos estatales y de cooperación internacional (RF-CAJ-05).</summary>
public partial class FondosViewModel : ViewModelBase
{
    private readonly IFondoService _fondoService;
    private readonly IAuditService _auditService;

    private string _codigo = string.Empty;
    private string _nombre = string.Empty;
    private string _organismo = string.Empty;
    private string _tipoFuente = "Presupuesto Estatal";
    private string _montoAsignadoTexto = string.Empty;
    private DateTimeOffset? _fechaInicio = DateTimeOffset.Now.Date;
    private DateTimeOffset? _fechaFin = DateTimeOffset.Now.Date.AddYears(1);
    private string _mensaje = string.Empty;
    private bool _mensajeEsError;
    private bool _mostrarFormulario;
    private string _totalAsignado = Formato.Moneda(0);
    private string _totalUtilizado = Formato.Moneda(0);
    private string _totalDisponible = Formato.Moneda(0);
    private string _resumen = string.Empty;

    public ObservableCollection<FondoItem> Fondos { get; } = new();

    public IReadOnlyList<string> TiposFuente { get; } =
        new[]
        {
            "Presupuesto Estatal",
            "Donación Internacional"
        };

    // ==========================================
    // FORMULARIO
    // ==========================================

    public string Codigo
    {
        get => _codigo;
        set => SetProperty(ref _codigo, value);
    }

    public string Nombre
    {
        get => _nombre;
        set => SetProperty(ref _nombre, value);
    }

    public string Organismo
    {
        get => _organismo;
        set => SetProperty(ref _organismo, value);
    }

    public string TipoFuente
    {
        get => _tipoFuente;
        set => SetProperty(ref _tipoFuente, value);
    }

    public string MontoAsignadoTexto
    {
        get => _montoAsignadoTexto;
        set => SetProperty(ref _montoAsignadoTexto, value);
    }

    public DateTimeOffset? FechaInicio
    {
        get => _fechaInicio;
        set => SetProperty(ref _fechaInicio, value);
    }

    public DateTimeOffset? FechaFin
    {
        get => _fechaFin;
        set => SetProperty(ref _fechaFin, value);
    }

    public bool MostrarFormulario
    {
        get => _mostrarFormulario;
        set
        {
            if (SetProperty(ref _mostrarFormulario, value))
            {
                OnPropertyChanged(nameof(TextoBotonFormulario));
            }
        }
    }

    public string TextoBotonFormulario => MostrarFormulario ? "Ocultar formulario" : "Registrar nueva partida";

    // ==========================================
    // TOTALES
    // ==========================================

    public string TotalAsignado
    {
        get => _totalAsignado;
        set => SetProperty(ref _totalAsignado, value);
    }

    public string TotalUtilizado
    {
        get => _totalUtilizado;
        set => SetProperty(ref _totalUtilizado, value);
    }

    public string TotalDisponible
    {
        get => _totalDisponible;
        set => SetProperty(ref _totalDisponible, value);
    }

    public string Resumen
    {
        get => _resumen;
        set => SetProperty(ref _resumen, value);
    }

    public bool SinFondos => Fondos.Count == 0;

    // ==========================================
    // MENSAJES
    // ==========================================

    public string Mensaje
    {
        get => _mensaje;
        set
        {
            if (SetProperty(ref _mensaje, value))
            {
                OnPropertyChanged(nameof(HayMensaje));
                OnPropertyChanged(nameof(MensajeExito));
                OnPropertyChanged(nameof(MensajeError));
            }
        }
    }

    public bool HayMensaje => !string.IsNullOrWhiteSpace(Mensaje);
    public bool MensajeExito => HayMensaje && !_mensajeEsError;
    public bool MensajeError => HayMensaje && _mensajeEsError;

    // ==========================================
    // COMANDOS
    // ==========================================

    public IAsyncRelayCommand CrearFondoCommand { get; }
    public IAsyncRelayCommand ActualizarCommand { get; }
    public IAsyncRelayCommand<FondoItem> CambiarEstadoCommand { get; }
    public IRelayCommand AlternarFormularioCommand { get; }

    public FondosViewModel(
        IFondoService fondoService,
        IAuditService auditService)
    {
        _fondoService = fondoService;
        _auditService = auditService;

        CrearFondoCommand = new AsyncRelayCommand(CrearFondoAsync);
        ActualizarCommand = new AsyncRelayCommand(CargarAsync);
        CambiarEstadoCommand = new AsyncRelayCommand<FondoItem>(CambiarEstadoAsync);
        AlternarFormularioCommand = new RelayCommand(() =>
        {
            MostrarFormulario = !MostrarFormulario;
            Mensaje = string.Empty;
        });
    }

    // ==========================================
    // CARGA
    // ==========================================

    public async Task CargarAsync()
    {
        try
        {
            var datos = await _fondoService.ObtenerTodosAsync();

            Fondos.Clear();

            foreach (var fondo in datos.OrderByDescending(f => f.Activo).ThenBy(f => f.Codigo))
            {
                Fondos.Add(new FondoItem(fondo));
            }

            var activos = datos.Where(f => f.Activo).ToList();

            TotalAsignado = Formato.Moneda(activos.Sum(f => f.MontoAsignado));
            TotalUtilizado = Formato.Moneda(activos.Sum(f => f.MontoUtilizado));
            TotalDisponible = Formato.Moneda(activos.Sum(f => f.SaldoDisponible));
            Resumen = $"{activos.Count} activo(s) de {datos.Count} registrado(s)";

            OnPropertyChanged(nameof(SinFondos));
        }
        catch (Exception ex)
        {
            MostrarMensaje($"No se pudieron cargar los fondos: {ex.Message}", true);
        }
    }

    // ==========================================
    // CREAR
    // ==========================================

    private async Task CrearFondoAsync()
    {
        if (string.IsNullOrWhiteSpace(Codigo))
        {
            MostrarMensaje("Escribe el código del fondo (por ejemplo FONDO-EST-2027).", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            MostrarMensaje("Escribe el nombre del fondo o convenio.", true);
            return;
        }

        if (!Formato.TryParseMonto(MontoAsignadoTexto, out decimal monto) || monto <= 0)
        {
            MostrarMensaje("Escribe un monto asignado mayor que cero.", true);
            return;
        }

        if (!FechaInicio.HasValue)
        {
            MostrarMensaje("Selecciona la fecha de inicio.", true);
            return;
        }

        if (FechaFin.HasValue && FechaFin.Value.Date < FechaInicio.Value.Date)
        {
            MostrarMensaje("La fecha final no puede ser anterior a la fecha de inicio.", true);
            return;
        }

        try
        {
            var tipo = TipoFuente == "Donación Internacional"
                ? TipoFuenteFondo.DonacionInternacional
                : TipoFuenteFondo.PresupuestoEstatal;

            var fondo = await _fondoService.CrearAsync(
                Codigo,
                Nombre,
                tipo,
                Organismo,
                monto,
                FechaInicio.Value.Date,
                FechaFin?.Date);

            _auditService.Info(
                EventosAuditoria.Fondo,
                $"Fondo {fondo.Codigo} registrado por {Formato.Moneda(fondo.MontoAsignado)}.");

            LimpiarFormulario();
            MostrarFormulario = false;
            await CargarAsync();
            MostrarMensaje($"Fondo {fondo.Codigo} registrado correctamente.", false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(ex.Message, true);
        }
    }

    // ==========================================
    // ACTIVAR / DESACTIVAR
    // ==========================================

    private async Task CambiarEstadoAsync(FondoItem? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            bool nuevoEstado = !item.Activo;

            await _fondoService.CambiarEstadoAsync(item.Id, nuevoEstado);

            _auditService.Info(
                EventosAuditoria.Fondo,
                $"Fondo {item.Codigo} {(nuevoEstado ? "activado" : "desactivado")}.");

            await CargarAsync();

            MostrarMensaje(
                nuevoEstado
                    ? $"Fondo {item.Codigo} activado."
                    : $"Fondo {item.Codigo} desactivado.",
                false);
        }
        catch (Exception ex)
        {
            MostrarMensaje(ex.Message, true);
        }
    }

    private void MostrarMensaje(string texto, bool esError)
    {
        _mensajeEsError = esError;
        Mensaje = string.Empty;
        Mensaje = texto;
    }

    private void LimpiarFormulario()
    {
        Codigo = string.Empty;
        Nombre = string.Empty;
        Organismo = string.Empty;
        TipoFuente = "Presupuesto Estatal";
        MontoAsignadoTexto = string.Empty;
        FechaInicio = DateTimeOffset.Now.Date;
        FechaFin = DateTimeOffset.Now.Date.AddYears(1);
    }
}
