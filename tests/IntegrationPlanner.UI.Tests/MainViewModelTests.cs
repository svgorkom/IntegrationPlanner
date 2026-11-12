using System.IO;
using System.Runtime.InteropServices;
using IntegrationPlanner.Core.Integrity;
using IntegrationPlanner.Layout;
using IntegrationPlanner.Persistence;
using IntegrationPlanner.UI.Export;
using IntegrationPlanner.UI.Localization;
using IntegrationPlanner.UI.Tests.Fakes;
using IntegrationPlanner.UI.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationPlanner.UI.Tests;

public sealed class MainViewModelTests
{
    private const string PlanPath = @"C:\plans\sample.iplan.json";

    private readonly FakePlanFileStore _store = new() { Result = PlanLoadResult.Success(TestPlans.TwoScenarios(), []) };
    private readonly FakeLayoutEngine _layout = new();
    private readonly FakeFileDialogService _dialogs = new() { PlanToOpen = PlanPath, ExportTarget = @"C:\out\diagram.png" };
    private readonly FakeMessageService _messages = new();
    private readonly FakeSceneExporter _exporter = new();

    [Fact]
    public async Task OpeningAPlan_ListsItsScenarios()
    {
        MainViewModel viewModel = CreateViewModel();

        await viewModel.OpenFileAsync(PlanPath, TestContext.Current.CancellationToken);

        Assert.Equal(["First scenario", "Second scenario"], viewModel.Scenarios.Select(item => item.Name));
    }

    [Fact]
    public async Task OpeningAPlan_SelectsAndRendersTheFirstScenario()
    {
        MainViewModel viewModel = CreateViewModel();

        await viewModel.OpenFileAsync(PlanPath, TestContext.Current.CancellationToken);

        Assert.Equal((TestPlans.First, FakeLayoutEngine.EmptyScene), (viewModel.SelectedScenario?.Id, viewModel.Scene));
    }

    [Fact]
    public async Task OpeningAPlan_ShowsTheFileNameInTheWindowTitle()
    {
        MainViewModel viewModel = CreateViewModel();

        await viewModel.OpenFileAsync(PlanPath, TestContext.Current.CancellationToken);

        Assert.Equal(UiText.Format(UiText.MainWindowTitleWithFile, "sample.iplan.json"), viewModel.WindowTitle);
    }

    [Fact]
    public async Task SelectingAnotherScenario_LaysThatScenarioOut()
    {
        MainViewModel viewModel = CreateViewModel();
        await viewModel.OpenFileAsync(PlanPath, TestContext.Current.CancellationToken);

        viewModel.SelectedScenario = viewModel.Scenarios[1];

        Assert.Equal(TestPlans.Second, _layout.LaidOut[^1]);
    }

    [Fact]
    public async Task OpenCommand_LoadsThePickedFile()
    {
        MainViewModel viewModel = CreateViewModel();

        await viewModel.OpenCommand.ExecuteAsync(null);

        Assert.Equal("sample.iplan.json", viewModel.FileName);
    }

    [Fact]
    public async Task OpenCommand_DoesNothingWhenThePickIsCancelled()
    {
        _dialogs.PlanToOpen = null;
        MainViewModel viewModel = CreateViewModel();

        await viewModel.OpenCommand.ExecuteAsync(null);

        Assert.Null(viewModel.FileName);
    }

    [Fact]
    public async Task OpeningACorruptFile_ShowsTheCorruptFileError()
    {
        _store.Result = PlanLoadResult.Failure(PlanLoadError.Corrupt);
        MainViewModel viewModel = CreateViewModel();

        await viewModel.OpenFileAsync(PlanPath, TestContext.Current.CancellationToken);

        Assert.Equal([UiText.ErrorOpenCorrupt], _messages.Errors);
    }

    [Fact]
    public async Task OpeningANewerFile_ShowsTheUnsupportedVersionError()
    {
        _store.Result = PlanLoadResult.Failure(PlanLoadError.UnsupportedSchemaVersion);
        MainViewModel viewModel = CreateViewModel();

        await viewModel.OpenFileAsync(PlanPath, TestContext.Current.CancellationToken);

        Assert.Equal([UiText.ErrorOpenUnsupportedVersion], _messages.Errors);
    }

    [Fact]
    public async Task OpeningAnUnreadableFile_ShowsTheReadError()
    {
        _store.Failure = new IOException("locked");
        MainViewModel viewModel = CreateViewModel();

        await viewModel.OpenFileAsync(PlanPath, TestContext.Current.CancellationToken);

        Assert.Equal([UiText.ErrorOpenRead], _messages.Errors);
    }

    [Fact]
    public async Task OpeningAFailingFile_KeepsNoScene()
    {
        _store.Result = PlanLoadResult.Failure(PlanLoadError.Corrupt);
        MainViewModel viewModel = CreateViewModel();

        await viewModel.OpenFileAsync(PlanPath, TestContext.Current.CancellationToken);

        Assert.Null(viewModel.Scene);
    }

    [Fact]
    public async Task OpeningAFileWithDroppedReferences_ReportsHowManyWereDropped()
    {
        DroppedReference dropped = new(TestPlans.First, DroppedItemKind.Connector, "connector", "missing");
        _store.Result = PlanLoadResult.Success(TestPlans.TwoScenarios(), [dropped, dropped]);
        MainViewModel viewModel = CreateViewModel();

        await viewModel.OpenFileAsync(PlanPath, TestContext.Current.CancellationToken);

        Assert.Equal([UiText.Format(UiText.InfoDroppedReferences, 2)], _messages.Informations);
    }

    [Fact]
    public void ExportCommands_AreDisabledWithoutAScene()
    {
        MainViewModel viewModel = CreateViewModel();

        bool[] canExecute = [viewModel.ExportPngCommand.CanExecute(null), viewModel.ExportSvgCommand.CanExecute(null), viewModel.CopyToClipboardCommand.CanExecute(null)];

        Assert.Equal([false, false, false], canExecute);
    }

    [Fact]
    public async Task ExportPngCommand_ExportsToThePickedPath()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        await viewModel.ExportPngCommand.ExecuteAsync(null);

        Assert.Equal([(ExportFormat.Png, @"C:\out\diagram.png")], _exporter.Exports);
    }

    [Fact]
    public async Task ExportCommand_SuggestsAFileNameWithoutInvalidCharacters()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        await viewModel.ExportSvgCommand.ExecuteAsync(null);

        Assert.Equal("Plan_ title - First scenario.svg", _dialogs.LastSuggestedFileName);
    }

    [Fact]
    public async Task ExportCommand_DoesNothingWhenThePickIsCancelled()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        _dialogs.ExportTarget = null;

        await viewModel.ExportPngCommand.ExecuteAsync(null);

        Assert.Empty(_exporter.Exports);
    }

    [Fact]
    public async Task FailingExport_ShowsTheExportError()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        _exporter.Failure = new IOException("disk full");

        await viewModel.ExportPngCommand.ExecuteAsync(null);

        Assert.Equal([UiText.ErrorExportFailed], _messages.Errors);
    }

    [Fact]
    public async Task FailingCopy_ShowsTheCopyError()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        _exporter.Failure = new ExternalException("clipboard busy");

        viewModel.CopyToClipboardCommand.Execute(null);

        Assert.Equal([UiText.ErrorCopyFailed], _messages.Errors);
    }

    [Fact]
    public void ZoomIn_EnlargesByTheZoomFactor()
    {
        MainViewModel viewModel = CreateViewModel();

        viewModel.ZoomInCommand.Execute(null);

        Assert.Equal(MainViewModel.ZoomFactor, viewModel.Zoom);
    }

    [Fact]
    public void ZoomIn_StopsAtTheMaximumZoom()
    {
        MainViewModel viewModel = CreateViewModel();
        viewModel.Zoom = MainViewModel.MaximumZoom / 1.1;

        viewModel.ZoomInCommand.Execute(null);

        Assert.Equal(MainViewModel.MaximumZoom, viewModel.Zoom);
    }

    [Fact]
    public void ZoomOut_StopsAtTheMinimumZoom()
    {
        MainViewModel viewModel = CreateViewModel();
        viewModel.Zoom = MainViewModel.MinimumZoom * 1.1;

        viewModel.ZoomOutCommand.Execute(null);

        Assert.Equal(MainViewModel.MinimumZoom, viewModel.Zoom);
    }

    [Fact]
    public void ZoomIn_IsDisabledAtTheMaximumZoom()
    {
        MainViewModel viewModel = CreateViewModel();

        viewModel.Zoom = MainViewModel.MaximumZoom;

        Assert.False(viewModel.ZoomInCommand.CanExecute(null));
    }

    [Fact]
    public void ResetZoom_ReturnsToActualSize()
    {
        MainViewModel viewModel = CreateViewModel();
        viewModel.Zoom = 2;

        viewModel.ResetZoomCommand.Execute(null);

        Assert.Equal(1.0, viewModel.Zoom);
    }

    private MainViewModel CreateViewModel() =>
        new(_store, _layout, _dialogs, _messages, _exporter, LayoutOptions.Default, NullLogger<MainViewModel>.Instance);

    private async Task<MainViewModel> CreateViewModelWithPlanAsync()
    {
        MainViewModel viewModel = CreateViewModel();
        await viewModel.OpenFileAsync(PlanPath, TestContext.Current.CancellationToken);
        return viewModel;
    }
}
