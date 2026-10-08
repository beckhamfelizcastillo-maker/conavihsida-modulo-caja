namespace CajaApp.Models;

public class EstadoSincronizacion
{
    public bool EnLinea { get; set; }

    public int TurnosPendientes { get; set; }

    public int MovimientosPendientes { get; set; }

    public int TransaccionesPendientes { get; set; }

    public int ComprobantesPendientes { get; set; }

    public int FondosPendientes { get; set; }

    public int TotalPendientes =>
        TurnosPendientes +
        MovimientosPendientes +
        TransaccionesPendientes +
        ComprobantesPendientes +
        FondosPendientes;
}