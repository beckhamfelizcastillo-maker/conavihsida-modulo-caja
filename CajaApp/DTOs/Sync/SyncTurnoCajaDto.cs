using System;

namespace CajaApp.DTOs.Sync;

public class SyncTurnoCajaDto
{
    public int IdLocal { get; set; }

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

    public string Estado { get; set; } = string.Empty;

    public string Observacion { get; set; } = string.Empty;
}