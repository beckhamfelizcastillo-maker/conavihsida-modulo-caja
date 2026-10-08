using CajaApp.Helpers;
using CajaApp.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CajaApp.ViewModels;

/// <summary>
/// Registro de cobros y entregas subsidiadas con emisión obligatoria
/// de comprobante (RF-CAJ-03, RF-CAJ-04, RF-CAJ-06).
/// </summary>
public partial class TransaccionViewModel : ViewModelBase
{
    private readonly IApiService _apiService;
    private readonly ITransaccionService _transaccionService;
    private readonly IComprobanteService _comprobanteService;
    private readonly ISessionService _sessionService;
    private readonly ITurnoStateService _turnoStateService;
    private readonly IAuditService _auditService;

    private string _cedula = string.Empty;
    private string _tipoServicio = "Consulta médica";
    private string _montoTexto = "300";
    private string _formaPago = "Efectivo";
    private bool _esSubsidiado;
    private string _cedulaValidada = string.Empty;
    private EstadoValidacion _estadoValidacion = EstadoValidacion.SinValidar;
    private string _detalleSubsidio = "Valida la cédula para saber si aplica gratuidad.";
    private string _mensaje = string.Empty;
    private TipoMensaje _tipoMensaje = TipoMensaje.Ninguno;
    private string _ultimoComprobante = string.Empty;
    private string _rutaUltimoComprobante = string.Empty;
    private bool _isLoading;

    private enum EstadoValidacion { SinValidar, Activo, NoDisponible }

    private enum TipoMensaje { Ninguno, Exito, Error, Info }

    // ==========================================
    // LISTAS
    // ==========================================

    public IReadOnlyList<string> TiposServicio { get; } =
        new[]
        {
            "Consulta médica",
            "Laboratorio",
            "Procedimiento",
            "Medicamentos ARV",
            "Otro servicio"
        };

    public IReadOnlyList<string> FormasPago { get; } =
        new[]
        {
            "Efectivo",
            "Tarjeta"
        };

    /// <summary>Precio sugerido por servicio (el cajero puede ajustarlo).</summary>
    private static readonly Dictionary<string, decimal> PreciosReferencia = new()
    {
        ["Consulta médica"] = 300m,
        ["Laboratorio"] = 650m,
        ["Procedimiento"] = 500m
    };

    // ==========================================
    // DATOS DE LA OPERACIÓN
    // ==========================================

    public string Cedula
    {
        get => _cedula;
        set
        {
            if (SetProperty(ref _cedula, value))
            {
                // Si la cédula cambia después de validarla, la validación
                // deja de aplicar: evita cobrar RD$0 a otra persona.
                if (_estadoValidacion != EstadoValidacion.SinValidar &&
                    CedulaHelper.Normalizar(value) != _cedulaValidada)
                {
                    ReiniciarValidacion();
                }

                OnPropertyChanged(nameof(PreviewCedula));
            }
        }
    }

    public string TipoServicio
    {
        get => _tipoServicio;
        set
        {
            if (SetProperty(ref _tipoServicio, value))
            {
                if (!EsSubsidiado &&
                    value is not null &&
                    PreciosReferencia.TryGetValue(value, out var precio))
                {
                    MontoTexto = Formato.MontoEditable(precio);
                }

                OnPropertyChanged(nameof(EsArvSinSubsidio));
            }
        }
    }

    public string MontoTexto
    {
        get => _montoTexto;
        set
        {
            if (SetProperty(ref _montoTexto, value))
            {
                OnPropertyChanged(nameof(TotalTexto));
            }
        }
    }

    public string FormaPago
    {
        get => _formaPago;
        set
        {
            if (SetProperty(ref _formaPago, value))
            {
                OnPropertyChanged(nameof(PreviewPago));
            }
        }
    }

    public bool EsSubsidiado
    {
        get => _esSubsidiado;
        private set
        {
            if (SetProperty(ref _esSubsidiado, value))
            {
                OnPropertyChanged(nameof(PuedeEditarCobro));
                OnPropertyChanged(nameof(TotalTexto));
                OnPropertyChanged(nameof(EtiquetaTotal));
                OnPropertyChanged(nameof(PreviewPago));
                OnPropertyChanged(nameof(EsArvSinSubsidio));
                OnPropertyChanged(nameof(TextoBotonRegistrar));
            }
        }
    }

    /// <summary>El monto y la forma de pago no se editan en una entrega subsidiada.</summary>
    public bool PuedeEditarCobro => !EsSubsidiado;

    public bool EsArvSinSubsidio => !EsSubsidiado && TipoServicio == "Medicamentos ARV";

    // ==========================================
    // ESTADO DEL SUBSIDIO
    // ==========================================

    public bool SubsidioSinValidar => _estadoValidacion == EstadoValidacion.SinValidar;
    public bool SubsidioActivo => _estadoValidacion == EstadoValidacion.Activo;
    public bool SubsidioNoDisponible => _estadoValidacion == EstadoValidacion.NoDisponible;

    public string TextoEstadoSubsidio => _estadoValidacion switch
    {
        EstadoValidacion.Activo => "Subsidio activo",
        EstadoValidacion.NoDisponible => "Sin subsidio",
        _ => "Sin validar"
    };

    public string DetalleSubsidio
    {
        get => _detalleSubsidio;
        set => SetProperty(ref _detalleSubsidio, value);
    }

    // ==========================================
    // TOTAL Y VISTA PREVIA DEL COMPROBANTE
    // ==========================================

    public string EtiquetaTotal => EsSubsidiado ? "Total a cobrar (gratuidad)" : "Total a cobrar";

    public string TotalTexto
    {
        get
        {
            if (EsSubsidiado)
            {
                return Formato.Moneda(0);
            }

            return Formato.TryParseMonto(MontoTexto, out var monto) && monto >= 0
                ? Formato.Moneda(monto)
                : "—";
        }
    }

    public string PreviewCedula => CedulaHelper.EsValida(Cedula)
        ? CedulaHelper.Enmascarar(Cedula)
        : "—";

    public string PreviewPago => EsSubsidiado ? "Subsidiado" : FormaPago;

    public string PreviewFecha => Formato.FechaHora(DateTime.Now);

    public string PreviewCajero => _sessionService.NombreCompleto;

    public string TextoBotonRegistrar => EsSubsidiado
        ? "Registrar entrega y emitir comprobante"
        : "Registrar cobro y emitir comprobante";

    // ==========================================
    // MENSAJES Y RESULTADO
    // ==========================================

    public string Mensaje
    {
        get => _mensaje;
        private set
        {
            if (SetProperty(ref _mensaje, value))
            {
                OnPropertyChanged(nameof(HayMensaje));
            }
        }
    }

    public bool HayMensaje => !string.IsNullOrWhiteSpace(Mensaje);
    public bool MensajeExito => HayMensaje && _tipoMensaje == TipoMensaje.Exito;
    public bool MensajeError => HayMensaje && _tipoMensaje == TipoMensaje.Error;
    public bool MensajeInfo => HayMensaje && _tipoMensaje == TipoMensaje.Info;

    public string UltimoComprobante
    {
        get => _ultimoComprobante;
        private set
        {
            if (SetProperty(ref _ultimoComprobante, value))
            {
                OnPropertyChanged(nameof(HayComprobante));
            }
        }
    }

    public bool HayComprobante => !string.IsNullOrWhiteSpace(UltimoComprobante);

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // ==========================================
    // COMANDOS
    // ==========================================

    public IAsyncRelayCommand ValidarSubsidioCommand { get; }
    public IAsyncRelayCommand ProcesarTransaccionCommand { get; }
    public IRelayCommand AbrirComprobanteCommand { get; }
    public IRelayCommand LimpiarCommand { get; }

    public TransaccionViewModel(
        IApiService apiService,
        ITransaccionService transaccionService,
        IComprobanteService comprobanteService,
        ISessionService sessionService,
        ITurnoStateService turnoStateService,
        IAuditService auditService)
    {
        _apiService = apiService;
        _transaccionService = transaccionService;
        _comprobanteService = comprobanteService;
        _sessionService = sessionService;
        _turnoStateService = turnoStateService;
        _auditService = auditService;

        ValidarSubsidioCommand = new AsyncRelayCommand(ValidarSubsidioAsync);
        ProcesarTransaccionCommand = new AsyncRelayCommand(ProcesarAsync);
        AbrirComprobanteCommand = new RelayCommand(AbrirComprobante);
        LimpiarCommand = new RelayCommand(() =>
        {
            LimpiarFormulario();
            MostrarMensaje(string.Empty, TipoMensaje.Ninguno);
        });
    }

    /// <summary>
    /// Se llama al entrar a la pantalla. Si viene una cédula desde
    /// "Validar subsidio", se precarga y se valida de inmediato.
    /// </summary>
    public async Task PrepararAsync(string? cedula)
    {
        OnPropertyChanged(nameof(PreviewFecha));
        OnPropertyChanged(nameof(PreviewCajero));

        if (!string.IsNullOrWhiteSpace(cedula))
        {
            Cedula = cedula;
            await ValidarSubsidioAsync();
        }
    }

    // ==========================================
    // VALIDAR SUBSIDIO
    // ==========================================

    private async Task ValidarSubsidioAsync()
    {
        if (!CedulaHelper.EsValida(Cedula))
        {
            MostrarMensaje("Escribe una cédula de 11 dígitos (formato 000-0000000-0).", TipoMensaje.Error);
            return;
        }

        try
        {
            IsLoading = true;
            Cedula = CedulaHelper.Normalizar(Cedula);
            MostrarMensaje(string.Empty, TipoMensaje.Ninguno);

            var resultado = await _apiService.ValidarSubsidioAsync(Cedula);

            _cedulaValidada = Cedula;

            if (resultado.Encontrado && resultado.TieneSubsidio && resultado.Vigente)
            {
                _estadoValidacion = EstadoValidacion.Activo;
                EsSubsidiado = true;
                DetalleSubsidio = resultado.VigenciaHasta.HasValue
                    ? $"Cobertura {resultado.Porcentaje:N0}% vigente hasta {Formato.Fecha(resultado.VigenciaHasta.Value)}. El total se ajusta a RD$ 0.00."
                    : $"Cobertura {resultado.Porcentaje:N0}% sin fecha límite. El total se ajusta a RD$ 0.00.";
            }
            else
            {
                _estadoValidacion = EstadoValidacion.NoDisponible;
                EsSubsidiado = false;
                DetalleSubsidio = resultado.Encontrado
                    ? $"{resultado.Mensaje} La operación se cobra normalmente."
                    : "Cédula no registrada en el padrón de subsidios. La operación se cobra normalmente.";
            }

            NotificarEstadoSubsidio();

            _auditService.Info(
                EventosAuditoria.Subsidio,
                $"Consulta de subsidio para {CedulaHelper.Enmascarar(Cedula)}: {TextoEstadoSubsidio}.");
        }
        catch (Exception ex)
        {
            ReiniciarValidacion();
            MostrarMensaje($"No se pudo validar el subsidio: {ex.Message}", TipoMensaje.Error);
            _auditService.Error(EventosAuditoria.Subsidio, "Error al validar subsidio.", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ==========================================
    // REGISTRAR
    // ==========================================

    private async Task ProcesarAsync()
    {
        if (!_turnoStateService.HayTurnoAbierto)
        {
            MostrarMensaje("No hay una caja abierta. Realiza la apertura antes de registrar operaciones.", TipoMensaje.Error);
            return;
        }

        if (!CedulaHelper.EsValida(Cedula))
        {
            MostrarMensaje("Escribe una cédula de 11 dígitos (formato 000-0000000-0).", TipoMensaje.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(TipoServicio))
        {
            MostrarMensaje("Selecciona el servicio.", TipoMensaje.Error);
            return;
        }

        decimal monto = 0m;

        if (!EsSubsidiado)
        {
            if (!Formato.TryParseMonto(MontoTexto, out monto))
            {
                MostrarMensaje("Escribe el monto en números, por ejemplo 300 o 1,250.00.", TipoMensaje.Error);
                return;
            }

            if (monto <= 0)
            {
                MostrarMensaje("El monto debe ser mayor que cero para una operación cobrada.", TipoMensaje.Error);
                return;
            }

            if (monto > Formato.MontoMaximoOperacion)
            {
                MostrarMensaje($"El monto no puede superar {Formato.Moneda(Formato.MontoMaximoOperacion)} por operación.", TipoMensaje.Error);
                return;
            }
        }

        // La gratuidad solo aplica a la cédula que se validó.
        bool esSubsidiado =
            EsSubsidiado &&
            CedulaHelper.Normalizar(Cedula) == _cedulaValidada;

        try
        {
            IsLoading = true;
            MostrarMensaje(string.Empty, TipoMensaje.Ninguno);
            UltimoComprobante = string.Empty;

            var transaccion = await _transaccionService.RegistrarAsync(
                CedulaHelper.Normalizar(Cedula),
                TipoServicio,
                monto,
                FormaPago,
                esSubsidiado,
                _sessionService.Usuario);

            _auditService.Info(
                EventosAuditoria.Transaccion,
                $"Transacción {transaccion.NumeroTransaccion} registrada. Servicio: {transaccion.TipoServicio}. " +
                $"Pago: {(esSubsidiado ? "Subsidiado" : FormaPago)}. Monto: {Formato.Moneda(transaccion.Monto)}. " +
                $"Beneficiario: {CedulaHelper.Enmascarar(transaccion.CedulaPaciente)}.");

            // RN-05: toda operación termina con su comprobante.
            var comprobante = await _comprobanteService.GenerarAsync(transaccion.Id);

            _auditService.Info(
                EventosAuditoria.Comprobante,
                $"Comprobante {comprobante.NumeroComprobante} emitido para {transaccion.NumeroTransaccion}.");

            UltimoComprobante = comprobante.NumeroComprobante;
            _rutaUltimoComprobante = comprobante.RutaArchivo;

            MostrarMensaje(
                esSubsidiado
                    ? $"Entrega subsidiada registrada. Comprobante {comprobante.NumeroComprobante} emitido con balance RD$ 0.00."
                    : $"Cobro de {Formato.Moneda(transaccion.Monto)} registrado. Comprobante {comprobante.NumeroComprobante} emitido.",
                TipoMensaje.Exito);

            LimpiarFormulario();

            // Actualiza indicadores de Inicio y el saldo del turno.
            _turnoStateService.NotificarCambio();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"No se pudo completar la operación: {ex.Message}", TipoMensaje.Error);
            _auditService.Error(EventosAuditoria.Transaccion, "Error al registrar transacción.", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ==========================================
    // ABRIR COMPROBANTE PDF
    // ==========================================

    private void AbrirComprobante()
    {
        if (string.IsNullOrWhiteSpace(_rutaUltimoComprobante) ||
            !File.Exists(_rutaUltimoComprobante))
        {
            MostrarMensaje("No se encontró el archivo del comprobante.", TipoMensaje.Error);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _rutaUltimoComprobante,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MostrarMensaje($"No se pudo abrir el comprobante: {ex.Message}", TipoMensaje.Error);
        }
    }

    // ==========================================
    // AUXILIARES
    // ==========================================

    private void MostrarMensaje(string texto, TipoMensaje tipo)
    {
        _tipoMensaje = tipo;
        Mensaje = texto;
        OnPropertyChanged(nameof(MensajeExito));
        OnPropertyChanged(nameof(MensajeError));
        OnPropertyChanged(nameof(MensajeInfo));
    }

    private void ReiniciarValidacion()
    {
        _estadoValidacion = EstadoValidacion.SinValidar;
        _cedulaValidada = string.Empty;
        EsSubsidiado = false;
        DetalleSubsidio = "Valida la cédula para saber si aplica gratuidad.";
        NotificarEstadoSubsidio();
    }

    private void NotificarEstadoSubsidio()
    {
        OnPropertyChanged(nameof(SubsidioSinValidar));
        OnPropertyChanged(nameof(SubsidioActivo));
        OnPropertyChanged(nameof(SubsidioNoDisponible));
        OnPropertyChanged(nameof(TextoEstadoSubsidio));
    }

    private void LimpiarFormulario()
    {
        Cedula = string.Empty;
        ReiniciarValidacion();
        TipoServicio = "Consulta médica";
        MontoTexto = "300";
        FormaPago = "Efectivo";
        OnPropertyChanged(nameof(PreviewFecha));
    }
}
