using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class UpdateConnectorCommand : IPlanCommand
{
    private readonly ScenarioId _scenarioId;
    private readonly Connector _connector;

    public UpdateConnectorCommand(ScenarioId scenarioId, Connector connector)
    {
        ArgumentNullException.ThrowIfNull(connector);
        _scenarioId = scenarioId;
        _connector = connector;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Branches.EnsureContains(_connector.FromBranchId, branch => branch.Id);
        plan.Branches.EnsureContains(_connector.ToBranchId, branch => branch.Id);
        return plan.WithScenario(
            _scenarioId,
            scenario => scenario with { Connectors = scenario.Connectors.ReplaceById(_connector, connector => connector.Id) });
    }
}
