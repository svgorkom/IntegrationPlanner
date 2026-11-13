using System.IO;
using IntegrationPlanner.Jira;
using IntegrationPlanner.Layout;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.Persistence;
using IntegrationPlanner.UI.Editing;
using IntegrationPlanner.UI.Localization;
using IntegrationPlanner.UI.Services;
using IntegrationPlanner.UI.Tests.Fakes;
using IntegrationPlanner.UI.ViewModels;
using IntegrationPlanner.UI.ViewModels.Properties;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationPlanner.UI.Tests;

public sealed class MainViewModelEditingTests
{
    private const string PlanPath = @"C:\plans\sample.iplan.json";
    private const string OtherPath = @"C:\plans\other.iplan.json";

    private readonly FakePlanFileStore _store = new() { Result = PlanLoadResult.Success(TestPlans.WithLanes(), []) };
    private readonly FakeLayoutEngine _layout = new();
    private readonly FakeFileDialogService _dialogs = new() { PlanToOpen = PlanPath, SaveTarget = OtherPath };
    private readonly FakeMessageService _messages = new();
    private readonly FakeSceneExporter _exporter = new();

    [Fact]
    public async Task NewCommand_CreatesAnUntitledPlanWithOneScenario()
    {
        MainViewModel viewModel = CreateViewModel();

        await viewModel.NewCommand.ExecuteAsync(null);

        Assert.Equal((true, null, 1), (viewModel.HasPlan, viewModel.FileName, viewModel.Scenarios.Count));
    }

    [Fact]
    public async Task NewPlan_HasNoUnsavedChanges()
    {
        MainViewModel viewModel = CreateViewModel();

        await viewModel.NewCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task Editing_MarksThePlanAsChangedInTheTitle()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.AddScenarioCommand.Execute(null);

        Assert.Equal(UiText.Format(UiText.MainWindowTitleWithFileUnsaved, "sample.iplan.json"), viewModel.WindowTitle);
    }

    [Fact]
    public async Task UndoingEveryEdit_ClearsTheUnsavedChanges()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        viewModel.AddScenarioCommand.Execute(null);

        viewModel.UndoCommand.Execute(null);

        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task Redo_ReappliesTheUndoneEdit()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        viewModel.AddScenarioCommand.Execute(null);
        viewModel.UndoCommand.Execute(null);

        viewModel.RedoCommand.Execute(null);

        Assert.Equal(2, viewModel.Scenarios.Count);
    }

    [Fact]
    public async Task SaveCommand_WritesToTheOpenedFile()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        viewModel.AddScenarioCommand.Execute(null);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal((PlanPath, false), (Assert.Single(_store.Saves).Path, viewModel.HasUnsavedChanges));
    }

    [Fact]
    public async Task SaveCommand_ForANewPlanAsksForATarget()
    {
        MainViewModel viewModel = CreateViewModel();
        await viewModel.NewCommand.ExecuteAsync(null);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(("other.iplan.json", OtherPath), (viewModel.FileName, Assert.Single(_store.Saves).Path));
    }

    [Fact]
    public async Task SaveAsCommand_SuggestsThePlanTitle()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        await viewModel.SaveAsCommand.ExecuteAsync(null);

        Assert.Equal("Plan.iplan.json", _dialogs.LastSuggestedFileName);
    }

    [Fact]
    public async Task SaveAsCommand_DoesNothingWhenThePickIsCancelled()
    {
        _dialogs.SaveTarget = null;
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        await viewModel.SaveAsCommand.ExecuteAsync(null);

        Assert.Empty(_store.Saves);
    }

    [Fact]
    public async Task SaveFailure_ShowsAnErrorAndKeepsTheChanges()
    {
        _store.SaveFailure = new IOException("disk full");
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        viewModel.AddScenarioCommand.Execute(null);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal((UiText.ErrorSaveFailed, true), (Assert.Single(_messages.Errors), viewModel.HasUnsavedChanges));
    }

    [Fact]
    public async Task OpenCommand_WithUnsavedChangesAsksFirst()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        viewModel.AddScenarioCommand.Execute(null);

        await viewModel.OpenCommand.ExecuteAsync(null);

        Assert.Equal(1, _messages.SaveChangesQuestions);
    }

    [Fact]
    public async Task CancellingTheSaveQuestion_KeepsTheCurrentPlan()
    {
        _messages.SaveChangesAnswer = SaveChangesAnswer.Cancel;
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        viewModel.AddScenarioCommand.Execute(null);

        await viewModel.NewCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Scenarios.Count);
    }

    [Fact]
    public async Task DiscardingChanges_AllowsTheNewPlan()
    {
        _messages.SaveChangesAnswer = SaveChangesAnswer.Discard;
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        viewModel.AddScenarioCommand.Execute(null);

        bool canContinue = await viewModel.ConfirmDiscardChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal((true, 0), (canContinue, _store.Saves.Count));
    }

    [Fact]
    public async Task AnsweringSave_SavesBeforeContinuing()
    {
        _messages.SaveChangesAnswer = SaveChangesAnswer.Save;
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        viewModel.AddScenarioCommand.Execute(null);

        bool canContinue = await viewModel.ConfirmDiscardChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal((true, 1), (canContinue, _store.Saves.Count));
    }

    [Fact]
    public async Task ConfirmDiscardChanges_WithoutChangesDoesNotAsk()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        await viewModel.ConfirmDiscardChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, _messages.SaveChangesQuestions);
    }

    [Fact]
    public async Task AddScenario_SelectsTheNewScenario()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.AddScenarioCommand.Execute(null);

        Assert.Same(viewModel.Scenarios[^1], viewModel.SelectedScenario);
    }

    [Fact]
    public async Task DuplicateScenario_CopiesTheSelectedScenario()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.DuplicateScenarioCommand.Execute(null);

        Assert.Equal(UiText.Format(UiText.DuplicateScenarioName, "First scenario"), viewModel.SelectedScenario?.Name);
    }

    [Fact]
    public async Task DeleteScenario_RemovesTheSelectedScenario()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.DeleteScenarioCommand.Execute(null);

        Assert.Equal((0, null), (viewModel.Scenarios.Count, viewModel.Scene));
    }

    [Fact]
    public async Task AddBranch_ListsAndSelectsTheNewBranch()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.AddBranchCommand.Execute(null);

        Assert.Equal((3, viewModel.Branches[^1]), (viewModel.Branches.Count, viewModel.SelectedBranch));
    }

    [Fact]
    public async Task AddPull_IsDisabledWithoutLanes()
    {
        MainViewModel viewModel = CreateViewModel();
        await viewModel.NewCommand.ExecuteAsync(null);

        bool canExecute = viewModel.AddPullCommand.CanExecute(null);

        Assert.False(canExecute);
    }

    [Fact]
    public async Task AddMerge_SelectsTheNewConnector()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.AddMergeCommand.Execute(null);

        Assert.IsType<ConnectorPropertiesViewModel>(viewModel.SelectedProperties);
    }

    [Fact]
    public async Task AddMarker_SelectsTheNewMarker()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.AddMarkerCommand.Execute(null);

        Assert.IsType<MarkerPropertiesViewModel>(viewModel.SelectedProperties);
    }

    [Fact]
    public async Task SelectingALane_ShowsTheBranchProperties()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.SelectElementCommand.Execute(SceneKeys.Lane(TestPlans.Feature));

        Assert.Equal(TestPlans.Feature, Assert.IsType<BranchPropertiesViewModel>(viewModel.SelectedProperties).BranchId);
    }

    [Fact]
    public async Task SelectingALane_HighlightsTheBranch()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.SelectElementCommand.Execute(SceneKeys.Lane(TestPlans.Feature));

        Assert.Contains(SceneKeys.Lane(TestPlans.Feature), viewModel.SelectedKeys);
    }

    [Fact]
    public async Task SelectingEmptySpace_ShowsThePlanProperties()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        viewModel.SelectElementCommand.Execute(SceneKeys.Lane(TestPlans.Feature));

        viewModel.SelectElementCommand.Execute(null);

        Assert.IsType<PlanPropertiesViewModel>(viewModel.SelectedProperties);
    }

    [Fact]
    public async Task SelectingABranchInTheExplorer_ShowsItsProperties()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.SelectedBranch = viewModel.Branches[0];

        Assert.Equal(TestPlans.Main, Assert.IsType<BranchPropertiesViewModel>(viewModel.SelectedProperties).BranchId);
    }

    [Fact]
    public async Task DeleteSelection_RemovesTheSelectedConnector()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        viewModel.SelectElementCommand.Execute(SceneKeys.Connector(TestPlans.Pull));

        viewModel.DeleteSelectionCommand.Execute(null);

        Assert.IsType<PlanPropertiesViewModel>(viewModel.SelectedProperties);
    }

    [Fact]
    public async Task DeleteSelection_IsDisabledWithoutSelection()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        bool canExecute = viewModel.DeleteSelectionCommand.CanExecute(null);

        Assert.False(canExecute);
    }

    [Fact]
    public async Task UndoingAnAddition_ClearsTheStaleSelection()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        viewModel.AddMarkerCommand.Execute(null);

        viewModel.UndoCommand.Execute(null);

        Assert.IsType<PlanPropertiesViewModel>(viewModel.SelectedProperties);
    }

    [Fact]
    public async Task MoveElement_DraggingAConnectorIsOneUndoableEdit()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.MoveElementCommand.Execute(new SceneMove(SceneKeys.Connector(TestPlans.Pull), LayoutOptions.Default.StepWidth, 0));

        Assert.Equal((true, true), (viewModel.HasUnsavedChanges, viewModel.UndoCommand.CanExecute(null)));
    }

    [Fact]
    public async Task OpeningAPlan_ProvidesDragTargetsForTheScenario()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        Assert.True(viewModel.DragTargets.ContainsKey(SceneKeys.Connector(TestPlans.Pull)));
    }

    [Fact]
    public async Task ConnectElements_AddsAnUndoableConnectorAndSelectsIt()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.ConnectElementsCommand.Execute(new SceneConnect(SceneKeys.Lane(TestPlans.Feature), SceneKeys.Lane(TestPlans.Main), 2 * LayoutOptions.Default.StepWidth, IsPull: false));

        Assert.Equal((true, true), (viewModel.SelectedProperties is ConnectorPropertiesViewModel, viewModel.UndoCommand.CanExecute(null)));
    }

    [Fact]
    public async Task ConnectElements_IgnoresAConnectionToTheSameBranch()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();

        viewModel.ConnectElementsCommand.Execute(new SceneConnect(SceneKeys.Lane(TestPlans.Feature), SceneKeys.Fork(TestPlans.Feature), 0, IsPull: true));

        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task RenameElement_RenamesAMarker()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        string key = SceneKeys.Marker(TestPlans.Release);

        viewModel.RenameElementCommand.Execute(new SceneRename(key, "R2"));

        Assert.Equal("R2", viewModel.RenameText(key));
    }

    [Fact]
    public async Task RenameElement_RenamesABranch()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        string key = SceneKeys.BranchLabel(TestPlans.Feature);

        viewModel.RenameElementCommand.Execute(new SceneRename(key, "release"));

        Assert.Equal("release", viewModel.RenameText(key));
    }

    [Fact]
    public async Task AddBranchFromTicket_SelectsTheNewBranch()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        JiraIssue issue = new("AD-42", "Fix login", "Open", new Uri("https://jira.example.com/browse/AD-42"));

        bool added = viewModel.AddBranchFromTicket(issue, "feature/AD-42", "develop");

        Assert.Equal((true, true), (added, viewModel.SelectedProperties is BranchPropertiesViewModel));
    }

    [Fact]
    public async Task AddBranchFromTicket_IsUndoneInOneStep()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        JiraIssue issue = new("AD-42", "Fix login", "Open", new Uri("https://jira.example.com/browse/AD-42"));
        viewModel.AddBranchFromTicket(issue, "feature/AD-42", "develop");

        viewModel.UndoCommand.Execute(null);

        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task AddBranchFromTicket_RejectsTheSameTicketTwice()
    {
        MainViewModel viewModel = await CreateViewModelWithPlanAsync();
        JiraIssue issue = new("AD-42", "Fix login", "Open", new Uri("https://jira.example.com/browse/AD-42"));
        viewModel.AddBranchFromTicket(issue, "feature/AD-42", "develop");

        bool added = viewModel.AddBranchFromTicket(issue, "feature/AD-42-b", "develop");

        Assert.Equal((false, UiText.Format(UiText.JiraDuplicateTicketFormat, "AD-42")), (added, Assert.Single(_messages.Errors)));
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
