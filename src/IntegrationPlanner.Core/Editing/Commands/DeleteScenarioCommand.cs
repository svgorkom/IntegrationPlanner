using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class DeleteScenarioCommand : IPlanCommand
{
    private readonly ScenarioId _scenarioId;

    public DeleteScenarioCommand(ScenarioId scenarioId)
    {
        _scenarioId = scenarioId;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Scenarios.EnsureContains(_scenarioId, scenario => scenario.Id);
        return plan with { Scenarios = plan.Scenarios.RemoveAll(scenario => scenario.Id == _scenarioId) };
    }
}
