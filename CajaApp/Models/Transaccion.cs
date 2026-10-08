using System;

namespace CajaApp.Models;

public enum FormaPago
{
    Efectivo,
    Tarjeta,
    Subsidiado
}

public enum EstadoTransaccion
{
    Pendiente,
    Completada,
    Anulada
}

public class Transaccion
{
    public int Id { get; set; }
    public string NumeroTransaccion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string CedulaPaciente { get; set; } = string.Empty;
    public string TipoServicio { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public FormaPago FormaPago { get; set; }
    public bool EsSubsidiado { get; set; }
    public EstadoTransaccion Estado { get; set; } = EstadoTransaccion.Pendiente;
    public bool ComprobanteGenerado { get; set; } = false;
    public bool Sincronizado { get; set; } = false;
    public string UsuarioCajero { get; set; } = string.Empty;
}