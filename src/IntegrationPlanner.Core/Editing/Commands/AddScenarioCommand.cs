using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class AddScenarioCommand : IPlanCommand
{
    private readonly Scenario _scenario;

    public AddScenarioCommand(Scenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        _scenario = scenario;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan with { Scenarios = plan.Scenarios.AddWithUniqueId(_scenario, scenario => scenario.Id) };
    }
}
