using System;

namespace CajaApp.DTOs.Sync;

public class SyncTransaccionDto
{
    public int IdLocal { get; set; }

    public string NumeroTransaccion { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string CedulaPaciente { get; set; } = string.Empty;

    public string TipoServicio { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public string FormaPago { get; set; } = string.Empty;

    public bool EsSubsidiado { get; set; }

    public string Estado { get; set; } = string.Empty;

    public bool ComprobanteGenerado { get; set; }

    public string UsuarioCajero { get; set; } = string.Empty;
}