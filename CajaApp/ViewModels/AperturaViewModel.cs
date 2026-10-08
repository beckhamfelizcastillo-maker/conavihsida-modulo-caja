using CajaApp.Helpers;
using CajaApp.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CajaApp.ViewModels;

/// <summary>Apertura del turno de caja (RF-CAJ-01).</summary>
public partial class AperturaViewModel : ViewModelBase
{
    private readonly ICajaService _cajaService;
    private readonly ISessionService _sessionService;
    private readonly ITurnoStateService _turnoStateService;
    private readonly INavegacionCaja _navegacion;
    private readonly IAuditService _auditService;

    private string _caja = "Ventanilla 2 — CONAVIHSIDA Central";
    private string _balanceInicialTexto = "5000";
    private string _observacion = string.Empty;
    private string _mensaje = string.Empty;
    private bool _isLoading;
    private bool _aperturaExitosa;
    private bool _yaHayTurnoAbierto;
    private string _turnoConfirmado = string.Empty;
    private string _cajaConfirmada = string.Empty;
    private string _balanceConfirmado = string.Empty;
    private string _horaConfirmada = string.Empty;

    public IReadOnlyList<string> CajasDisponibles { get; } =
        new[]
        {
            "Ventanilla 1 — CONAVIHSIDA Central",
            "Ventanilla 2 — CONAVIHSIDA Central",
            "Ventanilla 3 — Unidad Santiago"
        };

    public string Caja
    {
        get => _caja;
        set => SetProperty(ref _caja, value);
    }

    public string FechaTurno => Formato.FechaLarga(DateTime.Now);

    public string Cajero => _sessionService.NombreCompleto;

    /// <summary>Lo que el usuario escribe en el campo de balance inicial.</summary>
    public string BalanceInicialTexto
    {
        get => _balanceInicialTexto;
        set
        {
            if (SetProperty(ref _balanceInicialTexto, value))
            {
                OnPropertyChanged(nameof(BalanceVistaPrevia));
            }
        }
    }

    public string BalanceVistaPrevia =>
        Formato.TryParseMonto(BalanceInicialTexto, out var monto)
            ? Formato.Moneda(monto)
            : "Monto no válido";

    public string Observacion
    {
        get => _observacion;
        set => SetProperty(ref _observacion, value);
    }

    public string Mensaje
    {
        get => _mensaje;
        set
        {
            if (SetProperty(ref _mensaje, value))
            {
                OnPropertyChanged(nameof(HayMensaje));
            }
        }
    }

    public bool HayMensaje => !string.IsNullOrWhiteSpace(Mensaje);

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // ==========================================
    // ESTADOS DE LA PANTALLA
    // ==========================================

    public bool AperturaExitosa
    {
        get => _aperturaExitosa;
        set
        {
            if (SetProperty(ref _aperturaExitosa, value))
            {
                OnPropertyChanged(nameof(MostrarFormulario));
            }
        }
    }

    public bool YaHayTurnoAbierto
    {
        get => _yaHayTurnoAbierto;
        set
        {
            if (SetProperty(ref _yaHayTurnoAbierto, value))
            {
                OnPropertyChanged(nameof(MostrarFormulario));
            }
        }
    }

    public bool MostrarFormulario => !AperturaExitosa && !YaHayTurnoAbierto;

    public string TurnoConfirmado
    {
        get => _turnoConfirmado;
        set => SetProperty(ref _turnoConfirmado, value);
    }

    public string CajaConfirmada
    {
        get => _cajaConfirmada;
        set => SetProperty(ref _cajaConfirmada, value);
    }

    public string BalanceConfirmado
    {
        get => _balanceConfirmado;
        set => SetProperty(ref _balanceConfirmado, value);
    }

    public string HoraConfirmada
    {
        get => _horaConfirmada;
        set => SetProperty(ref _horaConfirmada, value);
    }

    // ==========================================
    // COMANDOS
    // ==========================================

    public IAsyncRelayCommand AbrirCajaCommand { get; }
    public IRelayCommand IrInicioCommand { get; }
    public IRelayCommand IrTransaccionCommand { get; }
    public IRelayCommand IrCierreCommand { get; }

    public AperturaViewModel(
        ICajaService cajaService,
        ISessionService sessionService,
        ITurnoStateService turnoStateService,
        INavegacionCaja navegacion,
        IAuditService auditService)
    {
        _cajaService = cajaService;
        _sessionService = sessionService;
        _turnoStateService = turnoStateService;
        _navegacion = navegacion;
        _auditService = auditService;

        AbrirCajaCommand = new AsyncRelayCommand(AbrirCajaAsync);
        IrInicioCommand = new RelayCommand(() => _navegacion.Ir(PaginaCaja.Inicio));
        IrTransaccionCommand = new RelayCommand(() => _navegacion.Ir(PaginaCaja.Transaccion));
        IrCierreCommand = new RelayCommand(() => _navegacion.Ir(PaginaCaja.Cierre));
    }

    /// <summary>Se llama cada vez que se entra a la pantalla.</summary>
    public async Task PrepararAsync()
    {
        OnPropertyChanged(nameof(FechaTurno));
        OnPropertyChanged(nameof(Cajero));

        var turno = await _cajaService.ObtenerTurnoAbiertoAsync();

        if (turno is null)
        {
            // No hay turno: formulario limpio (también después de un cierre).
            if (AperturaExitosa || YaHayTurnoAbierto)
            {
                Reiniciar();
            }

            return;
        }

        if (AperturaExitosa)
        {
            // Se mantiene la confirmación del turno recién abierto.
            return;
        }

        Mensaje = string.Empty;
        YaHayTurnoAbierto = true;

        if (turno is not null)
        {
            TurnoConfirmado = turno.NumeroTurno;
            CajaConfirmada = turno.Caja;
            BalanceConfirmado = Formato.Moneda(turno.BalanceInicial);
            HoraConfirmada = Formato.Hora(turno.FechaApertura);
        }
    }

    private async Task AbrirCajaAsync()
    {
        try
        {
            IsLoading = true;
            Mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(Caja))
            {
                Mensaje = "Selecciona la caja o ventanilla del turno.";
                return;
            }

            if (!Formato.TryParseMonto(BalanceInicialTexto, out decimal balance))
            {
                Mensaje = "Escribe el balance inicial en números, por ejemplo 5000 o 5,000.00.";
                return;
            }

            if (balance < 0)
            {
                Mensaje = "El balance inicial no puede ser negativo.";
                return;
            }

            if (balance > Formato.MontoMaximoOperacion)
            {
                Mensaje = $"El balance inicial no puede superar {Formato.Moneda(Formato.MontoMaximoOperacion)}.";
                return;
            }

            var turno = await _cajaService.AbrirCajaAsync(
                _sessionService.Usuario,
                _sessionService.NombreCompleto,
                Caja,
                balance,
                Observacion.Trim());

            TurnoConfirmado = turno.NumeroTurno;
            CajaConfirmada = turno.Caja;
            BalanceConfirmado = Formato.Moneda(turno.BalanceInicial);
            HoraConfirmada = Formato.Hora(turno.FechaApertura);
            AperturaExitosa = true;

            _auditService.Info(
                EventosAuditoria.AperturaCaja,
                $"Turno {turno.NumeroTurno} abierto en {turno.Caja} con balance inicial {Formato.Moneda(turno.BalanceInicial)}.");

            await _turnoStateService.ActualizarAsync();
        }
        catch (Exception ex)
        {
            Mensaje = ex.Message;
            _auditService.Advertencia(EventosAuditoria.AperturaCaja, $"Apertura rechazada: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Se llama después del cierre para dejar el formulario listo.</summary>
    public void Reiniciar()
    {
        AperturaExitosa = false;
        YaHayTurnoAbierto = false;
        BalanceInicialTexto = "5000";
        Observacion = string.Empty;
        Mensaje = string.Empty;
    }
}
