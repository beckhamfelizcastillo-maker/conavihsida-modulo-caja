using System;

namespace CajaApp.DTOs.Sync;

public class SyncComprobanteDto
{
    public int IdLocal { get; set; }

    public int TransaccionIdLocal { get; set; }

    public string NumeroComprobante { get; set; } = string.Empty;

    public DateTime FechaGeneracion { get; set; }

    public string TipoDocumento { get; set; } = string.Empty;

    public bool EsReimpresion { get; set; }

    public int CantidadReimpresiones { get; set; }
}