using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.Persistence;
using IntegrationPlanner.UI.Editing;
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
    private const string PlanFileExtension = ".iplan.json";

    private readonly IPlanFileStore _planFileStore;
    private readonly IDiagramLayoutEngine _layoutEngine;
    private readonly IFileDialogService _fileDialogs;
    private readonly IMessageService _messages;
    private readonly ISceneExporter _exporter;
    private readonly LayoutOptions _layoutOptions;
    private readonly ILogger<MainViewModel> _logger;

    private PlanEditor? _editor;
    private Plan? _savedPlan;
    private string? _filePath;

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
        LayoutOptions layoutOptions,
        ILogger<MainViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(planFileStore);
        ArgumentNullException.ThrowIfNull(layoutEngine);
        ArgumentNullException.ThrowIfNull(fileDialogs);
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(layoutOptions);
        ArgumentNullException.ThrowIfNull(logger);
        _planFileStore = planFileStore;
        _layoutEngine = layoutEngine;
        _fileDialogs = fileDialogs;
        _messages = messages;
        _exporter = exporter;
        _layoutOptions = layoutOptions;
        _logger = logger;
    }

    public ObservableCollection<ScenarioItem> Scenarios { get; } = [];

    public bool HasPlan => _editor is not null;

    public bool HasUnsavedChanges => _editor is not null && !ReferenceEquals(_editor.Current, _savedPlan);

    public string WindowTitle =>
        _editor is null
            ? UiText.MainWindowTitle
            : UiText.Format(HasUnsavedChanges ? UiText.MainWindowTitleWithFileUnsaved : UiText.MainWindowTitleWithFile, FileName ?? UiText.UntitledFileName);

    // Asks about unsaved changes before they would be lost; false means the user wants to keep them.
    public async Task<bool> ConfirmDiscardChangesAsync(CancellationToken cancellationToken)
    {
        if (!HasUnsavedChanges)
        {
            return true;
        }

        return _messages.AskSaveChanges() switch
        {
            SaveChangesAnswer.Discard => true,
            SaveChangesAnswer.Save => await SaveDocumentAsync(_filePath, cancellationToken),
            _ => false,
        };
    }

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

        LoadDocument(result.Plan, path);
        if (result.DroppedReferences.Count > 0)
        {
            _messages.ShowInformation(UiText.Format(UiText.InfoDroppedReferences, result.DroppedReferences.Count));
        }
    }

    [RelayCommand]
    private async Task NewAsync(CancellationToken cancellationToken)
    {
        if (await ConfirmDiscardChangesAsync(cancellationToken))
        {
            LoadDocument(PlanItemFactory.NewPlan(), null);
        }
    }

    [RelayCommand]
    private async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (!await ConfirmDiscardChangesAsync(cancellationToken))
        {
            return;
        }

        string? path = _fileDialogs.PickPlanToOpen();
        if (path is null)
        {
            return;
        }

        await OpenFileAsync(path, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(HasPlan))]
    private Task SaveAsync(CancellationToken cancellationToken) => SaveDocumentAsync(_filePath, cancellationToken);

    [RelayCommand(CanExecute = nameof(HasPlan))]
    private Task SaveAsAsync(CancellationToken cancellationToken) => SaveDocumentAsync(null, cancellationToken);

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

    partial void OnSelectedScenarioChanged(ScenarioItem? value)
    {
        if (_isSynchronizing || _editor is null)
        {
            return;
        }

        Relayout();
        SetSelection(_selection);
        UpdateCommandStates();
    }

    private void LoadDocument(Plan plan, string? filePath)
    {
        if (_editor is not null)
        {
            _editor.Changed -= OnEditorChanged;
        }

        _editor = new PlanEditor(plan);
        _editor.Changed += OnEditorChanged;
        _savedPlan = plan;
        _filePath = filePath;
        FileName = filePath is null ? null : Path.GetFileName(filePath);
        _selection = null;
        _isSynchronizing = true;
        try
        {
            SelectedScenario = null;
            Scenarios.Clear();
            Branches.Clear();
        }
        finally
        {
            _isSynchronizing = false;
        }

        Synchronize();
        OnPropertyChanged(nameof(HasPlan));
    }

    // Saves to the given path, or asks for one; returns whether the plan was saved.
    private async Task<bool> SaveDocumentAsync(string? filePath, CancellationToken cancellationToken)
    {
        string? path = filePath ?? _fileDialogs.PickPlanSaveTarget(SuggestedFileName(PlanFileExtension));
        if (path is null)
        {
            return false;
        }

        Plan plan = _editor!.Current;
        try
        {
            await _planFileStore.SaveAsync(plan, path, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "{Operation} could not write the plan file", nameof(SaveDocumentAsync));
            _messages.ShowError(UiText.ErrorSaveFailed);
            return false;
        }

        _savedPlan = plan;
        _filePath = path;
        FileName = Path.GetFileName(path);
        OnPropertyChanged(nameof(HasUnsavedChanges));
        OnPropertyChanged(nameof(WindowTitle));
        return true;
    }

    private async Task ExportAsync(ExportFormat format, CancellationToken cancellationToken)
    {
        string? path = _fileDialogs.PickExportTarget(format, SuggestedFileName(format == ExportFormat.Png ? ".png" : ".svg"));
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

    private string SuggestedFileName(string extension)
    {
        string baseName = extension == PlanFileExtension
            ? _editor?.Current.Title ?? string.Empty
            : $"{_editor?.Current.Title} - {SelectedScenario?.Name}".Trim(' ', '-');
        char[] invalid = Path.GetInvalidFileNameChars();
        string safeName = string.Concat(baseName.Select(character => invalid.Contains(character) ? '_' : character));
        return $"{(safeName.Length == 0 ? "diagram" : safeName)}{extension}";
    }
}
