using CajaApp.Helpers;
using CajaApp.Models;

namespace CajaApp.ViewModels.Items;

/// <summary>Fila de fondo o convenio para la tabla de Fondos.</summary>
public class FondoItem
{
    public FondoConvenio Fondo { get; }

    public FondoItem(FondoConvenio fondo)
    {
        Fondo = fondo;
    }

    public int Id => Fondo.Id;

    public string Codigo => Fondo.Codigo;

    public string Nombre => Fondo.Nombre;

    public string Organismo =>
        string.IsNullOrWhiteSpace(Fondo.Organismo) ? "—" : Fondo.Organismo;

    public string Fuente => Fondo.TipoFuente == TipoFuenteFondo.DonacionInternacional
        ? "Cooperación internacional"
        : "Presupuesto estatal";

    public bool EsInternacional => Fondo.TipoFuente == TipoFuenteFondo.DonacionInternacional;

    public string Asignado => Formato.Moneda(Fondo.MontoAsignado);

    public string Utilizado => Formato.Moneda(Fondo.MontoUtilizado);

    public string Disponible => Formato.Moneda(Fondo.SaldoDisponible);

    public string Vigencia => Fondo.FechaFin.HasValue
        ? $"{Formato.Fecha(Fondo.FechaInicio)} – {Formato.Fecha(Fondo.FechaFin.Value)}"
        : $"Desde {Formato.Fecha(Fondo.FechaInicio)}";

    public bool Activo => Fondo.Activo;

    public bool Inactivo => !Fondo.Activo;

    public string TextoAccion => Fondo.Activo ? "Desactivar" : "Activar";

    /// <summary>Porcentaje utilizado (0–100) para la barra de consumo.</summary>
    public double PorcentajeUso => Fondo.MontoAsignado <= 0
        ? 0
        : (double)(Fondo.MontoUtilizado / Fondo.MontoAsignado * 100m);
}
