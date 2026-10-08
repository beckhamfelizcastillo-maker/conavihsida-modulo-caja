using CajaApp.Models;
using CajaApp.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CajaApp.Services;

public class BackgroundSyncService :
    BackgroundService,
    IBackgroundSyncService
{
    private readonly ISyncService _syncService;

    private readonly IAuditService _auditService;

    private bool? _ultimoEnLinea;


    // ==========================================
    // INTERVALO
    // ==========================================

    private readonly TimeSpan _intervalo =
        TimeSpan.FromSeconds(30);


    // ==========================================
    // EVENTO
    // ==========================================

    public event EventHandler<EstadoSincronizacion>?
        EstadoActualizado;


    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    public BackgroundSyncService(
        ISyncService syncService,
        IAuditService auditService)
    {
        _auditService =
            auditService;

        _syncService =
            syncService;
    }


    // ==========================================
    // EJECUCIÓN AUTOMÁTICA
    // ==========================================

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Esperar unos segundos después de
        // arrancar la aplicación.

        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(5),
                stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }


        // ======================================
        // CICLO
        // ======================================

        while (!stoppingToken
            .IsCancellationRequested)
        {
            try
            {
                await EjecutarCicloAsync();
            }
            catch (Exception ex)
            {
                // El ciclo continúa: un fallo de red no detiene Caja.
                _auditService.Error(
                    EventosAuditoria.Sync,
                    "Error en el ciclo de sincronización automática.",
                    ex);
            }


            try
            {
                await Task.Delay(
                    _intervalo,
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }


    // ==========================================
    // CICLO DE SYNC
    // ==========================================

    private async Task EjecutarCicloAsync()
    {
        var estado =
            await _syncService
                .ObtenerEstadoAsync();

        RegistrarCambioConexion(estado);


        // ======================================
        // SIN CONEXIÓN
        // ======================================

        if (!estado.EnLinea)
        {
            EstadoActualizado?.Invoke(
                this,
                estado);

            return;
        }


        // ======================================
        // ONLINE Y SIN PENDIENTES
        // ======================================

        if (estado.TotalPendientes == 0)
        {
            EstadoActualizado?.Invoke(
                this,
                estado);

            return;
        }


        // ======================================
        // ONLINE Y CON PENDIENTES
        // ======================================

        var estadoFinal =
            await _syncService
                .SincronizarPendientesAsync();

        _auditService.Info(
            EventosAuditoria.Sync,
            $"Sincronización automática. Enviados: {estado.TotalPendientes - estadoFinal.TotalPendientes}. Pendientes: {estadoFinal.TotalPendientes}.");


        EstadoActualizado?.Invoke(
            this,
            estadoFinal);
    }

    /// <summary>Registra en bitácora solo cuando cambia el estado de conexión.</summary>
    private void RegistrarCambioConexion(EstadoSincronizacion estado)
    {
        if (_ultimoEnLinea == estado.EnLinea)
        {
            return;
        }

        _ultimoEnLinea = estado.EnLinea;

        if (estado.EnLinea)
        {
            _auditService.Info(
                EventosAuditoria.Sync,
                $"Integration disponible. Pendientes: {estado.TotalPendientes}.");
        }
        else
        {
            _auditService.Advertencia(
                EventosAuditoria.Sync,
                $"Integration no disponible; Caja opera en modo sin conexión. Pendientes: {estado.TotalPendientes}.");
        }
    }
}
