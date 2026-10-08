using System;

namespace CajaApp.Models;

public class Comprobante
{
    public int Id { get; set; }

    public int TransaccionId { get; set; }

    public string NumeroComprobante { get; set; } = string.Empty;

    public DateTime FechaGeneracion { get; set; }

    public string RutaArchivo { get; set; } = string.Empty;

    public string TipoDocumento { get; set; } = "RECIBO";

    public bool EsReimpresion { get; set; }

    public int CantidadReimpresiones { get; set; }

    public bool Sincronizado { get; set; }

    public Transaccion? Transaccion { get; set; }
}