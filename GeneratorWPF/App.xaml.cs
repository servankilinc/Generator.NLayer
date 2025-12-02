using GeneratorWPF.Services;
using GeneratorWPF.ViewModel;
using GeneratorWPF.ViewModel._Dto;
using GeneratorWPF.ViewModel._Entity;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Microsoft.Extensions.Hosting;
namespace GeneratorWPF;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }
    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<INavigationService, NavigationService>();

                services.AddSingleton<MainWindow>();

                services.AddTransient<MainWindowVM>();
                services.AddTransient<HomeVM>();
                services.AddTransient<EntityHomeVM>();
                services.AddTransient<EntityDetailVM>();
                services.AddTransient<DtoHomeVM>();
                services.AddTransient<DtoDetailVM>();
                services.AddTransient<EntranceVM>();
            })
            .Build();
    }


    protected override async void OnStartup(StartupEventArgs e)
    {
        if (AppHost is null) throw new InvalidOperationException("AppHost is not initialized.");

        await AppHost.StartAsync();

        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (AppHost is not null)
        {
            await AppHost.StopAsync();
            AppHost.Dispose();
        }

        base.OnExit(e);
    }
}
