using CajaApp.ViewModels;

namespace CajaApp.Services.Interfaces;

public interface INavigationService
{
    void MostrarLogin();

    void MostrarShell();

    void ConfigurarMainViewModel(MainViewModel mainViewModel);
}