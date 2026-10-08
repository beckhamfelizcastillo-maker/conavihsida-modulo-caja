using CajaApp.Helpers;
using CajaApp.Models;
using CajaApp.Services.Interfaces;
using CajaApp.ViewModels.Items;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CajaApp.ViewModels;

/// <summary>
/// Pantalla de inicio: resumen del turno, acciones rápidas
/// y últimos movimientos.
/// </summary>
public partial class InicioViewModel : ViewModelBase
{
    private readonly ICajaService _cajaService;
    private readonly ITransaccionService _transaccionService;
    private readonly ISessionService _sessionService;
    private readonly INavegacionCaja _navegacion;

    private TurnoCaja? _turnoActual;
    private bool _hayTurnoAbierto;
    private string _numeroTurno = "—";
    private string _caja = string.Empty;
    private string _horaApertura = string.Empty;
    private string _totalCobrado = Formato.Moneda(0);
    private int _cantidadCobros;
    private int _cantidadSubsidiadas;
    private string _balanceInicial = Formato.Moneda(0);
    private string _efectivoEsperado = Formato.Moneda(0);
    private string _totalTarjeta = Formato.Moneda(0);
    private int _pendientes;

    public ObservableCollection<TransaccionItem> UltimosMovimientos { get; } = new();

    // ==========================================
    // ENCABEZADO
    // ==========================================

    public string Saludo =>
        $"{Formato.Saludo(DateTime.Now)}, {Formato.PrimerNombre(_sessionService.NombreCompleto)}";

    public string FechaHoy => Formato.FechaLarga(DateTime.Now);

    // ==========================================
    // TURNO
    // ==========================================

    public bool HayTurnoAbierto
    {
        get => _hayTurnoAbierto;
        set
        {
            if (SetProperty(ref _hayTurnoAbierto, value))
            {
                OnPropertyChanged(nameof(CajaCerrada));
                OnPropertyChanged(nameof(EstadoCaja));
                OnPropertyChanged(nameof(DetalleEstado));
            }
        }
    }

    public bool CajaCerrada => !HayTurnoAbierto;

    public string EstadoCaja => HayTurnoAbierto ? "Abierta" : "Cerrada";

    public string DetalleEstado => HayTurnoAbierto
        ? $"{Caja} · desde {HoraApertura}"
        : "Abre tu turno para iniciar";

    public string NumeroTurno
    {
        get => _numeroTurno;
        set => SetProperty(ref _numeroTurno, value);
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

    // ==========================================
    // INDICADORES
    // ==========================================

    public string TotalCobrado
    {
        get => _totalCobrado;
        set => SetProperty(ref _totalCobrado, value);
    }

    public int CantidadCobros
    {
        get => _cantidadCobros;
        set
        {
            if (SetProperty(ref _cantidadCobros, value))
            {
                OnPropertyChanged(nameof(TextoCobros));
            }
        }
    }

    public string TextoCobros => CantidadCobros == 1
        ? "1 operación pagada"
        : $"{CantidadCobros} operaciones pagadas";

    public int CantidadSubsidiadas
    {
        get => _cantidadSubsidiadas;
        set => SetProperty(ref _cantidadSubsidiadas, value);
    }

    public string BalanceInicial
    {
        get => _balanceInicial;
        set => SetProperty(ref _balanceInicial, value);
    }

    public string EfectivoEsperado
    {
        get => _efectivoEsperado;
        set => SetProperty(ref _efectivoEsperado, value);
    }

    public string TotalTarjeta
    {
        get => _totalTarjeta;
        set => SetProperty(ref _totalTarjeta, value);
    }

    public int Pendientes
    {
        get => _pendientes;
        set
        {
            if (SetProperty(ref _pendientes, value))
            {
                OnPropertyChanged(nameof(TextoPendientes));
                OnPropertyChanged(nameof(HayPendientes));
            }
        }
    }

    public bool HayPendientes => Pendientes > 0;

    public string TextoPendientes => Pendientes == 1
        ? "1 registro pendiente de sincronizar"
        : $"{Pendientes} registros pendientes de sincronizar";

    public bool SinMovimientos => UltimosMovimientos.Count == 0;

    public string TituloMovimientos => HayTurnoAbierto
        ? "Últimos movimientos del turno"
        : "Últimos movimientos registrados";

    // ==========================================
    // ACCIONES RÁPIDAS
    // ==========================================

    public IRelayCommand IrAperturaCommand { get; }
    public IRelayCommand IrTransaccionCommand { get; }
    public IRelayCommand IrSubsidioCommand { get; }
    public IRelayCommand IrCierreCommand { get; }
    public IRelayCommand IrHistorialCommand { get; }

    public InicioViewModel(
        ICajaService cajaService,
        ITransaccionService transaccionService,
        ISessionService sessionService,
        INavegacionCaja navegacion)
    {
        _cajaService = cajaService;
        _transaccionService = transaccionService;
        _sessionService = sessionService;
        _navegacion = navegacion;

        IrAperturaCommand = new RelayCommand(() => _navegacion.Ir(PaginaCaja.Apertura));
        IrTransaccionCommand = new RelayCommand(() => _navegacion.Ir(PaginaCaja.Transaccion));
        IrSubsidioCommand = new RelayCommand(() => _navegacion.Ir(PaginaCaja.Subsidio));
        IrCierreCommand = new RelayCommand(() => _navegacion.Ir(PaginaCaja.Cierre));
        IrHistorialCommand = new RelayCommand(() => _navegacion.Ir(PaginaCaja.Historial));
    }

    // ==========================================
    // CARGA
    // ==========================================

    public async Task CargarAsync()
    {
        OnPropertyChanged(nameof(Saludo));
        OnPropertyChanged(nameof(FechaHoy));

        try
        {
            _turnoActual = await _cajaService.ObtenerTurnoAbiertoAsync();
            HayTurnoAbierto = _turnoActual is not null;

            if (_turnoActual is null)
            {
                NumeroTurno = "—";
                Caja = string.Empty;
                HoraApertura = string.Empty;
                TotalCobrado = Formato.Moneda(0);
                CantidadCobros = 0;
                CantidadSubsidiadas = 0;
                BalanceInicial = Formato.Moneda(0);
                EfectivoEsperado = Formato.Moneda(0);
                TotalTarjeta = Formato.Moneda(0);

                var recientes = await _transaccionService.ObtenerHistorialAsync();
                MostrarMovimientos(recientes.Take(5));
                return;
            }

            NumeroTurno = _turnoActual.NumeroTurno;
            Caja = _turnoActual.Caja.Split(" — ")[0];
            HoraApertura = Formato.Hora(_turnoActual.FechaApertura);
            OnPropertyChanged(nameof(DetalleEstado));

            var delTurno = await _transaccionService.ObtenerEntreAsync(
                _turnoActual.FechaApertura,
                null);

            var cobradas = delTurno
                .Where(t => !t.EsSubsidiado && t.Estado != EstadoTransaccion.Anulada)
                .ToList();

            TotalCobrado = Formato.Moneda(cobradas.Sum(t => t.Monto));
            CantidadCobros = cobradas.Count;
            CantidadSubsidiadas = delTurno.Count(t => t.EsSubsidiado);
            TotalTarjeta = Formato.Moneda(cobradas
                .Where(t => t.FormaPago == FormaPago.Tarjeta)
                .Sum(t => t.Monto));

            BalanceInicial = Formato.Moneda(_turnoActual.BalanceInicial);
            EfectivoEsperado = Formato.Moneda(_turnoActual.SaldoEsperado);

            MostrarMovimientos(delTurno.Take(5));
        }
        catch
        {
            // Si la base local no responde se muestran los valores en cero;
            // el error queda en la bitácora desde el Shell.
        }
    }

    private void MostrarMovimientos(System.Collections.Generic.IEnumerable<Transaccion> transacciones)
    {
        UltimosMovimientos.Clear();

        foreach (var transaccion in transacciones)
        {
            UltimosMovimientos.Add(new TransaccionItem(transaccion));
        }

        OnPropertyChanged(nameof(SinMovimientos));
        OnPropertyChanged(nameof(TituloMovimientos));
    }
}
