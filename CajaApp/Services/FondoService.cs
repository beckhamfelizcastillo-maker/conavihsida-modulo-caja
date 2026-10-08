using CajaApp.Data;
using CajaApp.Models;
using CajaApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CajaApp.Services;

public class FondoService : IFondoService
{
    public async Task<List<FondoConvenio>>
        ObtenerTodosAsync()
    {
        using var db = new AppDbContext();

        return await db.FondosConvenios
            .AsNoTracking()
            .OrderByDescending(x => x.Activo)
            .ThenBy(x => x.Nombre)
            .ToListAsync();
    }


    public async Task<List<FondoConvenio>>
        ObtenerActivosAsync()
    {
        using var db = new AppDbContext();

        return await db.FondosConvenios
            .AsNoTracking()
            .Where(x => x.Activo)
            .OrderBy(x => x.Nombre)
            .ToListAsync();
    }


    public async Task<FondoConvenio> CrearAsync(
        string codigo,
        string nombre,
        TipoFuenteFondo tipoFuente,
        string organismo,
        decimal montoAsignado,
        DateTime fechaInicio,
        DateTime? fechaFin)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException(
                "Debe ingresar el código del fondo.");
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException(
                "Debe ingresar el nombre del fondo o convenio.");
        }

        if (montoAsignado <= 0)
        {
            throw new ArgumentException(
                "El monto asignado debe ser mayor que cero.");
        }

        if (fechaFin.HasValue &&
            fechaFin.Value.Date < fechaInicio.Date)
        {
            throw new ArgumentException(
                "La fecha final no puede ser anterior a la fecha inicial.");
        }

        using var db = new AppDbContext();

        string codigoNormalizado =
            codigo.Trim().ToUpperInvariant();

        bool existe =
            await db.FondosConvenios
                .AnyAsync(x =>
                    x.Codigo == codigoNormalizado);

        if (existe)
        {
            throw new InvalidOperationException(
                "Ya existe un fondo o convenio con ese código.");
        }

        var fondo = new FondoConvenio
        {
            Codigo = codigoNormalizado,

            Nombre = nombre.Trim(),

            TipoFuente = tipoFuente,

            Organismo =
                organismo?.Trim() ?? string.Empty,

            MontoAsignado = montoAsignado,

            MontoUtilizado = 0,

            FechaInicio = fechaInicio,

            FechaFin = fechaFin,

            Activo = true,

            Sincronizado = false
        };

        db.FondosConvenios.Add(fondo);

        await db.SaveChangesAsync();

        return fondo;
    }


    public async Task CambiarEstadoAsync(
        int fondoId,
        bool activo)
    {
        using var db = new AppDbContext();

        var fondo =
            await db.FondosConvenios
                .FirstOrDefaultAsync(
                    x => x.Id == fondoId);

        if (fondo is null)
        {
            throw new InvalidOperationException(
                "El fondo seleccionado no existe.");
        }

        fondo.Activo = activo;

        fondo.Sincronizado = false;

        await db.SaveChangesAsync();
    }
}