using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.UI.Localization;

namespace IntegrationPlanner.UI.Editing;

// Default values for items created from the toolbar; the user adjusts them in the property panel.
public static class PlanItemFactory
{
    public const int DefaultStepCount = 10;
    private const int DefaultConnectorStep = 1;

    public static Plan NewPlan() =>
        Plan.Create(UiText.DefaultPlanTitle) with
        {
            Scenarios = [Scenario.Create(ScenarioId.New(), UiText.Format(UiText.DefaultScenarioName, 1), DefaultStepCount)],
        };

    public static Scenario NewScenario(Plan plan, Scenario? current)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return Scenario.Create(ScenarioId.New(), UiText.Format(UiText.DefaultScenarioName, plan.Scenarios.Count + 1), current?.StepCount ?? DefaultStepCount);
    }

    public static Scenario Duplicate(Scenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        return scenario with { Id = ScenarioId.New(), Name = UiText.Format(UiText.DuplicateScenarioName, scenario.Name) };
    }

    public static Lane NewLane(Scenario scenario, BranchId branchId)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        int order = scenario.Lanes.IsEmpty ? 0 : scenario.Lanes.Max(lane => lane.Order) + 1;
        return new Lane(branchId, order, 0, null, false, null, null);
    }

    public static IPlanCommand AddBranch(Plan plan, ScenarioId? scenarioId, BranchId branchId)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Branch branch = new(branchId, UiText.DefaultBranchName, string.Empty, IsMainline: false, IsCancelled: false);
        List<IPlanCommand> commands = [new AddBranchCommand(branch)];
        if (scenarioId is ScenarioId id)
        {
            commands.Add(new SetLaneCommand(id, NewLane(plan.GetScenario(id), branchId)));
        }

        return new CompositePlanCommand(commands);
    }

    public static Connector? NewConnector(Plan plan, Scenario scenario, ConnectorKind kind, BranchId? selectedBranchId)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(scenario);
        List<BranchId> lanes = OrderedLaneBranches(scenario);
        if (lanes.Count == 0)
        {
            return null;
        }

        BranchId from = selectedBranchId is BranchId selected && lanes.Contains(selected) ? selected : lanes[0];
        BranchId to = MainlineLane(plan, lanes, from) ?? NeighbourLane(lanes, from);
        int step = Math.Clamp(DefaultConnectorStep, 0, Math.Max(scenario.StepCount - 1, 0));
        return new Connector(ConnectorId.New(), kind, from, to, step, null, ProgressState.Planned);
    }

    public static Marker? NewMarker(Scenario scenario, EditorSelection? selection)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        MarkerAnchor? anchor = selection switch
        {
            ConnectorSelection connector when scenario.Connectors.Exists(item => item.Id == connector.ConnectorId) => new ConnectorAnchor(connector.ConnectorId),
            BranchSelection branch when scenario.Lanes.Find(lane => lane.BranchId == branch.BranchId) is { } lane => new BranchStepAnchor(lane.BranchId, lane.StartStep),
            MarkerSelection marker when scenario.Markers.Find(item => item.Id == marker.MarkerId) is { } selected => NextTo(scenario, selected.Anchor),
            _ => OrderedLaneBranches(scenario) is [BranchId first, ..] ? new BranchStepAnchor(first, 0) : null,
        };
        return anchor is null
            ? null
            : new Marker(MarkerId.New(), MarkerKind.Custom, UiText.DefaultMarkerLabel, anchor, MarkerDirection.FromTop, ProgressState.Planned);
    }

    // A new marker next to the selected one stays on its branch, one step further so it does not hide it.
    private static MarkerAnchor NextTo(Scenario scenario, MarkerAnchor anchor) => anchor is BranchStepAnchor branchStep
        ? branchStep with { Step = Math.Min(branchStep.Step + 1, Math.Max(scenario.StepCount - 1, 0)) }
        : anchor;

    private static List<BranchId> OrderedLaneBranches(Scenario scenario) =>
        [.. scenario.Lanes.OrderBy(lane => lane.Order).ThenBy(lane => lane.StartStep).Select(lane => lane.BranchId)];

    private static BranchId? MainlineLane(Plan plan, List<BranchId> lanes, BranchId from)
    {
        HashSet<BranchId> mainlines = [.. plan.Branches.Where(branch => branch.IsMainline).Select(branch => branch.Id)];
        BranchId match = lanes.Find(branchId => branchId != from && mainlines.Contains(branchId));
        return match == default ? null : match;
    }

    private static BranchId NeighbourLane(List<BranchId> lanes, BranchId from)
    {
        int index = lanes.IndexOf(from);
        return index + 1 < lanes.Count ? lanes[index + 1] : lanes[Math.Max(index - 1, 0)];
    }
}
