using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CajaApp.ViewModels.Items;

/// <summary>Una pregunta frecuente que se abre y se cierra con un clic.</summary>
public partial class PreguntaItem : ObservableObject
{
    public string Pregunta { get; }

    public string Respuesta { get; }

    [ObservableProperty]
    private bool _abierta;

    public PreguntaItem(string pregunta, string respuesta)
    {
        Pregunta = pregunta;
        Respuesta = respuesta;
    }

    [RelayCommand]
    private void Alternar() => Abierta = !Abierta;
}

/// <summary>Grupo de preguntas frecuentes (por ejemplo, "Cuadre y cierre").</summary>
public partial class CategoriaAyuda : ObservableObject
{
    public string Titulo { get; }

    public string Descripcion { get; }

    /// <summary>Todas las preguntas del tema.</summary>
    public IReadOnlyList<PreguntaItem> Todas { get; }

    /// <summary>Preguntas que coinciden con la búsqueda actual.</summary>
    public ObservableCollection<PreguntaItem> Visibles { get; } = new();

    [ObservableProperty]
    private bool _tieneResultados = true;

    [ObservableProperty]
    private bool _seleccionada;

    public CategoriaAyuda(string titulo, string descripcion, params PreguntaItem[] preguntas)
    {
        Titulo = titulo;
        Descripcion = descripcion;
        Todas = preguntas;

        foreach (var pregunta in preguntas)
        {
            Visibles.Add(pregunta);
        }
    }
}
