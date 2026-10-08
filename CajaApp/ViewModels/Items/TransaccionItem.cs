using CajaApp.Helpers;
using CajaApp.Models;

namespace CajaApp.ViewModels.Items;

/// <summary>
/// Fila de transacción lista para mostrarse en tablas
/// (Inicio, Historial). La cédula siempre va enmascarada.
/// </summary>
public class TransaccionItem
{
    public Transaccion Transaccion { get; }

    public TransaccionItem(Transaccion transaccion)
    {
        Transaccion = transaccion;
    }

    public int Id => Transaccion.Id;

    public string Numero => Transaccion.NumeroTransaccion;

    public string FechaHora => Formato.FechaHora(Transaccion.Fecha);

    public string Hora => Formato.Hora(Transaccion.Fecha);

    public string Cedula => CedulaHelper.Enmascarar(Transaccion.CedulaPaciente);

    public string Servicio => Transaccion.TipoServicio;

    public string FormaPago => Transaccion.FormaPago switch
    {
        Models.FormaPago.Efectivo => "Efectivo",
        Models.FormaPago.Tarjeta => "Tarjeta",
        _ => "Subsidiado"
    };

    public string Monto => Formato.Moneda(Transaccion.Monto);

    public bool EsSubsidiado => Transaccion.EsSubsidiado;

    public bool EsCobrado => !Transaccion.EsSubsidiado;

    public bool Sincronizado => Transaccion.Sincronizado;

    public bool Pendiente => !Transaccion.Sincronizado;

    public bool TieneComprobante => Transaccion.ComprobanteGenerado;

    public bool EstaAnulada => Transaccion.Estado == EstadoTransaccion.Anulada;

    /// <summary>Texto usado por el buscador del historial.</summary>
    public string TextoBusqueda =>
        $"{Numero} {Transaccion.CedulaPaciente} {CedulaHelper.SoloDigitos(Transaccion.CedulaPaciente)} {Servicio} {FormaPago}"
            .ToLowerInvariant();
}
