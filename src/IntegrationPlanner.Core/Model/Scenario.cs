using System.Collections.Immutable;

namespace IntegrationPlanner.Core.Model;

public sealed record Scenario(
    ScenarioId Id,
    string Name,
    int StepCount,
    ImmutableList<Lane> Lanes,
    ImmutableList<Connector> Connectors,
    ImmutableList<Marker> Markers)
{
    public static Scenario Create(ScenarioId id, string name, int stepCount)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new Scenario(id, name, stepCount, [], [], []);
    }

    public Scenario WithLane(Lane lane)
    {
        ArgumentNullException.ThrowIfNull(lane);
        int index = Lanes.FindIndex(existing => existing.BranchId == lane.BranchId);
        ImmutableList<Lane> lanes = index < 0 ? Lanes.Add(lane) : Lanes.SetItem(index, lane);
        return this with { Lanes = lanes };
    }

    // Removing a branch from a scenario also removes everything that would otherwise point at nothing.
    public Scenario WithoutBranch(BranchId branchId)
    {
        ImmutableList<ConnectorId> removedConnectorIds = Connectors
            .Where(connector => connector.Involves(branchId))
            .Select(connector => connector.Id)
            .ToImmutableList();

        return this with
        {
            Lanes = Lanes
                .RemoveAll(lane => lane.BranchId == branchId)
                .ConvertAll(lane => lane.ForkFromBranchId == branchId ? lane.WithoutFork() : lane),
            Connectors = Connectors.RemoveAll(connector => connector.Involves(branchId)),
            Markers = Markers.RemoveAll(marker =>
                marker.IsAnchoredTo(branchId) || removedConnectorIds.Any(marker.IsAnchoredTo)),
        };
    }

    public Scenario WithoutConnector(ConnectorId connectorId) =>
        this with
        {
            Connectors = Connectors.RemoveAll(connector => connector.Id == connectorId),
            Markers = Markers.RemoveAll(marker => marker.IsAnchoredTo(connectorId)),
        };
}
