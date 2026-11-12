using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class SetLaneCommand : IPlanCommand
{
    private readonly ScenarioId _scenarioId;
    private readonly Lane _lane;

    public SetLaneCommand(ScenarioId scenarioId, Lane lane)
    {
        ArgumentNullException.ThrowIfNull(lane);
        _scenarioId = scenarioId;
        _lane = lane;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Branches.EnsureContains(_lane.BranchId, branch => branch.Id);
        if (_lane.ForkFromBranchId is BranchId forkFromBranchId)
        {
            plan.Branches.EnsureContains(forkFromBranchId, branch => branch.Id);
        }

        return plan.WithScenario(_scenarioId, scenario => scenario.WithLane(_lane));
    }
}
