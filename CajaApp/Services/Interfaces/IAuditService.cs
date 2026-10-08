using System;

namespace CajaApp.Services.Interfaces;

/// <summary>
/// Bitácora de auditoría del módulo Caja.
/// Cada registro incluye fecha y hora, usuario, terminal y evento.
/// Nunca debe recibir información clínica (RN-01 / RN-03).
/// </summary>
public interface IAuditService
{
    void Info(string evento, string mensaje);

    void Advertencia(string evento, string mensaje);

    void Error(string evento, string mensaje, Exception? excepcion = null);

    /// <summary>Carpeta donde se guardan los archivos diarios de bitácora.</summary>
    string CarpetaLogs { get; }
}

/// <summary>Nombres de evento usados en la bitácora.</summary>
public static class EventosAuditoria
{
    public const string Sistema = "SISTEMA";
    public const string Login = "LOGIN";
    public const string Logout = "LOGOUT";
    public const string AperturaCaja = "APERTURA_CAJA";
    public const string Transaccion = "TRANSACCION";
    public const string Subsidio = "SUBSIDIO";
    public const string Comprobante = "COMPROBANTE";
    public const string Reimpresion = "REIMPRESION";
    public const string CierreCaja = "CIERRE_CAJA";
    public const string Fondo = "FONDO";
    public const string Sync = "SYNC";
    public const string Error = "ERROR";
}
