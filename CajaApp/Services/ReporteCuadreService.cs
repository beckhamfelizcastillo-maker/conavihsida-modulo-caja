using CajaApp.Data;
using CajaApp.Helpers;
using CajaApp.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CajaApp.Services;

/// <summary>
/// Reporte de cuadre de caja en PDF (RF-CAJ-02): balance inicial, cobros,
/// efectivo esperado, efectivo contado y la diferencia (sobrante o faltante),
/// con el detalle de las operaciones del turno. Se genera sin conexión.
/// </summary>
public class ReporteCuadreService
{
    public async Task<string> GenerarAsync(int turnoId)
    {
        using var db = new AppDbContext();

        var turno = await db.TurnosCaja
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == turnoId)
            ?? throw new InvalidOperationException("El turno no existe.");

        if (turno.Estado != EstadoTurnoCaja.Cerrado)
        {
            throw new InvalidOperationException("El reporte de cuadre se genera después de cerrar el turno.");
        }

        var referencias = await db.MovimientosCaja
            .AsNoTracking()
            .Where(x => x.TurnoCajaId == turno.Id && x.Tipo == TipoMovimientoCaja.Cobro)
            .Select(x => x.Referencia)
            .ToListAsync();

        DateTime hasta = turno.FechaCierre ?? DateTime.Now;

        var operaciones = await db.Transacciones
            .AsNoTracking()
            .Where(x =>
                referencias.Contains(x.NumeroTransaccion) ||
                (x.EsSubsidiado && x.Fecha >= turno.FechaApertura && x.Fecha <= hasta))
            .OrderBy(x => x.Fecha)
            .ToListAsync();

        var comprobantes = await db.Comprobantes
            .AsNoTracking()
            .Select(x => new { x.TransaccionId, x.NumeroComprobante })
            .ToListAsync();

        var numeroComprobante = comprobantes
            .GroupBy(x => x.TransaccionId)
            .ToDictionary(g => g.Key, g => g.First().NumeroComprobante);

        var completadas = operaciones.Where(x => x.Estado == EstadoTransaccion.Completada).ToList();

        decimal efectivo = completadas.Where(x => x.FormaPago == FormaPago.Efectivo).Sum(x => x.Monto);
        decimal tarjeta = completadas.Where(x => x.FormaPago == FormaPago.Tarjeta).Sum(x => x.Monto);
        int subsidiadas = completadas.Count(x => x.EsSubsidiado);

        decimal declarado = turno.EfectivoDeclarado ?? 0m;
        decimal diferencia = turno.Diferencia ?? 0m;
        decimal esperado = declarado - diferencia;

        string resultado = diferencia == 0
            ? "CUADRE EXACTO"
            : diferencia > 0
                ? $"SOBRANTE DE {Formato.Moneda(diferencia)}"
                : $"FALTANTE (PÉRDIDA) DE {Formato.Moneda(Math.Abs(diferencia))}";

        string colorResultado = diferencia == 0 ? "#35694B" : diferencia > 0 ? "#8A6D1F" : "#A12B2B";
        string fondoResultado = diferencia == 0 ? "#EEF8F2" : diferencia > 0 ? "#FFF6DD" : "#FCEBEB";

        string carpeta = Path.Combine(AppContext.BaseDirectory, "Reportes");
        Directory.CreateDirectory(carpeta);

        string ruta = Path.Combine(carpeta, $"Cuadre-{turno.NumeroTurno}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    var logo = ComprobanteService.ObtenerLogo();

                    if (logo is not null)
                    {
                        col.Item().AlignCenter().Height(46).Image(logo).FitHeight();
                    }
                    else
                    {
                        col.Item().AlignCenter().Text("CONAVIHSIDA").Bold().FontSize(19).FontColor("#163D63");
                    }

                    col.Item().PaddingTop(6).AlignCenter().Text("REPORTE DE CUADRE DE CAJA").Bold().FontSize(13);
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });

                        void Dato(string etiqueta, string valor)
                        {
                            t.Cell().PaddingVertical(2).Text(etiqueta).FontColor("#526D82");
                            t.Cell().PaddingVertical(2).Text(valor);
                        }

                        Dato("Turno", turno.NumeroTurno);
                        Dato("Caja", turno.Caja);
                        Dato("Cajero", $"{turno.NombreCajero} ({turno.UsuarioCajero})");
                        Dato("Apertura", turno.FechaApertura.ToString("dd/MM/yyyy hh:mm tt"));
                        Dato("Cierre", hasta.ToString("dd/MM/yyyy hh:mm tt"));
                    });

                    col.Item().BorderTop(1).BorderColor("#D7E5EF").PaddingTop(8).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); });

                        void Linea(string etiqueta, string valor, bool negrita = false)
                        {
                            var a = t.Cell().PaddingVertical(3).Text(etiqueta);
                            var b = t.Cell().PaddingVertical(3).AlignRight().Text(valor);
                            if (negrita) { a.Bold(); b.Bold(); }
                        }

                        Linea("Balance inicial", Formato.Moneda(turno.BalanceInicial));
                        Linea("Cobros en efectivo", Formato.Moneda(efectivo));
                        Linea("Cobros con tarjeta", Formato.Moneda(tarjeta));
                        Linea("Total cobrado", Formato.Moneda(efectivo + tarjeta));
                        Linea("Entregas subsidiadas (RD$ 0.00)", subsidiadas.ToString());
                        Linea("Efectivo esperado en caja", Formato.Moneda(esperado), true);
                        Linea("Efectivo contado", Formato.Moneda(declarado), true);
                        Linea("Diferencia", Formato.MonedaConSigno(diferencia), true);
                    });

                    col.Item().Background(fondoResultado).Padding(10).AlignCenter()
                        .Text(resultado).Bold().FontSize(13).FontColor(colorResultado);

                    if (!string.IsNullOrWhiteSpace(turno.Observacion))
                    {
                        col.Item().Text($"Observación: {turno.Observacion}").FontSize(9.5f);
                    }

                    col.Item().PaddingTop(8).Text($"Operaciones del turno ({operaciones.Count})").Bold();

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(50);
                            c.RelativeColumn(3);
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        t.Header(h =>
                        {
                            foreach (var titulo in new[] { "Hora", "Transacción", "Servicio", "Pago", "Comprobante", "Monto" })
                            {
                                h.Cell().Background("#EAF5FC").Padding(4).Text(titulo).Bold().FontSize(8.5f);
                            }
                        });

                        foreach (var op in operaciones)
                        {
                            string pago = op.Estado == EstadoTransaccion.Anulada ? "Anulada" : op.FormaPago.ToString();
                            numeroComprobante.TryGetValue(op.Id, out var comp);

                            t.Cell().BorderBottom(0.5f).BorderColor("#D7E5EF").Padding(3).Text(op.Fecha.ToString("HH:mm")).FontSize(8.5f);
                            t.Cell().BorderBottom(0.5f).BorderColor("#D7E5EF").Padding(3).Text(op.NumeroTransaccion).FontSize(8.5f);
                            t.Cell().BorderBottom(0.5f).BorderColor("#D7E5EF").Padding(3).Text(op.TipoServicio).FontSize(8.5f);
                            t.Cell().BorderBottom(0.5f).BorderColor("#D7E5EF").Padding(3).Text(pago).FontSize(8.5f);
                            t.Cell().BorderBottom(0.5f).BorderColor("#D7E5EF").Padding(3).Text(comp ?? "—").FontSize(8.5f);
                            t.Cell().BorderBottom(0.5f).BorderColor("#D7E5EF").Padding(3).AlignRight().Text(Formato.Moneda(op.Monto)).FontSize(8.5f);
                        }
                    });

                    col.Item().PaddingTop(30).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().BorderTop(1).PaddingTop(3).AlignCenter().Text("Firma del cajero").FontSize(9);
                        });
                        r.ConstantItem(40);
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().BorderTop(1).PaddingTop(3).AlignCenter().Text("Firma del supervisor").FontSize(9);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("CONAVIHSIDA · Sistema de Caja · Generado ");
                    x.Span(DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"));
                });
            });
        }).GeneratePdf(ruta);

        return ruta;
    }
}
