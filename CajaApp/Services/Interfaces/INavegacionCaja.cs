using System;

namespace CajaApp.Services.Interfaces;

/// <summary>Pantallas internas del módulo Caja (dentro del Shell).</summary>
public enum PaginaCaja
{
    Inicio,
    Apertura,
    Transaccion,
    Subsidio,
    Fondos,
    Historial,
    Cierre,
    Ayuda
}

public sealed record SolicitudNavegacion(
    PaginaCaja Pagina,
    string? Cedula = null);

/// <summary>
/// Permite que una pantalla pida ir a otra (por ejemplo, las acciones
/// rápidas de Inicio o "Continuar a transacción" desde Validar subsidio)
/// sin conocer al ShellViewModel.
/// </summary>
public interface INavegacionCaja
{
    event EventHandler<SolicitudNavegacion>? NavegacionSolicitada;

    void Ir(PaginaCaja pagina, string? cedula = null);
}
