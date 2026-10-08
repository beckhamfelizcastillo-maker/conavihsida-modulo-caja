using CajaApp.Helpers;
using CajaApp.Models;
using CajaApp.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CajaApp.ViewModels;

/// <summary>
/// Cuadre y cierre del turno (RF-CAJ-01, RF-CAJ-02).
/// Efectivo esperado = balance inicial + cobros en efectivo.
/// Diferencia = efectivo contado − efectivo esperado.
/// </summary>
public partial class CierreViewModel : ViewModelBase
{
    private readonly ICajaService _cajaService;
    private readonly ITransaccionService _transaccionService;
    private readonly ITurnoStateService _turnoStateService;
    private readonly INavegacionCaja _navegacion;
    private readonly IAuditService _auditService;

    private TurnoCaja? _turnoActual;
    private string _numeroTurno = "—";
    private string _cajero = "—";
    private string _caja = "—";
    private string _horaApertura = "—";
    private decimal _balanceInicial;
    private decimal _totalCobros;
    private decimal _totalEfectivo;
    private decimal _totalTarjeta;
    private int _cantidadOperaciones;
    private int _cantidadSubsidiadas;
    private string _efectivoDeclaradoTexto = string.Empty;
    private string _observacion = string.Empty;
    private string _mensaje = string.Empty;
    private bool _hayTurnoAbierto;
    private bool _turnoCerrado;
    private string _resultadoCierre = string.Empty;
    private bool _cierreConDiferencia;
    private bool _isLoading;

    // ==========================================
    // DATOS DEL TURNO
    // ==========================================

    public string NumeroTurno
    {
        get => _numeroTurno;
        set => SetProperty(ref _numeroTurno, value);
    }

    public string Cajero
    {
        get => _cajero;
        set => SetProperty(ref _cajero, value);
    }

    public string Caja
    {
        get => _caja;
        set => SetProperty(ref _caja, value);
    }

    public string HoraApertura
    {
        get => _horaApertura;
        set => SetProperty(ref _horaApertura, value);
    }

    public string HoraActual => Formato.Hora(DateTime.Now);

    public int CantidadOperaciones
    {
        get => _cantidadOperaciones;
        set => SetProperty(ref _cantidadOperaciones, value);
    }

    public int CantidadSubsidiadas
    {
        get => _cantidadSubsidiadas;
        set => SetProperty(ref _cantidadSubsidiadas, value);
    }

    // ==========================================
    // MONTOS
    // ==========================================

    public string BalanceInicialTexto => Formato.Moneda(_balanceInicial);
    public string TotalCobrosTexto => Formato.Moneda(_totalCobros);
    public string TotalEfectivoTexto => Formato.Moneda(_totalEfectivo);
    public string TotalTarjetaTexto => Formato.Moneda(_totalTarjeta);

    public decimal EfectivoEsperado => _balanceInicial + _totalEfectivo;

    public string EfectivoEsperadoTexto => Formato.Moneda(EfectivoEsperado);

    public string EfectivoDeclaradoTexto
    {
        get => _efectivoDeclaradoTexto;
        set
        {
            if (SetProperty(ref _efectivoDeclaradoTexto, value))
            {
                NotificarCuadre();
            }
        }
    }

    public bool HaDeclarado =>
        Formato.TryParseMonto(EfectivoDeclaradoTexto, out var monto) && monto >= 0;

    public bool SinDeclarar => !HaDeclarado;

    public decimal Diferencia =>
        Formato.TryParseMonto(EfectivoDeclaradoTexto, out var declarado)
            ? declarado - EfectivoEsperado
            : 0m;

    public bool EsCuadreExacto => HaDeclarado && Diferencia == 0;
    public bool EsSobrante => HaDeclarado && Diferencia > 0;
    public bool EsFaltante => HaDeclarado && Diferencia < 0;

    public string ContadoTexto =>
        Formato.TryParseMonto(EfectivoDeclaradoTexto, out var declarado)
            ? Formato.Moneda(declarado)
            : "—";

    public string DiferenciaTexto => HaDeclarado ? Formato.MonedaConSigno(Diferencia) : "—";

    public string MensajeCuadre => !HaDeclarado
        ? "Escribe el efectivo contado para calcular la diferencia frente a lo esperado."
        : Diferencia == 0
            ? "Cuadre exacto. No hay diferencia."
            : Diferencia > 0
                ? $"Sobrante de {Formato.Moneda(Diferencia)} frente a lo esperado. Explica la causa en la observación."
                : $"Faltante de {Formato.Moneda(Math.Abs(Diferencia))} frente a lo esperado. Explica la causa en la observación.";

    public string Observacion
    {
        get => _observacion;
        set => SetProperty(ref _observacion, value);
    }

    // ==========================================
    // ESTADO
    // ==========================================

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

    public bool HayTurnoAbierto
    {
        get => _hayTurnoAbierto;
        set
        {
            if (SetProperty(ref _hayTurnoAbierto, value))
            {
                OnPropertyChanged(nameof(MostrarCuadre));
                OnPropertyChanged(nameof(SinTurno));
            }
        }
    }

    public bool TurnoCerrado
    {
        get => _turnoCerrado;
        set
        {
            if (SetProperty(ref _turnoCerrado, value))
            {
                OnPropertyChanged(nameof(MostrarCuadre));
                OnPropertyChanged(nameof(SinTurno));
            }
        }
    }

    public bool MostrarCuadre => HayTurnoAbierto && !TurnoCerrado;

    public bool SinTurno => !HayTurnoAbierto && !TurnoCerrado;

    public string ResultadoCierre
    {
        get => _resultadoCierre;
        set => SetProperty(ref _resultadoCierre, value);
    }

    public bool CierreConDiferencia
    {
        get => _cierreConDiferencia;
        set
        {
            if (SetProperty(ref _cierreConDiferencia, value))
            {
                OnPropertyChanged(nameof(CierreExacto));
            }
        }
    }

    public bool CierreExacto => !CierreConDiferencia;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // ==========================================
    // COMANDOS
    // ==========================================

    public IAsyncRelayCommand ActualizarCommand { get; }
    public IAsyncRelayCommand CerrarCajaCommand { get; }
    public IRelayCommand NuevaAperturaCommand { get; }
    public IRelayCommand IrInicioCommand { get; }
    public IRelayCommand UsarEsperadoCommand { get; }

    public CierreViewModel(
        ICajaService cajaService,
        ITransaccionService transaccionService,
        ITurnoStateService turnoStateService,
        INavegacionCaja navegacion,
        IAuditService auditService)
    {
        _cajaService = cajaService;
        _transaccionService = transaccionService;
        _turnoStateService = turnoStateService;
        _navegacion = navegacion;
        _auditService = auditService;

        ActualizarCommand = new AsyncRelayCommand(CargarAsync);
        CerrarCajaCommand = new AsyncRelayCommand(CerrarCajaAsync);
        NuevaAperturaCommand = new RelayCommand(() => _navegacion.Ir(PaginaCaja.Apertura));
        IrInicioCommand = new RelayCommand(() => _navegacion.Ir(PaginaCaja.Inicio));
        UsarEsperadoCommand = new RelayCommand(() =>
            EfectivoDeclaradoTexto = Formato.MontoEditable(EfectivoEsperado));
    }

    // ==========================================
    // CARGA
    // ==========================================

    public async Task CargarAsync()
    {
        try
        {
            IsLoading = true;
            Mensaje = string.Empty;
            OnPropertyChanged(nameof(HoraActual));

            _turnoActual = await _cajaService.ObtenerTurnoAbiertoAsync();

            if (_turnoActual is null)
            {
                if (!TurnoCerrado)
                {
                    LimpiarTurno();
                }

                return;
            }

            TurnoCerrado = false;
            HayTurnoAbierto = true;
            NumeroTurno = _turnoActual.NumeroTurno;
            Cajero = _turnoActual.NombreCajero;
            Caja = _turnoActual.Caja;
            HoraApertura = Formato.Hora(_turnoActual.FechaApertura);

            _balanceInicial = _turnoActual.BalanceInicial;
            _totalCobros = await _cajaService.ObtenerTotalCobrosTurnoAsync(_turnoActual.Id);
            _totalEfectivo = await _cajaService.ObtenerTotalEfectivoTurnoAsync(_turnoActual.Id);
            _totalTarjeta = await _cajaService.ObtenerTotalTarjetaTurnoAsync(_turnoActual.Id);

            var operaciones = await _transaccionService.ObtenerEntreAsync(_turnoActual.FechaApertura, null);
            CantidadOperaciones = operaciones.Count;
            CantidadSubsidiadas = operaciones.Count(t => t.EsSubsidiado);

            OnPropertyChanged(nameof(BalanceInicialTexto));
            OnPropertyChanged(nameof(TotalCobrosTexto));
            OnPropertyChanged(nameof(TotalEfectivoTexto));
            OnPropertyChanged(nameof(TotalTarjetaTexto));
            OnPropertyChanged(nameof(EfectivoEsperadoTexto));
            NotificarCuadre();
        }
        catch (Exception ex)
        {
            Mensaje = $"No se pudo cargar el cuadre: {ex.Message}";
            _auditService.Error(EventosAuditoria.CierreCaja, "Error al cargar el cuadre.", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ==========================================
    // CERRAR CAJA
    // ==========================================

    private async Task CerrarCajaAsync()
    {
        if (!HayTurnoAbierto || _turnoActual is null)
        {
            Mensaje = "No existe una caja abierta.";
            return;
        }

        if (!Formato.TryParseMonto(EfectivoDeclaradoTexto, out decimal declarado))
        {
            Mensaje = "Escribe el efectivo contado físicamente antes de cerrar el turno.";
            return;
        }

        if (declarado < 0)
        {
            Mensaje = "El efectivo contado no puede ser negativo.";
            return;
        }

        decimal diferencia = declarado - EfectivoEsperado;

        if (diferencia != 0 && Observacion.Trim().Length < 10)
        {
            Mensaje = "Hay una diferencia en el cuadre: explica la causa en la observación (mínimo 10 caracteres).";
            return;
        }

        try
        {
            IsLoading = true;
            Mensaje = string.Empty;

            var turnoCerrado = await _cajaService.CerrarCajaAsync(declarado, Observacion.Trim());

            decimal diferenciaFinal = turnoCerrado.Diferencia ?? 0m;
            CierreConDiferencia = diferenciaFinal != 0;

            ResultadoCierre = diferenciaFinal == 0
                ? $"Turno {turnoCerrado.NumeroTurno} cerrado con cuadre exacto."
                : diferenciaFinal > 0
                    ? $"Turno {turnoCerrado.NumeroTurno} cerrado con un sobrante de {Formato.Moneda(diferenciaFinal)}."
                    : $"Turno {turnoCerrado.NumeroTurno} cerrado con un faltante de {Formato.Moneda(Math.Abs(diferenciaFinal))}.";

            _auditService.Info(
                EventosAuditoria.CierreCaja,
                $"Turno {turnoCerrado.NumeroTurno} cerrado. Esperado: {Formato.Moneda(EfectivoEsperado)}. " +
                $"Declarado: {Formato.Moneda(declarado)}. Diferencia: {Formato.Moneda(diferenciaFinal)}.");

            TurnoCerrado = true;
            HayTurnoAbierto = false;
            _turnoActual = null;
            EfectivoDeclaradoTexto = string.Empty;
            Observacion = string.Empty;

            await _turnoStateService.ActualizarAsync();
        }
        catch (Exception ex)
        {
            Mensaje = $"No se pudo cerrar la caja: {ex.Message}";
            _auditService.Error(EventosAuditoria.CierreCaja, "Error al cerrar la caja.", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NotificarCuadre()
    {
        OnPropertyChanged(nameof(HaDeclarado));
        OnPropertyChanged(nameof(SinDeclarar));
        OnPropertyChanged(nameof(Diferencia));
        OnPropertyChanged(nameof(EsCuadreExacto));
        OnPropertyChanged(nameof(EsSobrante));
        OnPropertyChanged(nameof(EsFaltante));
        OnPropertyChanged(nameof(ContadoTexto));
        OnPropertyChanged(nameof(DiferenciaTexto));
        OnPropertyChanged(nameof(MensajeCuadre));
    }

    private void LimpiarTurno()
    {
        _turnoActual = null;
        HayTurnoAbierto = false;
        NumeroTurno = "—";
        Cajero = "—";
        Caja = "—";
        HoraApertura = "—";
        _balanceInicial = 0;
        _totalCobros = 0;
        _totalEfectivo = 0;
        _totalTarjeta = 0;
        CantidadOperaciones = 0;
        CantidadSubsidiadas = 0;
        EfectivoDeclaradoTexto = string.Empty;
        Observacion = string.Empty;
        NotificarCuadre();
    }
}
