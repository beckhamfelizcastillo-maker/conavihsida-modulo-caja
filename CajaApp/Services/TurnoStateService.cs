using CajaApp.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace CajaApp.Services;

public class TurnoStateService : ITurnoStateService
{
    private readonly ICajaService _cajaService;

    public bool HayTurnoAbierto { get; private set; }

    public event EventHandler? EstadoCambiado;


    public TurnoStateService(
        ICajaService cajaService)
    {
        _cajaService = cajaService;
    }


    public async Task ActualizarAsync()
    {
        var turno =
            await _cajaService
                .ObtenerTurnoAbiertoAsync();

        HayTurnoAbierto =
            turno is not null;

        EstadoCambiado?.Invoke(
            this,
            EventArgs.Empty);
    }


    public void NotificarCambio()
    {
        _ = ActualizarAsync();
    }
}