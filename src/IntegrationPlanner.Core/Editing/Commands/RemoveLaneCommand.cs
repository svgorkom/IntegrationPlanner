using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class RemoveLaneCommand : IPlanCommand
{
    private readonly ScenarioId _scenarioId;
    private readonly BranchId _branchId;

    public RemoveLaneCommand(ScenarioId scenarioId, BranchId branchId)
    {
        _scenarioId = scenarioId;
        _branchId = branchId;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.GetScenario(_scenarioId).Lanes.EnsureContains(_branchId, lane => lane.BranchId);
        return plan.WithScenario(_scenarioId, scenario => scenario.WithoutBranch(_branchId));
    }
}
