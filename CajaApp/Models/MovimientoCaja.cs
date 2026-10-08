using System;

namespace CajaApp.Models;

public enum TipoMovimientoCaja
{
    Entrada = 1,
    Salida = 2,
    Cobro = 3,
    Subsidio = 4,
    Ajuste = 5
}

public class MovimientoCaja
{
    public int Id { get; set; }

    public int TurnoCajaId { get; set; }

    public string NumeroMovimiento { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public TipoMovimientoCaja Tipo { get; set; }

    public decimal Monto { get; set; }

    public string Concepto { get; set; } = string.Empty;

    public string Referencia { get; set; } = string.Empty;

    public string UsuarioCajero { get; set; } = string.Empty;

    public bool Sincronizado { get; set; }

    public TurnoCaja? TurnoCaja { get; set; }
}