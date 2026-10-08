using System;

namespace CajaApp.Models;

public enum EstadoTurnoCaja
{
    Abierto = 1,
    Cerrado = 2
}

public class TurnoCaja
{
    public int Id { get; set; }

    public string NumeroTurno { get; set; } = string.Empty;

    public string UsuarioCajero { get; set; } = string.Empty;

    public string NombreCajero { get; set; } = string.Empty;

    public string Caja { get; set; } = string.Empty;

    public DateTime FechaApertura { get; set; }

    public DateTime? FechaCierre { get; set; }

    public decimal BalanceInicial { get; set; }

    public decimal SaldoEsperado { get; set; }

    public decimal? EfectivoDeclarado { get; set; }

    public decimal? Diferencia { get; set; }

    public EstadoTurnoCaja Estado { get; set; }

    public string Observacion { get; set; } = string.Empty;

    public bool Sincronizado { get; set; }
}