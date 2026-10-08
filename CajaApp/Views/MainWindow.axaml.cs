using Avalonia.Controls;
using System;

namespace CajaApp.Views;

public partial class MainWindow : Window
{
    // Tamaño cómodo en pantallas grandes. Si la pantalla es más
    // pequeña (por ejemplo, una laptop de 1366 × 768), la ventana
    // se abre maximizada para que todo quepa.
    private const double AnchoPreferido = 1320;
    private const double AltoPreferido = 840;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        AjustarAPantalla();
    }

    private void AjustarAPantalla()
    {
        var pantalla = Screens.ScreenFromWindow(this) ?? Screens.Primary;

        if (pantalla is null)
        {
            WindowState = WindowState.Maximized;
            return;
        }

        // El área de trabajo excluye la barra de tareas de Windows.
        // Se convierte de píxeles físicos a unidades de Avalonia
        // para respetar la escala de pantalla (125 %, 150 %…).
        double escala = pantalla.Scaling > 0 ? pantalla.Scaling : 1;
        double anchoDisponible = pantalla.WorkingArea.Width / escala;
        double altoDisponible = pantalla.WorkingArea.Height / escala;

        // Margen para la barra de título y el borde de la ventana.
        const double margen = 48;

        if (anchoDisponible < AnchoPreferido + margen ||
            altoDisponible < AltoPreferido + margen)
        {
            WindowState = WindowState.Maximized;
            return;
        }

        Width = AnchoPreferido;
        Height = AltoPreferido;

        var area = pantalla.WorkingArea;
        int x = area.X + (int)((area.Width - AnchoPreferido * escala) / 2);
        int y = area.Y + (int)((area.Height - AltoPreferido * escala) / 2);
        Position = new Avalonia.PixelPoint(x, y);
    }
}
