using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

internal static class MarkerAnchorGuard
{
    public static void EnsureExists(Plan plan, ScenarioId scenarioId, MarkerAnchor anchor)
    {
        switch (anchor)
        {
            case BranchStepAnchor branchAnchor:
                plan.Branches.EnsureContains(branchAnchor.BranchId, branch => branch.Id);
                return;
            case ConnectorAnchor connectorAnchor:
                plan.GetScenario(scenarioId).Connectors.EnsureContains(connectorAnchor.ConnectorId, connector => connector.Id);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(anchor), anchor, $"Unsupported {nameof(MarkerAnchor)} type.");
        }
    }
}
