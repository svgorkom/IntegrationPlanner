using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class UpdateScenarioCommand : IPlanCommand
{
    private readonly ScenarioId _scenarioId;
    private readonly string _name;
    private readonly int _stepCount;

    public UpdateScenarioCommand(ScenarioId scenarioId, string name, int stepCount)
    {
        ArgumentNullException.ThrowIfNull(name);
        _scenarioId = scenarioId;
        _name = name;
        _stepCount = stepCount;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan.WithScenario(_scenarioId, scenario => scenario with { Name = _name, StepCount = _stepCount });
    }
}
