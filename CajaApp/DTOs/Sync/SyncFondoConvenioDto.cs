using System;

namespace CajaApp.DTOs.Sync;

public class SyncFondoConvenioDto
{
    public int IdLocal { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string TipoFuente { get; set; } = string.Empty;

    public string Organismo { get; set; } = string.Empty;

    public decimal MontoAsignado { get; set; }

    public decimal MontoUtilizado { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Activo { get; set; }
}