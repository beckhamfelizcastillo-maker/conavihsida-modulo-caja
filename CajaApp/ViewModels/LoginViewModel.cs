using CajaApp.Helpers;
using CajaApp.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace CajaApp.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    private readonly ISessionService _sessionService;

    private readonly INavigationService _navigationService;

    private readonly IAuditService _auditService;

    private string _usuario = string.Empty;

    private string _password = string.Empty;

    private string _resultadoLogin = string.Empty;

    private bool _isLoading;


    public string Usuario
    {
        get => _usuario;
        set => SetProperty(ref _usuario, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ResultadoLogin
    {
        get => _resultadoLogin;
        set
        {
            if (SetProperty(ref _resultadoLogin, value))
            {
                OnPropertyChanged(nameof(HayResultado));
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }


    public IAsyncRelayCommand LoginCommand { get; }

    /// <summary>true cuando el login no llama a Integration (usuario de demostración).</summary>
    public bool EsModoDemo => _authService.EsModoDemo;

    public bool HayResultado => !string.IsNullOrWhiteSpace(ResultadoLogin);


    public LoginViewModel(
        IAuthService authService,
        ISessionService sessionService,
        INavigationService navigationService,
        IAuditService auditService)
    {
        _auditService = auditService;

        _authService = authService;

        _sessionService = sessionService;

        _navigationService = navigationService;

        LoginCommand = new AsyncRelayCommand(LoginAsync);
    }


    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Usuario))
        {
            ResultadoLogin = "Ingrese su usuario.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ResultadoLogin = "Ingrese su contraseña.";
            return;
        }

        try
        {
            IsLoading = true;

            ResultadoLogin =
                "Validando credenciales...";

            var resultado =
                await _authService.LoginAsync(
                    Usuario.Trim(),
                    Password);

            if (!resultado.Exito)
            {
                ResultadoLogin =
                    resultado.Mensaje;

                _auditService.Advertencia(
                    EventosAuditoria.Login,
                    $"Intento de acceso rechazado para el usuario '{Usuario.Trim()}'.");

                return;
            }

            // Validar rol: el CORE puede devolver varios; Caja usa uno
            // (prioridad Administrador > Cajero > Auditor).
            var rol =
                RolesHelper.ResolverRolCaja(
                    resultado.Rol,
                    resultado.Roles);

            if (rol is null)
            {
                ResultadoLogin =
                    "Su usuario no tiene permisos para acceder al módulo de Caja.";

                var recibidos =
                    string.Join(", ", resultado.Rol) +
                    (resultado.Rol.Count > 0 && resultado.Roles.Count > 0 ? ", " : string.Empty) +
                    string.Join(", ", resultado.Roles);

                _auditService.Advertencia(
                    EventosAuditoria.Login,
                    $"Acceso denegado por rol '{recibidos}' para '{Usuario.Trim()}'.");

                return;
            }

            // Guardar sesión
            _sessionService.Usuario =
                Usuario.Trim();

            _sessionService.NombreCompleto =
                resultado.NombreCompleto;

            _sessionService.Rol =
                rol;

            _sessionService.Token =
                resultado.Token;

            ResultadoLogin =
                $"Bienvenido, {resultado.NombreCompleto}.";

            _auditService.Info(
                EventosAuditoria.Login,
                $"Inicio de sesión correcto. Rol: {rol}." +
                (resultado.SinConexion ? " Validado sin conexión (operación continua)." : string.Empty));

            // No se conserva la contraseña en memoria.
            Password = string.Empty;

            // Entrar al sistema
            _navigationService.MostrarShell();
        }
        catch (Exception ex)
        {
            ResultadoLogin =
                $"Error durante el inicio de sesión: {ex.Message}";

            _auditService.Error(EventosAuditoria.Login, "Error durante el inicio de sesión.", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
