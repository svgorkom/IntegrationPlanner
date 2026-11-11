using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class DeleteConnectorCommand : IPlanCommand
{
    private readonly ScenarioId _scenarioId;
    private readonly ConnectorId _connectorId;

    public DeleteConnectorCommand(ScenarioId scenarioId, ConnectorId connectorId)
    {
        _scenarioId = scenarioId;
        _connectorId = connectorId;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.GetScenario(_scenarioId).Connectors.EnsureContains(_connectorId, connector => connector.Id);
        return plan.WithScenario(_scenarioId, scenario => scenario.WithoutConnector(_connectorId));
    }
}
