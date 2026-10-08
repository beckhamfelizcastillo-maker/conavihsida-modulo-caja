namespace CajaApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;

        set => SetProperty(
            ref _currentViewModel,
            value);
    }

    public MainViewModel(
        LoginViewModel loginViewModel)
    {
        _currentViewModel =
            loginViewModel;
    }
}