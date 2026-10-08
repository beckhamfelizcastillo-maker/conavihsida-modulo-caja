using System;

namespace CajaApp.DTOs;

public class SubsidioResponseDto
{
    public bool Encontrado { get; set; }
    public bool TieneSubsidio { get; set; }
    public decimal Porcentaje { get; set; }
    public bool Vigente { get; set; }
    public DateTime? VigenciaHasta { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}