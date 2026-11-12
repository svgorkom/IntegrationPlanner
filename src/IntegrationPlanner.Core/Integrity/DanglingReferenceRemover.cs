using System.Collections.Immutable;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Integrity;

public static class DanglingReferenceRemover
{
    public static DanglingReferenceRemoval Remove(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        HashSet<BranchId> knownBranchIds = [.. plan.Branches.Select(branch => branch.Id)];
        List<DroppedReference> dropped = [];
        ImmutableList<Scenario> scenarios = plan.Scenarios.ConvertAll(scenario => Clean(scenario, knownBranchIds, dropped));
        return new DanglingReferenceRemoval(plan with { Scenarios = scenarios }, dropped);
    }

    private static Scenario Clean(Scenario scenario, HashSet<BranchId> knownBranchIds, List<DroppedReference> dropped)
    {
        ImmutableList<Lane> lanes = CleanLanes(scenario, knownBranchIds, dropped);
        ImmutableList<Connector> connectors = CleanConnectors(scenario, knownBranchIds, dropped);
        HashSet<ConnectorId> knownConnectorIds = [.. connectors.Select(connector => connector.Id)];
        ImmutableList<Marker> markers = CleanMarkers(scenario, knownBranchIds, knownConnectorIds, dropped);
        return scenario with { Lanes = lanes, Connectors = connectors, Markers = markers };
    }

    private static ImmutableList<Lane> CleanLanes(Scenario scenario, HashSet<BranchId> knownBranchIds, List<DroppedReference> dropped)
    {
        ImmutableList<Lane>.Builder lanes = ImmutableList.CreateBuilder<Lane>();
        foreach (Lane lane in scenario.Lanes)
        {
            if (!knownBranchIds.Contains(lane.BranchId))
            {
                dropped.Add(new DroppedReference(scenario.Id, DroppedItemKind.Lane, lane.BranchId.Value, lane.BranchId.Value));
                continue;
            }

            if (lane.ForkFromBranchId is BranchId forkFrom && !knownBranchIds.Contains(forkFrom))
            {
                dropped.Add(new DroppedReference(scenario.Id, DroppedItemKind.LaneFork, lane.BranchId.Value, forkFrom.Value));
                lanes.Add(lane.WithoutFork());
                continue;
            }

            lanes.Add(lane);
        }

        return lanes.ToImmutable();
    }

    private static ImmutableList<Connector> CleanConnectors(Scenario scenario, HashSet<BranchId> knownBranchIds, List<DroppedReference> dropped)
    {
        ImmutableList<Connector>.Builder connectors = ImmutableList.CreateBuilder<Connector>();
        foreach (Connector connector in scenario.Connectors)
        {
            BranchId? missing = !knownBranchIds.Contains(connector.FromBranchId) ? connector.FromBranchId
                : !knownBranchIds.Contains(connector.ToBranchId) ? connector.ToBranchId
                : null;
            if (missing is BranchId missingId)
            {
                dropped.Add(new DroppedReference(scenario.Id, DroppedItemKind.Connector, connector.Id.Value, missingId.Value));
                continue;
            }

            connectors.Add(connector);
        }

        return connectors.ToImmutable();
    }

    private static ImmutableList<Marker> CleanMarkers(
        Scenario scenario,
        HashSet<BranchId> knownBranchIds,
        HashSet<ConnectorId> knownConnectorIds,
        List<DroppedReference> dropped)
    {
        ImmutableList<Marker>.Builder markers = ImmutableList.CreateBuilder<Marker>();
        foreach (Marker marker in scenario.Markers)
        {
            string? missingId = marker.Anchor switch
            {
                BranchStepAnchor anchor when !knownBranchIds.Contains(anchor.BranchId) => anchor.BranchId.Value,
                ConnectorAnchor anchor when !knownConnectorIds.Contains(anchor.ConnectorId) => anchor.ConnectorId.Value,
                _ => null,
            };
            if (missingId is not null)
            {
                dropped.Add(new DroppedReference(scenario.Id, DroppedItemKind.Marker, marker.Id.Value, missingId));
                continue;
            }

            markers.Add(marker);
        }

        return markers.ToImmutable();
    }
}
