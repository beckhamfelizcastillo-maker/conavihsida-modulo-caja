using System;

namespace CajaApp.Models;

public enum TipoFuenteFondo
{
    PresupuestoEstatal = 1,
    DonacionInternacional = 2
}

public class FondoConvenio
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public TipoFuenteFondo TipoFuente { get; set; }

    public string Organismo { get; set; } = string.Empty;

    public decimal MontoAsignado { get; set; }

    public decimal MontoUtilizado { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Activo { get; set; } = true;

    public bool Sincronizado { get; set; } = false;

    public decimal SaldoDisponible =>
        MontoAsignado - MontoUtilizado;
}