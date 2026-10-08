using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CajaApp.Data;
using CajaApp.Services;
using CajaApp.Services.Interfaces;
using CajaApp.ViewModels;
using CajaApp.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace CajaApp;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // ==========================================
        // QUESTPDF
        // ==========================================

        QuestPDF.Settings.License =
            LicenseType.Community;


        // ==========================================
        // BASE DE DATOS SQLITE
        // ==========================================

        using (var db = new AppDbContext())
        {
            db.Database.EnsureCreated();
        }


        // ==========================================
        // CONFIGURACIÓN
        // ==========================================

        var configuration =
            new ConfigurationBuilder()
                .SetBasePath(
                    AppContext.BaseDirectory)
                .AddJsonFile(
                    "appsettings.json",
                    optional: true)
                .Build();


        // ==========================================
        // INYECCIÓN DE DEPENDENCIAS
        // ==========================================

        var services =
            new ServiceCollection();


        services.AddSingleton<IConfiguration>(
            configuration);


        // ==========================================
        // AUTENTICACIÓN
        // ==========================================

        services.AddHttpClient<
            IAuthService,
            AuthService>();


        // ==========================================
        // API TEMPORAL DE SUBSIDIOS
        // ==========================================

        services.AddSingleton<
            IApiService,
            MockApiService>();


        // ==========================================
        // SESIÓN
        // ==========================================

        services.AddSingleton<
            ISessionService,
            SessionService>();


        // ==========================================
        // AUDITORÍA (Logs/caja-AAAAMMDD.log)
        // ==========================================

        services.AddSingleton<
            IAuditService,
            AuditService>();


        // ==========================================
        // NAVEGACIÓN ENTRE PANTALLAS DE CAJA
        // ==========================================

        services.AddSingleton<
            INavegacionCaja,
            NavegacionCaja>();


        // ==========================================
        // NAVEGACIÓN
        // ==========================================

        services.AddSingleton<
            INavigationService,
            NavigationService>();


        // ==========================================
        // CAJA
        // ==========================================

        services.AddSingleton<
            ICajaService,
            CajaService>();


        // ==========================================
        // ESTADO GLOBAL DEL TURNO
        // ==========================================

        services.AddSingleton<
            ITurnoStateService,
            TurnoStateService>();


        // ==========================================
        // TRANSACCIONES
        // ==========================================

        services.AddSingleton<
            ITransaccionService,
            TransaccionService>();


        // ==========================================
        // COMPROBANTES
        // ==========================================

        services.AddSingleton<
            IComprobanteService,
            ComprobanteService>();


        // ==========================================
        // FONDOS Y CONVENIOS
        // ==========================================

        services.AddSingleton<
            IFondoService,
            FondoService>();


        // ==========================================
        // SINCRONIZACIÓN HTTP CON INTEGRATION
        // ==========================================

        services.AddHttpClient<
            ISyncApiService,
            SyncApiService>(
                client =>
                {
                    client.Timeout =
                        TimeSpan.FromSeconds(3);
                });


        // ==========================================
        // MOTOR DE SINCRONIZACIÓN
        // ==========================================

        services.AddSingleton<
            ISyncService,
            SyncService>();


        // ==========================================
        // BACKGROUND SYNC SERVICE
        // ==========================================

        services.AddSingleton<
            BackgroundSyncService>();


        // Ambas resoluciones apuntan a la MISMA
        // instancia singleton.
        services.AddSingleton<
            IBackgroundSyncService>(
                provider =>
                    provider.GetRequiredService<
                        BackgroundSyncService>());


        // ==========================================
        // VIEWMODELS
        // ==========================================

        services.AddTransient<
            LoginViewModel>();


        services.AddTransient<
            ShellViewModel>();


        services.AddTransient<
            InicioViewModel>();


        services.AddTransient<
            AperturaViewModel>();


        services.AddTransient<
            TransaccionViewModel>();


        services.AddTransient<
            SubsidioViewModel>();


        services.AddTransient<
            FondosViewModel>();


        services.AddTransient<
            HistorialViewModel>();


        services.AddTransient<
            CierreViewModel>();


        services.AddTransient<
            AyudaViewModel>();


        services.AddTransient<
            MainViewModel>();


        // ==========================================
        // CONSTRUIR CONTENEDOR
        // ==========================================

        Services =
            services.BuildServiceProvider();


        // ==========================================
        // MAIN VIEWMODEL
        // ==========================================

        // ==========================================
        // BITÁCORA Y ERRORES NO CONTROLADOS
        // ==========================================

        var audit =
            Services.GetRequiredService<
                IAuditService>();

        audit.Info(
            EventosAuditoria.Sistema,
            "Inicio del módulo Caja de CONAVIHSIDA.");

        RegistrarManejoDeErrores(audit);


        var mainViewModel =
            Services.GetRequiredService<
                MainViewModel>();


        // ==========================================
        // CONFIGURAR NAVEGACIÓN
        // ==========================================

        var navigationService =
            Services.GetRequiredService<
                INavigationService>();


        navigationService
            .ConfigurarMainViewModel(
                mainViewModel);


        // ==========================================
        // INICIAR SINCRONIZACIÓN AUTOMÁTICA
        // ==========================================

        var backgroundSyncService =
            Services.GetRequiredService<
                IBackgroundSyncService>();


        _ = backgroundSyncService
            .StartAsync(
                CancellationToken.None);


        // ==========================================
        // CREAR VENTANA PRINCIPAL
        // ==========================================

        if (ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow =
                new MainWindow
                {
                    DataContext =
                        mainViewModel
                };


            // ======================================
            // DETENER BACKGROUND SERVICE AL SALIR
            // ======================================

            desktop.Exit += async (_, _) =>
            {
                try
                {
                    var backgroundService =
                        Services.GetRequiredService<
                            IBackgroundSyncService>();


                    await backgroundService
                        .StopAsync(
                            CancellationToken.None);

                    audit.Info(
                        EventosAuditoria.Sistema,
                        "Cierre del módulo Caja.");
                }
                catch
                {
                    // El cierre de Caja no debe
                    // fallar por el servicio de sync.
                }
            };
        }


        base.OnFrameworkInitializationCompleted();
    }

    // ==========================================
    // ERRORES NO CONTROLADOS
    // ==========================================

    /// <summary>
    /// Registra en la bitácora cualquier error no controlado.
    /// Los errores de la interfaz se marcan como manejados para
    /// que la caja no se cierre en medio de una operación.
    /// </summary>
    private static void RegistrarManejoDeErrores(IAuditService audit)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            audit.Error(
                EventosAuditoria.Error,
                "Error no controlado de la aplicación.",
                e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            audit.Error(
                EventosAuditoria.Error,
                "Error no observado en una tarea en segundo plano.",
                e.Exception);

            e.SetObserved();
        };

        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            audit.Error(
                EventosAuditoria.Error,
                "Error no controlado en la interfaz.",
                e.Exception);

            e.Handled = true;
        };
    }
}
