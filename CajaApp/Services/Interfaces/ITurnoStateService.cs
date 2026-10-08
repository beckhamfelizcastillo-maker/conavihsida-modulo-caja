using System;
using System.Threading.Tasks;

namespace CajaApp.Services.Interfaces;

public interface ITurnoStateService
{
    bool HayTurnoAbierto { get; }

    event EventHandler? EstadoCambiado;

    Task ActualizarAsync();

    void NotificarCambio();
}