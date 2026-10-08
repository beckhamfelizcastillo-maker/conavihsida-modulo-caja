using CajaApp.ViewModels.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CajaApp.ViewModels;

/// <summary>
/// Preguntas frecuentes del módulo Caja. Reúne en un solo lugar las
/// explicaciones de uso que antes estaban repartidas en cada pantalla.
/// </summary>
public partial class AyudaViewModel : ViewModelBase
{
    public ObservableCollection<CategoriaAyuda> Categorias { get; }

    /// <summary>Temas que se muestran según el filtro y la búsqueda.</summary>
    public ObservableCollection<CategoriaAyuda> CategoriasVisibles { get; } = new();

    [ObservableProperty]
    private string _busqueda = string.Empty;

    [ObservableProperty]
    private CategoriaAyuda? _categoriaSeleccionada;

    [ObservableProperty]
    private bool _sinResultados;

    public bool TodasSeleccionadas => CategoriaSeleccionada is null;

    public AyudaViewModel()
    {
        Categorias = new ObservableCollection<CategoriaAyuda>(CrearContenido());
        AplicarFiltro();
    }

    partial void OnBusquedaChanged(string value) => AplicarFiltro();

    partial void OnCategoriaSeleccionadaChanged(CategoriaAyuda? value)
    {
        foreach (var categoria in Categorias)
        {
            categoria.Seleccionada = categoria == value;
        }

        OnPropertyChanged(nameof(TodasSeleccionadas));
        AplicarFiltro();
    }

    [RelayCommand]
    private void VerTodas() => CategoriaSeleccionada = null;

    [RelayCommand]
    private void VerCategoria(CategoriaAyuda categoria) => CategoriaSeleccionada = categoria;

    [RelayCommand]
    private void LimpiarBusqueda() => Busqueda = string.Empty;

    // ==========================================
    // FILTRO
    // ==========================================

    private void AplicarFiltro()
    {
        string termino = Normalizar(Busqueda);
        bool buscando = termino.Length > 0;

        CategoriasVisibles.Clear();

        foreach (var categoria in Categorias)
        {
            if (CategoriaSeleccionada is not null && categoria != CategoriaSeleccionada)
            {
                continue;
            }

            categoria.Visibles.Clear();

            foreach (var pregunta in categoria.Todas)
            {
                bool coincide = !buscando ||
                                Normalizar(pregunta.Pregunta).Contains(termino) ||
                                Normalizar(pregunta.Respuesta).Contains(termino);

                if (coincide)
                {
                    // Al buscar, se abren las respuestas para leerlas de una vez.
                    pregunta.Abierta = buscando;
                    categoria.Visibles.Add(pregunta);
                }
            }

            categoria.TieneResultados = categoria.Visibles.Count > 0;

            if (categoria.TieneResultados)
            {
                CategoriasVisibles.Add(categoria);
            }
        }

        SinResultados = CategoriasVisibles.Count == 0;
    }

    /// <summary>Minúsculas y sin tildes, para que "cedula" encuentre "cédula".</summary>
    private static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);

        foreach (char c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // ==========================================
    // CONTENIDO
    // ==========================================

    private static CategoriaAyuda[] CrearContenido() => new[]
    {
        new CategoriaAyuda(
            "Turno y apertura",
            "Inicio de la jornada en la ventanilla.",
            new PreguntaItem(
                "¿Para qué sirve la apertura de caja?",
                "La apertura fija el punto de partida del turno. El sistema usa el balance inicial para calcular el cuadre al cierre, comparando el efectivo esperado contra el efectivo contado físicamente."),
            new PreguntaItem(
                "¿Puedo tener dos turnos abiertos al mismo tiempo?",
                "No. Solo puede haber un turno abierto por caja. Para abrir uno nuevo, primero cierra el actual en Cuadre y cierre."),
            new PreguntaItem(
                "¿Por qué no puedo entrar a Nueva transacción o a Cuadre y cierre?",
                "Esas pantallas necesitan una caja abierta. Si no hay turno, el sistema te lleva a Apertura de caja."),
            new PreguntaItem(
                "¿Queda constancia de la apertura?",
                "Sí. La apertura se registra en la bitácora con tu usuario, la terminal, la hora y el balance inicial.")),

        new CategoriaAyuda(
            "Cobros y comprobantes",
            "Registro de servicios y emisión de recibos.",
            new PreguntaItem(
                "¿Cómo registro un cobro?",
                "Ve a Nueva transacción, escribe la cédula, elige el servicio, el monto y la forma de pago, y presiona Registrar. Al terminar aparece el número de comprobante y el botón para ver el PDF."),
            new PreguntaItem(
                "¿El comprobante es obligatorio?",
                "Sí. Al registrar, el sistema guarda la operación localmente, genera el PDF del recibo y la marca como completada. Si Integration no está disponible, la operación se sincroniza después de forma automática."),
            new PreguntaItem(
                "¿Cómo debo escribir la cédula y el monto?",
                "La cédula tiene 11 dígitos y se puede escribir con o sin guiones (00112345678 o 001-1234567-8). El monto acepta formatos como 5000, 5,000.50 o RD$ 1,250, con un tope de RD$ 1,000,000 por operación."),
            new PreguntaItem(
                "¿Por qué un pago con tarjeta no cambia el efectivo esperado?",
                "El efectivo esperado suma solo los cobros en efectivo. Los pagos con tarjeta y las entregas subsidiadas no cambian el dinero físico de la caja."),
            new PreguntaItem(
                "¿Cómo vuelvo a abrir o imprimir un comprobante?",
                "En Historial, busca la operación por número, cédula o servicio y presiona PDF. Cada reimpresión queda registrada.")),

        new CategoriaAyuda(
            "Subsidios",
            "Validación de elegibilidad y entregas gratuitas.",
            new PreguntaItem(
                "¿Qué información muestra la validación de subsidio?",
                "Solo el estado del subsidio y el dato mínimo para identificar al titular. El expediente clínico, el diagnóstico y el tratamiento permanecen fuera de este módulo, conforme a la separación de información del sistema (Ley 172-13 y RN-03)."),
            new PreguntaItem(
                "¿Qué significa cada estado del subsidio?",
                "Activo: la operación se registra con gratuidad (RD$ 0.00).\nVencido: la cobertura expiró y se cobra el servicio.\nNo registrado: la cédula no está en el padrón de subsidios."),
            new PreguntaItem(
                "¿Por qué se reinició la validación al cambiar la cédula?",
                "La gratuidad solo se aplica a la cédula que se validó. Si se cambia algún dígito, el sistema vuelve a cobrar el servicio para evitar aplicar el subsidio a otra persona."),
            new PreguntaItem(
                "¿Una entrega subsidiada mueve dinero de la caja?",
                "No. Se registra con monto RD$ 0.00 y emite una constancia de entrega subsidiada, sin movimiento de efectivo.")),

        new CategoriaAyuda(
            "Cuadre y cierre",
            "Verificación del efectivo al final del turno.",
            new PreguntaItem(
                "¿Cómo se calcula la diferencia del cuadre?",
                "Diferencia = efectivo contado − efectivo esperado. El efectivo esperado es el balance inicial más los cobros en efectivo del turno."),
            new PreguntaItem(
                "¿Por qué me pide una observación?",
                "Si hay sobrante o faltante, el cierre exige una observación de al menos 10 caracteres que explique la diferencia."),
            new PreguntaItem(
                "¿Qué pasa después de cerrar el turno?",
                "No se pueden registrar más transacciones en esa caja hasta abrir un turno nuevo. El cierre queda registrado en la bitácora.")),

        new CategoriaAyuda(
            "Conexión y sincronización",
            "Trabajo con y sin conexión a Integration.",
            new PreguntaItem(
                "¿Qué significa «Sin conexión · N pendientes»?",
                "Integration no está respondiendo. Caja sigue trabajando normalmente y guarda cada operación en la base local; N es la cantidad de registros que todavía no se han enviado."),
            new PreguntaItem(
                "¿Se pierden las operaciones si no hay conexión?",
                "No. Quedan guardadas en la terminal y se envían automáticamente cuando Integration vuelve a estar disponible. El sistema lo intenta cada 30 segundos."),
            new PreguntaItem(
                "¿Para qué sirve el botón Sincronizar ahora?",
                "Fuerza un intento de envío inmediato, sin esperar el siguiente ciclo automático.")),

        new CategoriaAyuda(
            "Seguridad y uso general",
            "Bitácora, privacidad y navegación.",
            new PreguntaItem(
                "¿Qué queda registrado en la bitácora de auditoría?",
                "Inicios y cierres de sesión, aperturas, cobros, consultas de subsidio, comprobantes, reimpresiones, cierres, fondos y sincronizaciones, con fecha, usuario y terminal. Las cédulas aparecen enmascaradas y no se guardan datos clínicos."),
            new PreguntaItem(
                "¿Dónde está el archivo de la bitácora?",
                "En la carpeta Logs, junto al programa, con un archivo por día (caja-AAAAMMDD.log). Se conservan los últimos 30 días."),
            new PreguntaItem(
                "¿Para qué sirve el logo de la barra lateral?",
                "Al hacer clic vuelves a Inicio y se actualizan el turno, los indicadores y el estado de sincronización.")),

        new CategoriaAyuda(
            "Datos de prueba",
            "Para la demostración del sistema.",
            new PreguntaItem(
                "¿Con qué usuario entro en modo demostración?",
                "Usuario: cajero · Contraseña: 1234."),
            new PreguntaItem(
                "¿Qué cédulas puedo usar para probar los subsidios?",
                "001-1234567-8: subsidio activo.\n001-9999999-9: subsidio vencido.\nCualquier otra cédula válida aparece como no registrada.")),
    };
}
