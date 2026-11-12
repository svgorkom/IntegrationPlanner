using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class DeleteMarkerCommand : IPlanCommand
{
    private readonly ScenarioId _scenarioId;
    private readonly MarkerId _markerId;

    public DeleteMarkerCommand(ScenarioId scenarioId, MarkerId markerId)
    {
        _scenarioId = scenarioId;
        _markerId = markerId;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.GetScenario(_scenarioId).Markers.EnsureContains(_markerId, marker => marker.Id);
        return plan.WithScenario(
            _scenarioId,
            scenario => scenario with { Markers = scenario.Markers.RemoveAll(marker => marker.Id == _markerId) });
    }
}
