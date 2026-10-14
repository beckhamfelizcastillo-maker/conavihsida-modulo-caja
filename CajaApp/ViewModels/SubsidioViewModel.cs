using CajaApp.Helpers;
using CajaApp.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace CajaApp.ViewModels;

/// <summary>
/// Consulta de elegibilidad financiera (RF-CAJ-06).
/// Solo muestra el estado del subsidio y el dato mínimo del titular;
/// nunca información clínica (RN-01 / RN-03).
/// </summary>
public partial class SubsidioViewModel : ViewModelBase
{
    private readonly IApiService _apiService;
    private readonly INavegacionCaja _navegacion;
    private readonly IAuditService _auditService;

    private string _cedula = string.Empty;
    private string _cedulaConsultada = string.Empty;
    private string _mensaje = string.Empty;
    private bool _mostrarResultado;
    private bool _encontrado;
    private bool _tieneSubsidio;
    private bool _vencido;
    private string _titular = string.Empty;
    private string _porcentaje = "—";
    private string _vigencia = "—";
    private string _cobertura = "—";
    private bool _isLoading;
    private bool _puedeContinuar;

    public string Cedula
    {
        get => _cedula;
        set => SetProperty(ref _cedula, value);
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
    // RESULTADO
    // ==========================================

    public bool MostrarResultado
    {
        get => _mostrarResultado;
        set => SetProperty(ref _mostrarResultado, value);
    }

    public bool Encontrado
    {
        get => _encontrado;
        set
        {
            if (SetProperty(ref _encontrado, value))
            {
                OnPropertyChanged(nameof(NoEncontrado));
            }
        }
    }

    public bool NoEncontrado => !Encontrado;

    public bool TieneSubsidio
    {
        get => _tieneSubsidio;
        set
        {
            if (SetProperty(ref _tieneSubsidio, value))
            {
                OnPropertyChanged(nameof(SinSubsidio));
                OnPropertyChanged(nameof(MostrarContinuar));
                OnPropertyChanged(nameof(MostrarAvisoCajaCerrada));
            }
        }
    }

    public bool SinSubsidio => Encontrado && !TieneSubsidio;

    /// <summary>Registrado pero sin subsidio (no es un caso de vencimiento).</summary>
    public bool SinCobertura => Encontrado && !TieneSubsidio && !Vencido;

    public bool Vencido
    {
        get => _vencido;
        set => SetProperty(ref _vencido, value);
    }

    public string Titular
    {
        get => _titular;
        set => SetProperty(ref _titular, value);
    }

    public string TerminacionCedula => $"Cédula terminada en {CedulaHelper.Terminacion(_cedulaConsultada)}";

    public string CedulaEnmascarada => CedulaHelper.Enmascarar(_cedulaConsultada);

    public string Iniciales => Encontrado ? Formato.Iniciales(Titular) : "?";

    public string EstadoTexto => !Encontrado
        ? "No registrado"
        : TieneSubsidio ? "Activo" : Vencido ? "Vencido" : "Sin subsidio";

    public string Porcentaje
    {
        get => _porcentaje;
        set => SetProperty(ref _porcentaje, value);
    }

    public string Vigencia
    {
        get => _vigencia;
        set => SetProperty(ref _vigencia, value);
    }

    public string Cobertura
    {
        get => _cobertura;
        set => SetProperty(ref _cobertura, value);
    }

    /// <summary>Lo fija el Shell: solo se puede continuar con caja abierta.</summary>
    public bool PuedeContinuar
    {
        get => _puedeContinuar;
        set
        {
            if (SetProperty(ref _puedeContinuar, value))
            {
                OnPropertyChanged(nameof(MostrarContinuar));
                OnPropertyChanged(nameof(MostrarAvisoCajaCerrada));
            }
        }
    }

    public bool MostrarContinuar => TieneSubsidio && PuedeContinuar;

    public bool MostrarAvisoCajaCerrada => TieneSubsidio && !PuedeContinuar;

    // ==========================================
    // COMANDOS
    // ==========================================

    public IAsyncRelayCommand ValidarCommand { get; }
    public IRelayCommand ContinuarCommand { get; }
    public IRelayCommand NuevaConsultaCommand { get; }

    public SubsidioViewModel(
        IApiService apiService,
        INavegacionCaja navegacion,
        IAuditService auditService)
    {
        _apiService = apiService;
        _navegacion = navegacion;
        _auditService = auditService;

        ValidarCommand = new AsyncRelayCommand(ValidarAsync);
        ContinuarCommand = new RelayCommand(Continuar);
        NuevaConsultaCommand = new RelayCommand(NuevaConsulta);
    }

    private async Task ValidarAsync()
    {
        if (!CedulaHelper.EsValida(Cedula))
        {
            Mensaje = "Escribe una cédula de 11 dígitos (formato 000-0000000-0).";
            MostrarResultado = false;
            return;
        }

        try
        {
            IsLoading = true;
            Mensaje = string.Empty;
            Cedula = CedulaHelper.Normalizar(Cedula);

            var resultado = await _apiService.ValidarSubsidioAsync(Cedula);

            _cedulaConsultada = Cedula;
            Encontrado = resultado.Encontrado;
            TieneSubsidio = resultado.Encontrado && resultado.TieneSubsidio && resultado.Vigente;
            Vencido = resultado.Encontrado && resultado.TieneSubsidio && !resultado.Vigente;
            Titular = resultado.Encontrado ? "Titular verificado" : "Titular no encontrado";

            Porcentaje = resultado.Encontrado ? $"{resultado.Porcentaje:N0}%" : "—";
            Vigencia = !resultado.Encontrado
                ? "—"
                : resultado.VigenciaHasta.HasValue
                    ? Formato.Fecha(resultado.VigenciaHasta.Value)
                    : "Sin fecha límite";
            Cobertura = TieneSubsidio
                ? "Medicamentos ARV gratuitos (RD$ 0.00). Los demás servicios se cobran."
                : Vencido
                    ? "La cobertura expiró; se cobra el servicio"
                    : "Sin cobertura; se cobra el servicio";

            OnPropertyChanged(nameof(TerminacionCedula));
            OnPropertyChanged(nameof(CedulaEnmascarada));
            OnPropertyChanged(nameof(Iniciales));
            OnPropertyChanged(nameof(EstadoTexto));
            OnPropertyChanged(nameof(SinCobertura));

            MostrarResultado = true;

            _auditService.Info(
                EventosAuditoria.Subsidio,
                $"Consulta de elegibilidad para {CedulaHelper.Enmascarar(Cedula)}: {EstadoTexto}.");
        }
        catch (Exception ex)
        {
            MostrarResultado = false;
            Mensaje = $"No se pudo consultar la cobertura: {ex.Message}";
            _auditService.Error(EventosAuditoria.Subsidio, "Error en consulta de elegibilidad.", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Continuar()
    {
        if (!TieneSubsidio || string.IsNullOrWhiteSpace(_cedulaConsultada))
        {
            return;
        }

        _navegacion.Ir(PaginaCaja.Transaccion, _cedulaConsultada);
    }

    private void NuevaConsulta()
    {
        Cedula = string.Empty;
        _cedulaConsultada = string.Empty;
        Mensaje = string.Empty;
        MostrarResultado = false;
        Encontrado = false;
        TieneSubsidio = false;
        Vencido = false;
    }
}
