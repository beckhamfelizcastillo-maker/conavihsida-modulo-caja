using CajaApp.Services.Interfaces;
using CajaApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CajaApp.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    private MainViewModel? _mainViewModel;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ConfigurarMainViewModel(
        MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public void MostrarLogin()
    {
        if (_mainViewModel is null)
            return;

        _mainViewModel.CurrentViewModel =
            _serviceProvider
                .GetRequiredService<LoginViewModel>();
    }

    public void MostrarShell()
    {
        if (_mainViewModel is null)
            return;

        _mainViewModel.CurrentViewModel =
            _serviceProvider
                .GetRequiredService<ShellViewModel>();
    }
}