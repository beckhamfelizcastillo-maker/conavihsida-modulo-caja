using System;

namespace CajaApp.DTOs.Sync;

public class SyncMovimientoCajaDto
{
    public int IdLocal { get; set; }

    public int TurnoCajaIdLocal { get; set; }

    public string NumeroMovimiento { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public string Concepto { get; set; } = string.Empty;

    public string Referencia { get; set; } = string.Empty;

    public string UsuarioCajero { get; set; } = string.Empty;
}