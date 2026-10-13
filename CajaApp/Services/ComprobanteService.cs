using CajaApp.Data;
using CajaApp.Models;
using CajaApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CajaApp.Services;

public class ComprobanteService : IComprobanteService
{
    // ==========================================
    // NUMERACIÓN POR TERMINAL
    // ==========================================

    public const string PrefijoPorDefecto = "CJ01";

    /// <summary>
    /// Prefijo de esta terminal (appsettings: Caja:Terminal). Los comprobantes
    /// de ventanilla se numeran CJ01-000001, CJ01-000002... para que nunca
    /// choquen con los REC-n / FAC-n que emite el CORE para el Portal Web,
    /// ni con los de otra terminal. La numeración es local: funciona sin conexión.
    /// </summary>
    public static string PrefijoTerminal { get; set; } = PrefijoPorDefecto;

    /// <summary>Deja solo letras y dígitos en mayúscula (máx. 10). Vacío → CJ01.</summary>
    public static string NormalizarPrefijo(string? valor)
    {
        var limpio = new StringBuilder();

        foreach (char c in valor ?? string.Empty)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                limpio.Append(char.ToUpperInvariant(c));
            }
        }

        if (limpio.Length == 0)
        {
            return PrefijoPorDefecto;
        }

        return limpio.Length > 10
            ? limpio.ToString(0, 10)
            : limpio.ToString();
    }

    private static async Task<string> GenerarNumeroAsync(
        AppDbContext db)
    {
        string prefijo = $"{PrefijoTerminal}-";

        var existentes =
            await db.Comprobantes
                .AsNoTracking()
                .Where(x => x.NumeroComprobante.StartsWith(prefijo))
                .Select(x => x.NumeroComprobante)
                .ToListAsync();

        int ultimo = 0;

        foreach (var numero in existentes)
        {
            if (int.TryParse(numero.AsSpan(prefijo.Length), out int valor) &&
                valor > ultimo)
            {
                ultimo = valor;
            }
        }

        return $"{prefijo}{ultimo + 1:D6}";
    }

    public async Task<Comprobante> GenerarAsync(
        int transaccionId)
    {
        using var db =
            new AppDbContext();

        var transaccion =
            await db.Transacciones
                .FirstOrDefaultAsync(
                    x => x.Id == transaccionId);

        if (transaccion is null)
        {
            throw new InvalidOperationException(
                "La transacción no existe.");
        }


        // ======================================
        // EVITAR DUPLICADOS
        // ======================================

        var comprobanteExistente =
            await db.Comprobantes
                .FirstOrDefaultAsync(
                    x => x.TransaccionId ==
                         transaccionId);

        if (comprobanteExistente is not null)
        {
            return comprobanteExistente;
        }


        // ======================================
        // CREAR NÚMERO
        // ======================================

        var numeroComprobante =
            await GenerarNumeroAsync(db);


        // ======================================
        // CARPETA
        // ======================================

        string carpeta =
            Path.Combine(
                AppContext.BaseDirectory,
                "Comprobantes");

        Directory.CreateDirectory(
            carpeta);


        string rutaArchivo =
            Path.Combine(
                carpeta,
                $"{numeroComprobante}.pdf");


        // ======================================
        // PDF
        // ======================================

        GenerarPdf(
            transaccion,
            numeroComprobante,
            rutaArchivo);


        // ======================================
        // REGISTRO
        // ======================================

        var comprobante =
            new Comprobante
            {
                TransaccionId =
                    transaccion.Id,

                NumeroComprobante =
                    numeroComprobante,

                FechaGeneracion =
                    DateTime.Now,

                RutaArchivo =
                    rutaArchivo,

                TipoDocumento =
                    transaccion.EsSubsidiado
                        ? "CONSTANCIA DE ENTREGA"
                        : "RECIBO DE CAJA",

                EsReimpresion =
                    false,

                CantidadReimpresiones =
                    0,

                Sincronizado =
                    false
            };


        db.Comprobantes.Add(
            comprobante);


        // ======================================
        // COMPLETAR TRANSACCIÓN
        // ======================================

        transaccion.ComprobanteGenerado =
            true;

        transaccion.Estado =
            EstadoTransaccion.Completada;


        await db.SaveChangesAsync();

        return comprobante;
    }


    // ==========================================
    // PDF
    // ==========================================

    private static void GenerarPdf(
        Transaccion transaccion,
        string numeroComprobante,
        string rutaArchivo)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(
                    PageSizes.A5);

                page.Margin(
                    30);

                page.DefaultTextStyle(
                    x => x
                        .FontSize(10));


                // ==============================
                // HEADER
                // ==============================

                page.Header()
                    .Column(column =>
                    {
                        var logo = ObtenerLogo();

                        if (logo is not null)
                        {
                            // Logo institucional completo (Assets/logo-completo.png).
                            column.Item()
                                .AlignCenter()
                                .Height(46)
                                .Image(logo)
                                .FitHeight();
                        }
                        else
                        {
                            column.Item()
                                .AlignCenter()
                                .Text("CONAVIHSIDA")
                                .Bold()
                                .FontSize(19)
                                .FontColor("#163D63");

                            column.Item()
                                .AlignCenter()
                                .Text(
                                    "Consejo Nacional para el VIH y el SIDA")
                                .FontSize(9)
                                .FontColor("#526D82");
                        }

                        column.Item()
                            .PaddingTop(5)
                            .AlignCenter()
                            .Text(
                                transaccion.EsSubsidiado
                                    ? "CONSTANCIA DE ENTREGA SUBSIDIADA"
                                    : "RECIBO DE CAJA")
                            .Bold()
                            .FontSize(12);
                    });


                // ==============================
                // CONTENIDO
                // ==============================

                page.Content()
                    .PaddingVertical(20)
                    .Column(column =>
                    {
                        column.Spacing(8);


                        // ----------------------
                        // IDENTIFICADORES
                        // ----------------------

                        column.Item()
                            .BorderBottom(1)
                            .BorderColor("#D7E5EF")
                            .PaddingBottom(8)
                            .Column(info =>
                            {
                                info.Item()
                                    .Text(
                                        $"Comprobante: {numeroComprobante}");

                                info.Item()
                                    .Text(
                                        $"Transacción: {transaccion.NumeroTransaccion}");

                                info.Item()
                                    .Text(
                                        $"Fecha: {transaccion.Fecha:dd/MM/yyyy hh:mm tt}");
                            });


                        // ----------------------
                        // PACIENTE
                        // ----------------------

                        column.Item()
                            .PaddingTop(5)
                            .Text(
                                $"Paciente: {OcultarCedula(transaccion.CedulaPaciente)}");

                        column.Item()
                            .Text(
                                $"Servicio: {transaccion.TipoServicio}");

                        if (!string.IsNullOrWhiteSpace(transaccion.NumeroReceta))
                        {
                            column.Item()
                                .Text(
                                    $"Receta: {transaccion.NumeroReceta}");
                        }


                        // ----------------------
                        // FORMA PAGO
                        // ----------------------

                        column.Item()
                            .Text(
                                $"Forma de pago: {ObtenerFormaPago(transaccion.FormaPago)}");


                        // ----------------------
                        // MONTO
                        // ----------------------

                        column.Item()
                            .PaddingTop(12)
                            .Background("#EAF5FC")
                            .Padding(12)
                            .Row(row =>
                            {
                                row.RelativeItem()
                                    .Text(
                                        transaccion.EsSubsidiado
                                            ? "Balance del paciente"
                                            : "Total pagado")
                                    .FontColor("#526D82");

                                row.ConstantItem(160)
                                    .AlignRight()
                                    .Text(
                                        $"RD$ {transaccion.Monto:N2}")
                                    .Bold()
                                    .FontSize(18)
                                    .FontColor("#163D63");
                            });


                        // ----------------------
                        // SUBSIDIO
                        // ----------------------

                        if (transaccion.EsSubsidiado)
                        {
                            column.Item()
                                .PaddingTop(8)
                                .Background("#EEF8F2")
                                .Padding(10)
                                .Text(
                                    "Entrega cubierta mediante subsidio. Balance del paciente: RD$0.00.")
                                .FontSize(9)
                                .FontColor("#35694B");
                        }


                        // ----------------------
                        // CAJERO
                        // ----------------------

                        column.Item()
                            .PaddingTop(15)
                            .Text(
                                $"Cajero: {transaccion.UsuarioCajero}")
                            .FontSize(9);


                        column.Item()
                            .PaddingTop(10)
                            .Text(
                                "Documento generado electrónicamente por el Sistema de Caja de CONAVIHSIDA.")
                            .FontSize(8)
                            .FontColor("#7C93A8");
                    });


                // ==============================
                // FOOTER
                // ==============================

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span(
                            "CONAVIHSIDA · ");

                        text.Span(
                            DateTime.Now.ToString(
                                "dd/MM/yyyy"));
                    });
            });
        })
        .GeneratePdf(
            rutaArchivo);
    }


    // ==========================================
    // OBTENER COMPROBANTE
    // ==========================================

    public async Task<Comprobante?>
        ObtenerPorTransaccionAsync(
            int transaccionId)
    {
        using var db =
            new AppDbContext();

        return await db.Comprobantes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TransaccionId ==
                     transaccionId);
    }


    // ==========================================
    // REIMPRESIÓN
    // ==========================================

    public async Task<string> ReimprimirAsync(
        int transaccionId)
    {
        using var db =
            new AppDbContext();

        var comprobante =
            await db.Comprobantes
                .FirstOrDefaultAsync(
                    x => x.TransaccionId ==
                         transaccionId);

        if (comprobante is null)
        {
            throw new InvalidOperationException(
                "La transacción no tiene un comprobante generado.");
        }


        if (!File.Exists(
                comprobante.RutaArchivo))
        {
            throw new FileNotFoundException(
                "El archivo PDF del comprobante no fue encontrado.",
                comprobante.RutaArchivo);
        }


        comprobante.EsReimpresion =
            true;

        comprobante.CantidadReimpresiones++;


        await db.SaveChangesAsync();


        return comprobante.RutaArchivo;
    }


    // ==========================================
    // HELPERS
    // ==========================================

    private static string ObtenerFormaPago(
        FormaPago formaPago)
    {
        return formaPago switch
        {
            FormaPago.Efectivo =>
                "Efectivo",

            FormaPago.Tarjeta =>
                "Tarjeta",

            FormaPago.Subsidiado =>
                "Subsidio",

            _ =>
                "No especificado"
        };
    }


    private static string OcultarCedula(
        string cedula)
    {
        if (string.IsNullOrWhiteSpace(
                cedula))
        {
            return "—";
        }


        if (cedula.Length <= 4)
        {
            return cedula;
        }


        return "*****" +
               cedula[^4..];
    }

    // ==========================================
    // LOGO PARA EL PDF
    // ==========================================

    private static byte[]? _logo;

    /// <summary>
    /// Lee el logo desde los recursos de Avalonia. Si no está disponible
    /// (por ejemplo, en pruebas automáticas) el PDF usa el encabezado de texto.
    /// </summary>
    internal static byte[]? ObtenerLogo()
    {
        if (_logo is not null)
        {
            return _logo;
        }

        try
        {
            using var stream = Avalonia.Platform.AssetLoader.Open(
                new Uri("avares://CajaApp/Assets/logo-completo.png"));

            using var memoria = new MemoryStream();
            stream.CopyTo(memoria);
            _logo = memoria.ToArray();
        }
        catch
        {
            _logo = null;
        }

        return _logo;
    }
}
