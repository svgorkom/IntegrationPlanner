using CommunityToolkit.Mvvm.ComponentModel;
using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.UI.Editing;
using IntegrationPlanner.UI.Localization;

namespace IntegrationPlanner.UI.ViewModels.Properties;

// Catalogue fields apply to every scenario (D9); lane fields only to the current scenario.
public sealed partial class BranchPropertiesViewModel : PropertiesViewModel
{
    private IReadOnlyList<IdChoice> _forkSources = [];

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private bool _isMainline;

    [ObservableProperty]
    private bool _isCancelled;

    [ObservableProperty]
    private bool _isInScenario;

    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private int _startStep;

    [ObservableProperty]
    private int? _endStep;

    [ObservableProperty]
    private bool _showFork;

    [ObservableProperty]
    private string _forkFromId = string.Empty;

    [ObservableProperty]
    private int? _forkStep;

    public BranchPropertiesViewModel(PlanEditor editor, ScenarioId? scenarioId, BranchId branchId)
        : base(editor, scenarioId)
    {
        BranchId = branchId;
        Refresh();
    }

    public BranchId BranchId { get; }

    public IReadOnlyList<IdChoice> ForkSources => _forkSources;

    protected override bool Load(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Branch? branch = plan.Branches.Find(item => item.Id == BranchId);
        if (branch is null)
        {
            return false;
        }

        SetChoices(
            ref _forkSources,
            [new IdChoice(string.Empty, UiText.ChoiceNone), .. Choices.Branches(plan).Where(choice => choice.Id != BranchId.Value)],
            nameof(ForkSources));
        Name = branch.Name;
        Summary = branch.Summary;
        IsMainline = branch.IsMainline;
        IsCancelled = branch.IsCancelled;
        Lane? lane = CurrentLane();
        IsInScenario = lane is not null;
        Order = lane?.Order ?? 0;
        StartStep = lane?.StartStep ?? 0;
        EndStep = lane?.EndStep;
        ShowFork = lane?.ShowFork ?? false;
        ForkFromId = lane?.ForkFromBranchId?.Value ?? string.Empty;
        ForkStep = lane?.ForkStep;
        return true;
    }

    partial void OnNameChanged(string value) => CommitBranch();

    partial void OnSummaryChanged(string value) => CommitBranch();

    partial void OnIsMainlineChanged(bool value) => CommitBranch();

    partial void OnIsCancelledChanged(bool value) => CommitBranch();

    partial void OnIsInScenarioChanged(bool value)
    {
        if (IsRefreshing || ScenarioId is not ScenarioId scenarioId || CurrentScenario is not Scenario scenario || value == (CurrentLane() is not null))
        {
            return;
        }

        Execute(value ? new SetLaneCommand(scenarioId, PlanItemFactory.NewLane(scenario, BranchId)) : new RemoveLaneCommand(scenarioId, BranchId));
    }

    partial void OnOrderChanged(int value) => CommitLane();

    partial void OnStartStepChanged(int value) => CommitLane();

    partial void OnEndStepChanged(int? value) => CommitLane();

    partial void OnShowForkChanged(bool value) => CommitLane();

    partial void OnForkFromIdChanged(string value) => CommitLane();

    partial void OnForkStepChanged(int? value) => CommitLane();

    private Lane? CurrentLane() => CurrentScenario?.Lanes.Find(lane => lane.BranchId == BranchId);

    private void CommitBranch()
    {
        Branch? current = Editor.Current.Branches.Find(item => item.Id == BranchId);
        if (IsRefreshing || current is null)
        {
            return;
        }

        Branch updated = current with { Name = Name, Summary = Summary, IsMainline = IsMainline, IsCancelled = IsCancelled };
        if (updated != current)
        {
            Execute(new UpdateBranchCommand(updated));
        }
    }

    private void CommitLane()
    {
        if (IsRefreshing || ScenarioId is not ScenarioId scenarioId || CurrentLane() is not Lane current)
        {
            return;
        }

        Lane updated = current with
        {
            Order = Order,
            StartStep = StartStep,
            EndStep = EndStep,
            ShowFork = ShowFork,
            ForkFromBranchId = string.IsNullOrEmpty(ForkFromId) ? null : new BranchId(ForkFromId),
            ForkStep = ForkStep,
        };
        if (updated != current)
        {
            Execute(new SetLaneCommand(scenarioId, updated));
        }
    }
}
