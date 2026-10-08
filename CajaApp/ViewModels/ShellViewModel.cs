using Avalonia.Threading;
using CajaApp.Helpers;
using CajaApp.Models;
using CajaApp.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace CajaApp.ViewModels;

/// <summary>
/// Contenedor principal después del login: barra lateral, barra
/// superior, pie y la pantalla activa (CurrentPage).
/// </summary>
public partial class ShellViewModel : ViewModelBase
{
    // ==========================================
    // PANTALLAS
    // ==========================================

    private readonly InicioViewModel _inicioViewModel;
    private readonly AperturaViewModel _aperturaViewModel;
    private readonly TransaccionViewModel _transaccionViewModel;
    private readonly SubsidioViewModel _subsidioViewModel;
    private readonly FondosViewModel _fondosViewModel;
    private readonly HistorialViewModel _historialViewModel;
    private readonly CierreViewModel _cierreViewModel;
    private readonly AyudaViewModel _ayudaViewModel;

    // ==========================================
    // SERVICIOS
    // ==========================================

    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly ITurnoStateService _turnoStateService;
    private readonly ISyncService _syncService;
    private readonly IBackgroundSyncService _backgroundSyncService;
    private readonly INavegacionCaja _navegacionCaja;
    private readonly ICajaService _cajaService;
    private readonly IAuditService _auditService;

    // ==========================================
    // CAMPOS
    // ==========================================

    private ViewModelBase _currentPage;
    private PaginaCaja _paginaActual = PaginaCaja.Inicio;
    private bool _hayTurnoAbierto;
    private string _turnoNumero = "—";
    private string _turnoCaja = string.Empty;
    private string _turnoInicial = Formato.Moneda(0);
    private bool _enLinea;
    private int _totalPendientes;
    private string _mensajeSincronizacion = "Verificando estado de sincronización…";
    private bool _sincronizando;

    // ==========================================
    // PÁGINA ACTUAL
    // ==========================================

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public PaginaCaja PaginaActual
    {
        get => _paginaActual;
        private set
        {
            if (SetProperty(ref _paginaActual, value))
            {
                OnPropertyChanged(nameof(EsInicio));
                OnPropertyChanged(nameof(EsApertura));
                OnPropertyChanged(nameof(EsTransaccion));
                OnPropertyChanged(nameof(EsSubsidio));
                OnPropertyChanged(nameof(EsFondos));
                OnPropertyChanged(nameof(EsHistorial));
                OnPropertyChanged(nameof(EsCierre));
                OnPropertyChanged(nameof(EsAyuda));
                OnPropertyChanged(nameof(TituloPagina));
            }
        }
    }

    public bool EsInicio => PaginaActual == PaginaCaja.Inicio;
    public bool EsApertura => PaginaActual == PaginaCaja.Apertura;
    public bool EsTransaccion => PaginaActual == PaginaCaja.Transaccion;
    public bool EsSubsidio => PaginaActual == PaginaCaja.Subsidio;
    public bool EsFondos => PaginaActual == PaginaCaja.Fondos;
    public bool EsHistorial => PaginaActual == PaginaCaja.Historial;
    public bool EsCierre => PaginaActual == PaginaCaja.Cierre;
    public bool EsAyuda => PaginaActual == PaginaCaja.Ayuda;

    public string TituloPagina => PaginaActual switch
    {
        PaginaCaja.Apertura => "Apertura de caja",
        PaginaCaja.Transaccion => "Nueva transacción",
        PaginaCaja.Subsidio => "Validar subsidio",
        PaginaCaja.Fondos => "Fondos y convenios",
        PaginaCaja.Historial => "Historial",
        PaginaCaja.Cierre => "Cuadre y cierre",
        PaginaCaja.Ayuda => "Preguntas frecuentes",
        _ => "Inicio"
    };

    // ==========================================
    // ESTADO DEL TURNO
    // ==========================================

    public bool HayTurnoAbierto
    {
        get => _hayTurnoAbierto;
        set
        {
            if (SetProperty(ref _hayTurnoAbierto, value))
            {
                OnPropertyChanged(nameof(PuedeAbrirCaja));
                OnPropertyChanged(nameof(PuedeOperarCaja));
                OnPropertyChanged(nameof(EstadoTurno));
                OnPropertyChanged(nameof(CajaCerrada));
                OnPropertyChanged(nameof(TextoSesion));
            }
        }
    }

    public bool PuedeAbrirCaja => !HayTurnoAbierto;

    public bool PuedeOperarCaja => HayTurnoAbierto;

    public bool CajaCerrada => !HayTurnoAbierto;

    public string EstadoTurno => HayTurnoAbierto ? "Caja abierta" : "Caja sin abrir";

    public string TurnoNumero
    {
        get => _turnoNumero;
        set => SetProperty(ref _turnoNumero, value);
    }

    public string TurnoCaja
    {
        get => _turnoCaja;
        set
        {
            if (SetProperty(ref _turnoCaja, value))
            {
                OnPropertyChanged(nameof(TextoSesion));
            }
        }
    }

    public string TurnoInicial
    {
        get => _turnoInicial;
        set => SetProperty(ref _turnoInicial, value);
    }

    // ==========================================
    // USUARIO
    // ==========================================

    public string NombreUsuario => _sessionService.NombreCompleto;

    public string Usuario => _sessionService.Usuario;

    public string Rol => _sessionService.Rol;

    public string Iniciales => Formato.Iniciales(_sessionService.NombreCompleto);

    public string TextoSesion =>
        HayTurnoAbierto && !string.IsNullOrWhiteSpace(TurnoCaja)
            ? $"{NombreUsuario} · {TurnoCaja}"
            : NombreUsuario;

    // ==========================================
    // SINCRONIZACIÓN
    // ==========================================

    public bool EnLinea
    {
        get => _enLinea;
        set
        {
            if (SetProperty(ref _enLinea, value))
            {
                OnPropertyChanged(nameof(EstadoConexion));
                OnPropertyChanged(nameof(SinConexion));
            }
        }
    }

    public bool SinConexion => !EnLinea;

    public string EstadoConexion => EnLinea ? "En línea" : "Sin conexión";

    public int TotalPendientes
    {
        get => _totalPendientes;
        set
        {
            if (SetProperty(ref _totalPendientes, value))
            {
                OnPropertyChanged(nameof(TextoPendientes));
            }
        }
    }

    public string TextoPendientes => TotalPendientes switch
    {
        0 => "Sin pendientes",
        1 => "1 pendiente",
        _ => $"{TotalPendientes} pendientes"
    };

    public string MensajeSincronizacion
    {
        get => _mensajeSincronizacion;
        set => SetProperty(ref _mensajeSincronizacion, value);
    }

    public bool Sincronizando
    {
        get => _sincronizando;
        set => SetProperty(ref _sincronizando, value);
    }

    // ==========================================
    // COMANDOS
    // ==========================================

    public IRelayCommand IrInicioCommand { get; }
    public IAsyncRelayCommand ReiniciarCommand { get; }
    public IRelayCommand IrAperturaCommand { get; }
    public IRelayCommand IrTransaccionCommand { get; }
    public IRelayCommand IrSubsidioCommand { get; }
    public IRelayCommand IrFondosCommand { get; }
    public IRelayCommand IrHistorialCommand { get; }
    public IRelayCommand IrCierreCommand { get; }
    public IRelayCommand IrAyudaCommand { get; }
    public IRelayCommand CerrarSesionCommand { get; }
    public IAsyncRelayCommand ActualizarEstadoSyncCommand { get; }
    public IAsyncRelayCommand SincronizarAhoraCommand { get; }

    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    public ShellViewModel(
        InicioViewModel inicioViewModel,
        AperturaViewModel aperturaViewModel,
        TransaccionViewModel transaccionViewModel,
        SubsidioViewModel subsidioViewModel,
        FondosViewModel fondosViewModel,
        HistorialViewModel historialViewModel,
        CierreViewModel cierreViewModel,
        AyudaViewModel ayudaViewModel,
        ISessionService sessionService,
        INavigationService navigationService,
        ITurnoStateService turnoStateService,
        ISyncService syncService,
        IBackgroundSyncService backgroundSyncService,
        INavegacionCaja navegacionCaja,
        ICajaService cajaService,
        IAuditService auditService)
    {
        _inicioViewModel = inicioViewModel;
        _aperturaViewModel = aperturaViewModel;
        _transaccionViewModel = transaccionViewModel;
        _subsidioViewModel = subsidioViewModel;
        _fondosViewModel = fondosViewModel;
        _historialViewModel = historialViewModel;
        _cierreViewModel = cierreViewModel;
        _ayudaViewModel = ayudaViewModel;

        _sessionService = sessionService;
        _navigationService = navigationService;
        _turnoStateService = turnoStateService;
        _syncService = syncService;
        _backgroundSyncService = backgroundSyncService;
        _navegacionCaja = navegacionCaja;
        _cajaService = cajaService;
        _auditService = auditService;

        _currentPage = _inicioViewModel;

        IrInicioCommand = new RelayCommand(() => Navegar(PaginaCaja.Inicio));
        ReiniciarCommand = new AsyncRelayCommand(ReiniciarAsync);
        IrAperturaCommand = new RelayCommand(() => Navegar(PaginaCaja.Apertura));
        IrTransaccionCommand = new RelayCommand(() => Navegar(PaginaCaja.Transaccion));
        IrSubsidioCommand = new RelayCommand(() => Navegar(PaginaCaja.Subsidio));
        IrFondosCommand = new RelayCommand(() => Navegar(PaginaCaja.Fondos));
        IrHistorialCommand = new RelayCommand(() => Navegar(PaginaCaja.Historial));
        IrCierreCommand = new RelayCommand(() => Navegar(PaginaCaja.Cierre));
        IrAyudaCommand = new RelayCommand(() => Navegar(PaginaCaja.Ayuda));
        CerrarSesionCommand = new RelayCommand(CerrarSesion);
        ActualizarEstadoSyncCommand = new AsyncRelayCommand(ActualizarEstadoSyncAsync);
        SincronizarAhoraCommand = new AsyncRelayCommand(SincronizarAhoraAsync);

        _turnoStateService.EstadoCambiado += TurnoStateService_EstadoCambiado;
        _backgroundSyncService.EstadoActualizado += BackgroundSyncService_EstadoActualizado;
        _navegacionCaja.NavegacionSolicitada += NavegacionCaja_NavegacionSolicitada;

        _ = InicializarAsync();
    }

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    private async Task InicializarAsync()
    {
        await _turnoStateService.ActualizarAsync();
        await ActualizarDatosTurnoAsync();
        await _inicioViewModel.CargarAsync();
        await ActualizarEstadoSyncAsync();
    }

    /// <summary>
    /// Clic en el logotipo: vuelve a Inicio y recarga turno,
    /// indicadores y estado de sincronización.
    /// </summary>
    private async Task ReiniciarAsync()
    {
        Navegar(PaginaCaja.Inicio);
        await _turnoStateService.ActualizarAsync();
        await ActualizarDatosTurnoAsync();
        await ActualizarEstadoSyncAsync();
    }

    private async Task ActualizarDatosTurnoAsync()
    {
        try
        {
            var turno = await _cajaService.ObtenerTurnoAbiertoAsync();

            HayTurnoAbierto = turno is not null;
            TurnoNumero = turno?.NumeroTurno ?? "—";
            TurnoCaja = NombreCorto(turno?.Caja);
            TurnoInicial = Formato.Moneda(turno?.BalanceInicial ?? 0m);
        }
        catch (Exception ex)
        {
            _auditService.Error(EventosAuditoria.Error, "No se pudo consultar el turno abierto.", ex);
        }
    }

    private static string NombreCorto(string? caja)
    {
        if (string.IsNullOrWhiteSpace(caja))
        {
            return string.Empty;
        }

        int separador = caja.IndexOf(" — ", StringComparison.Ordinal);
        return separador > 0 ? caja[..separador] : caja;
    }

    // ==========================================
    // NAVEGACIÓN
    // ==========================================

    private void NavegacionCaja_NavegacionSolicitada(
        object? sender,
        SolicitudNavegacion solicitud)
    {
        Navegar(solicitud.Pagina, solicitud.Cedula);
    }

    private void Navegar(PaginaCaja pagina, string? cedula = null)
    {
        // Las pantallas que requieren caja abierta redirigen a Apertura.
        if (!HayTurnoAbierto &&
            pagina is PaginaCaja.Transaccion or PaginaCaja.Cierre)
        {
            pagina = PaginaCaja.Apertura;
        }

        switch (pagina)
        {
            case PaginaCaja.Inicio:
                _ = _inicioViewModel.CargarAsync();
                CurrentPage = _inicioViewModel;
                _ = ActualizarEstadoSyncAsync();
                break;

            case PaginaCaja.Apertura:
                _ = _aperturaViewModel.PrepararAsync();
                CurrentPage = _aperturaViewModel;
                break;

            case PaginaCaja.Transaccion:
                _ = _transaccionViewModel.PrepararAsync(cedula);
                CurrentPage = _transaccionViewModel;
                break;

            case PaginaCaja.Subsidio:
                _subsidioViewModel.PuedeContinuar = HayTurnoAbierto;
                CurrentPage = _subsidioViewModel;
                break;

            case PaginaCaja.Fondos:
                _ = _fondosViewModel.CargarAsync();
                CurrentPage = _fondosViewModel;
                break;

            case PaginaCaja.Historial:
                _ = _historialViewModel.CargarAsync();
                CurrentPage = _historialViewModel;
                _ = ActualizarEstadoSyncAsync();
                break;

            case PaginaCaja.Cierre:
                _ = _cierreViewModel.CargarAsync();
                CurrentPage = _cierreViewModel;
                break;

            case PaginaCaja.Ayuda:
                CurrentPage = _ayudaViewModel;
                break;
        }

        PaginaActual = pagina;
    }

    // ==========================================
    // CAMBIO EN EL TURNO
    // ==========================================

    private void TurnoStateService_EstadoCambiado(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            HayTurnoAbierto = _turnoStateService.HayTurnoAbierto;
            _subsidioViewModel.PuedeContinuar = HayTurnoAbierto;

            await ActualizarDatosTurnoAsync();
            await _inicioViewModel.CargarAsync();
            await ActualizarEstadoSyncAsync();
        });
    }

    // ==========================================
    // EVENTO DEL BACKGROUND SYNC
    // ==========================================

    private void BackgroundSyncService_EstadoActualizado(
        object? sender,
        EstadoSincronizacion estado)
    {
        // El evento llega desde un hilo de fondo: los cambios
        // visuales deben hacerse en el hilo principal.
        Dispatcher.UIThread.Post(() => AplicarEstado(estado));
    }

    private void AplicarEstado(EstadoSincronizacion estado)
    {
        EnLinea = estado.EnLinea;
        TotalPendientes = estado.TotalPendientes;
        _inicioViewModel.Pendientes = estado.TotalPendientes;

        if (!estado.EnLinea)
        {
            MensajeSincronizacion = estado.TotalPendientes == 0
                ? "Modo sin conexión. No hay registros pendientes."
                : $"Modo sin conexión. {estado.TotalPendientes} registro(s) se enviarán cuando Integration esté disponible.";
        }
        else if (estado.TotalPendientes == 0)
        {
            MensajeSincronizacion = "Todos los datos están sincronizados.";
        }
        else
        {
            MensajeSincronizacion = $"{estado.TotalPendientes} registro(s) continúan pendientes.";
        }
    }

    // ==========================================
    // ESTADO DE SYNC
    // ==========================================

    private async Task ActualizarEstadoSyncAsync()
    {
        try
        {
            var estado = await _syncService.ObtenerEstadoAsync();
            AplicarEstado(estado);
        }
        catch (Exception ex)
        {
            EnLinea = false;
            MensajeSincronizacion = "No se pudo consultar el estado de sincronización.";
            _auditService.Error(EventosAuditoria.Sync, "Error al consultar el estado de sincronización.", ex);
        }
    }

    private async Task SincronizarAhoraAsync()
    {
        if (Sincronizando)
        {
            return;
        }

        try
        {
            Sincronizando = true;
            MensajeSincronizacion = "Verificando conexión con Integration…";

            var estadoInicial = await _syncService.ObtenerEstadoAsync();
            AplicarEstado(estadoInicial);

            if (!estadoInicial.EnLinea)
            {
                MensajeSincronizacion =
                    $"Integration no está disponible. {estadoInicial.TotalPendientes} registro(s) permanecen guardados localmente.";
                _auditService.Advertencia(
                    EventosAuditoria.Sync,
                    $"Sincronización manual sin conexión. Pendientes: {estadoInicial.TotalPendientes}.");
                return;
            }

            if (estadoInicial.TotalPendientes == 0)
            {
                MensajeSincronizacion = "No hay registros pendientes de sincronización.";
                return;
            }

            MensajeSincronizacion = $"Sincronizando {estadoInicial.TotalPendientes} registro(s)…";

            var estadoFinal = await _syncService.SincronizarPendientesAsync();
            AplicarEstado(estadoFinal);

            MensajeSincronizacion = estadoFinal.TotalPendientes == 0
                ? "Todos los registros fueron sincronizados correctamente."
                : $"Sincronización parcial. {estadoFinal.TotalPendientes} registro(s) continúan pendientes.";

            _auditService.Info(
                EventosAuditoria.Sync,
                $"Sincronización manual. Enviados: {estadoInicial.TotalPendientes - estadoFinal.TotalPendientes}. Pendientes: {estadoFinal.TotalPendientes}.");
        }
        catch (Exception ex)
        {
            EnLinea = false;
            MensajeSincronizacion = "Error durante la sincronización. Los datos siguen guardados localmente.";
            _auditService.Error(EventosAuditoria.Sync, "Error en sincronización manual.", ex);
        }
        finally
        {
            Sincronizando = false;
        }
    }

    // ==========================================
    // CERRAR SESIÓN
    // ==========================================

    private void CerrarSesion()
    {
        _auditService.Info(EventosAuditoria.Logout, "Cierre de sesión.");

        // Los servicios de estado son únicos (singleton): se
        // desconectan los eventos para que este Shell no siga
        // reaccionando después de salir.
        _turnoStateService.EstadoCambiado -= TurnoStateService_EstadoCambiado;
        _backgroundSyncService.EstadoActualizado -= BackgroundSyncService_EstadoActualizado;
        _navegacionCaja.NavegacionSolicitada -= NavegacionCaja_NavegacionSolicitada;

        _sessionService.CerrarSesion();
        _navigationService.MostrarLogin();
    }
}
