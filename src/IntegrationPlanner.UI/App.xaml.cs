using System.Windows;
using IntegrationPlanner.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationPlanner.UI;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _services = ConfigureServices().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        MainWindow mainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }

    private static ServiceCollection ConfigureServices()
    {
        ServiceCollection services = new();
        services.AddLogging(logging => logging.AddDebug());
        services.AddSingleton<IPlanFileStore, PlanFileStore>();
        services.AddTransient<MainWindow>();
        return services;
    }
}
