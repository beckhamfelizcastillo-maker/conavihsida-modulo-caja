using System;

namespace CajaApp.Models;

public class Paciente
{
    public int Id { get; set; }
    public string Cedula { get; set; } = string.Empty;
    public string NombreMinimo { get; set; } = string.Empty; // solo lo necesario, sin datos clínicos (RN-03)
    public bool TieneSubsidio { get; set; }
    public decimal PorcentajeSubsidio { get; set; }
    public DateTime? VigenciaHasta { get; set; }
}