using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using IntegrationPlanner.Layout;
using IntegrationPlanner.Persistence;
using IntegrationPlanner.UI.Diagnostics;
using IntegrationPlanner.UI.Export;
using IntegrationPlanner.UI.Logging;
using IntegrationPlanner.UI.Rendering;
using IntegrationPlanner.UI.Services;
using IntegrationPlanner.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationPlanner.UI;

public partial class App : Application
{
    private const string ScenePaletteKey = "ScenePalette";

    private ServiceProvider? _services;
    private UnhandledExceptionHandler? _exceptionHandler;

    // async void is the only option for this framework override; failures reach the dispatcher handler.
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ScenePalette palette = (ScenePalette)FindResource(ScenePaletteKey);
        _services = ConfigureServices(palette).BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        _exceptionHandler = _services.GetRequiredService<UnhandledExceptionHandler>();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        MainWindow mainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        // Optional first argument: a plan file to open at start-up, e.g. when a file is opened from Explorer.
        if (e.Args.Length > 0)
        {
            await _services.GetRequiredService<MainViewModel>().OpenFileAsync(e.Args[0], CancellationToken.None);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        _services?.Dispose();
        base.OnExit(e);
    }

    private static ServiceCollection ConfigureServices(ScenePalette palette)
    {
        ServiceCollection services = new();
        services.AddLogging(logging => logging
            .AddDebug()
            .AddProvider(new FileLoggerProvider(new FileLoggerOptions(FileLoggerOptions.DefaultDirectory), TimeProvider.System)));
        services.AddSingleton(palette);
        services.AddSingleton(LayoutOptions.Default);
        services.AddSingleton<ITextMeasurer, WpfTextMeasurer>();
        services.AddSingleton<IDiagramLayoutEngine, DiagramLayoutEngine>();
        services.AddSingleton<IPlanFileStore, PlanFileStore>();
        services.AddSingleton<IMessageService, MessageService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<SvgSceneWriter>();
        services.AddSingleton<ISceneExporter, SceneExporter>();
        services.AddSingleton<UnhandledExceptionHandler>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainWindow>();
        return services;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _exceptionHandler?.HandleOnUiThread(e.Exception);

        // The user has been told and the exception is logged; keeping the read-only viewer alive is safer than closing it.
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e) =>
        _exceptionHandler?.HandleTerminating(e.ExceptionObject);

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _exceptionHandler?.HandleUnobservedTask(e.Exception);
        e.SetObserved();
    }
}
