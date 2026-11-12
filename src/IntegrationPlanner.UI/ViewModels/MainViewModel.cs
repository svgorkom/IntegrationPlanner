using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.Persistence;
using IntegrationPlanner.UI.Export;
using IntegrationPlanner.UI.Localization;
using IntegrationPlanner.UI.Services;
using Microsoft.Extensions.Logging;

namespace IntegrationPlanner.UI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    public const double MinimumZoom = 0.25;
    public const double MaximumZoom = 4.0;
    public const double ZoomFactor = 1.25;

    private readonly IPlanFileStore _planFileStore;
    private readonly IDiagramLayoutEngine _layoutEngine;
    private readonly IFileDialogService _fileDialogs;
    private readonly IMessageService _messages;
    private readonly ISceneExporter _exporter;
    private readonly ILogger<MainViewModel> _logger;

    private Plan? _plan;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string? _fileName;

    [ObservableProperty]
    private ScenarioItem? _selectedScenario;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportPngCommand), nameof(ExportSvgCommand), nameof(CopyToClipboardCommand))]
    private DiagramScene? _scene;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ZoomInCommand), nameof(ZoomOutCommand))]
    private double _zoom = 1.0;

    public MainViewModel(
        IPlanFileStore planFileStore,
        IDiagramLayoutEngine layoutEngine,
        IFileDialogService fileDialogs,
        IMessageService messages,
        ISceneExporter exporter,
        ILogger<MainViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(planFileStore);
        ArgumentNullException.ThrowIfNull(layoutEngine);
        ArgumentNullException.ThrowIfNull(fileDialogs);
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(logger);
        _planFileStore = planFileStore;
        _layoutEngine = layoutEngine;
        _fileDialogs = fileDialogs;
        _messages = messages;
        _exporter = exporter;
        _logger = logger;
    }

    public ObservableCollection<ScenarioItem> Scenarios { get; } = [];

    public string WindowTitle => FileName is null ? UiText.MainWindowTitle : UiText.Format(UiText.MainWindowTitleWithFile, FileName);

    public async Task OpenFileAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        PlanLoadResult result;
        try
        {
            result = await _planFileStore.LoadAsync(path, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "{Operation} could not read the plan file", nameof(OpenFileAsync));
            _messages.ShowError(UiText.ErrorOpenRead);
            return;
        }

        if (result.Plan is null)
        {
            _messages.ShowError(result.Error == PlanLoadError.UnsupportedSchemaVersion ? UiText.ErrorOpenUnsupportedVersion : UiText.ErrorOpenCorrupt);
            return;
        }

        ShowPlan(result.Plan, Path.GetFileName(path));
        if (result.DroppedReferences.Count > 0)
        {
            _messages.ShowInformation(UiText.Format(UiText.InfoDroppedReferences, result.DroppedReferences.Count));
        }
    }

    [RelayCommand]
    private async Task OpenAsync(CancellationToken cancellationToken)
    {
        string? path = _fileDialogs.PickPlanToOpen();
        if (path is null)
        {
            return;
        }

        await OpenFileAsync(path, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanZoomIn))]
    private void ZoomIn() => Zoom = Math.Min(MaximumZoom, Zoom * ZoomFactor);

    [RelayCommand(CanExecute = nameof(CanZoomOut))]
    private void ZoomOut() => Zoom = Math.Max(MinimumZoom, Zoom / ZoomFactor);

    [RelayCommand]
    private void ResetZoom() => Zoom = 1.0;

    [RelayCommand(CanExecute = nameof(HasScene))]
    private Task ExportPngAsync(CancellationToken cancellationToken) => ExportAsync(ExportFormat.Png, cancellationToken);

    [RelayCommand(CanExecute = nameof(HasScene))]
    private Task ExportSvgAsync(CancellationToken cancellationToken) => ExportAsync(ExportFormat.Svg, cancellationToken);

    [RelayCommand(CanExecute = nameof(HasScene))]
    private void CopyToClipboard()
    {
        try
        {
            _exporter.CopyToClipboard(Scene!);
        }
        catch (ExternalException exception)
        {
            _logger.LogWarning(exception, "{Operation} failed", nameof(CopyToClipboard));
            _messages.ShowError(UiText.ErrorCopyFailed);
        }
    }

    private bool CanZoomIn() => Zoom < MaximumZoom;

    private bool CanZoomOut() => Zoom > MinimumZoom;

    private bool HasScene() => Scene is not null;

    partial void OnSelectedScenarioChanged(ScenarioItem? value) =>
        Scene = _plan is null || value is null ? null : _layoutEngine.Layout(_plan, value.Id);

    private void ShowPlan(Plan plan, string fileName)
    {
        _plan = plan;
        FileName = fileName;
        Scenarios.Clear();
        foreach (Scenario scenario in plan.Scenarios)
        {
            Scenarios.Add(new ScenarioItem(scenario.Id, scenario.Name));
        }

        SelectedScenario = null;
        SelectedScenario = Scenarios.FirstOrDefault();
    }

    private async Task ExportAsync(ExportFormat format, CancellationToken cancellationToken)
    {
        string? path = _fileDialogs.PickExportTarget(format, SuggestedFileName(format));
        if (path is null)
        {
            return;
        }

        try
        {
            await _exporter.ExportAsync(Scene!, format, path, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "{Operation} failed for {Format}", nameof(ExportAsync), format);
            _messages.ShowError(UiText.ErrorExportFailed);
        }
    }

    private string SuggestedFileName(ExportFormat format)
    {
        string baseName = $"{_plan?.Title} - {SelectedScenario?.Name}".Trim(' ', '-');
        char[] invalid = Path.GetInvalidFileNameChars();
        string safeName = string.Concat(baseName.Select(character => invalid.Contains(character) ? '_' : character));
        string extension = format == ExportFormat.Png ? ".png" : ".svg";
        return $"{(safeName.Length == 0 ? "diagram" : safeName)}{extension}";
    }
}
