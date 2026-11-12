using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class UpdateMarkerCommand : IPlanCommand
{
    private readonly ScenarioId _scenarioId;
    private readonly Marker _marker;

    public UpdateMarkerCommand(ScenarioId scenarioId, Marker marker)
    {
        ArgumentNullException.ThrowIfNull(marker);
        _scenarioId = scenarioId;
        _marker = marker;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        MarkerAnchorGuard.EnsureExists(plan, _scenarioId, _marker.Anchor);
        return plan.WithScenario(
            _scenarioId,
            scenario => scenario with { Markers = scenario.Markers.ReplaceById(_marker, marker => marker.Id) });
    }
}
