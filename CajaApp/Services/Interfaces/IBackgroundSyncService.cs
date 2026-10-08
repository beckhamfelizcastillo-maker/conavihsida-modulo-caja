using CajaApp.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CajaApp.Services.Interfaces;

public interface IBackgroundSyncService
{
    event EventHandler<EstadoSincronizacion>?
        EstadoActualizado;

    Task StartAsync(
        CancellationToken cancellationToken);

    Task StopAsync(
        CancellationToken cancellationToken);
}