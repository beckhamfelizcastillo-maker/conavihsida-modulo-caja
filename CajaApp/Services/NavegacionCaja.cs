using CajaApp.Services.Interfaces;
using System;

namespace CajaApp.Services;

public class NavegacionCaja : INavegacionCaja
{
    public event EventHandler<SolicitudNavegacion>? NavegacionSolicitada;

    public void Ir(PaginaCaja pagina, string? cedula = null)
    {
        NavegacionSolicitada?.Invoke(
            this,
            new SolicitudNavegacion(pagina, cedula));
    }
}
