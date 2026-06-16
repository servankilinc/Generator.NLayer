using GeneratorWPF.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace GeneratorWPF.Services;

public interface INavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
}


public class NavigationService: ObversableObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;


    private BaseViewModel _currentView;
    public BaseViewModel CurrentView { get => _currentView;  private set { _currentView = value; OnPropertyChanged(); } }

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
    {
        // ViewModel'i DI konteynerinden almak için
        CurrentView = _serviceProvider.GetRequiredService<TViewModel>();
    }
}
