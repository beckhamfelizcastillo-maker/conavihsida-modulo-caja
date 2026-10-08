using CajaApp.Data;
using CajaApp.DTOs;
using CajaApp.Helpers;
using CajaApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace CajaApp.Services;

public class MockApiService : IApiService
{
    public async Task<SubsidioResponseDto> ValidarSubsidioAsync(string cedula)
    {
        await Task.Delay(300); // simula latencia de red real

        using var db = new AppDbContext();
        if (!CedulaHelper.EsValida(cedula))
        {
            return new SubsidioResponseDto
            {
                Encontrado = false,
                Mensaje = "La cédula debe tener 11 dígitos (formato 000-0000000-0)."
            };
        }

        string cedulaNormalizada = CedulaHelper.Normalizar(cedula);

        var paciente = await db.Pacientes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Cedula == cedulaNormalizada);

        if (paciente is null)
        {
            return new SubsidioResponseDto
            {
                Encontrado = false,
                Mensaje = "Cédula no encontrada en el sistema."
            };
        }

        bool vigente = paciente.TieneSubsidio &&
                       (paciente.VigenciaHasta is null || paciente.VigenciaHasta >= DateTime.Now);

        return new SubsidioResponseDto
        {
            Encontrado = true,
            TieneSubsidio = paciente.TieneSubsidio,
            Porcentaje = paciente.PorcentajeSubsidio,
            Vigente = vigente,
            VigenciaHasta = paciente.VigenciaHasta,
            Mensaje = vigente ? "Subsidio activo." :
                      paciente.TieneSubsidio ? "Subsidio vencido." : "Paciente sin subsidio registrado."
        };
    }
}
