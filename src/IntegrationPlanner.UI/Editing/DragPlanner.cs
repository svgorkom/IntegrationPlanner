using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.UI.Editing;

// Canvas drag (D10): lanes move vertically between lane rows, connectors and branch-anchored markers move
// horizontally between steps. Everything else follows the item it is attached to.
public static class DragPlanner
{
    public static IReadOnlyDictionary<string, SceneDragTarget> TargetsFor(Scenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        Dictionary<string, SceneDragTarget> targets = [];
        foreach (Lane lane in scenario.Lanes)
        {
            Add(targets, DragAxis.Vertical, SceneSelection.KeysOf(new BranchSelection(lane.BranchId)), MarkerKeys(scenario, marker => marker.IsAnchoredTo(lane.BranchId)));
        }

        foreach (Connector connector in scenario.Connectors)
        {
            Add(targets, DragAxis.Horizontal, SceneSelection.KeysOf(new ConnectorSelection(connector.Id)), MarkerKeys(scenario, marker => marker.IsAnchoredTo(connector.Id)));
        }

        foreach (Marker marker in scenario.Markers.Where(marker => marker.Anchor is BranchStepAnchor))
        {
            Add(targets, DragAxis.Horizontal, SceneSelection.KeysOf(new MarkerSelection(marker.Id)), []);
        }

        return targets;
    }

    public static IPlanCommand? CommandFor(Scenario scenario, SceneMove move, LayoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(move);
        ArgumentNullException.ThrowIfNull(options);
        int steps = Snap(move.DeltaX, options.StepWidth);
        int lanes = Snap(move.DeltaY, options.LaneSpacing);
        return SceneSelection.FromKey(move.Key) switch
        {
            BranchSelection branch when lanes != 0 && scenario.Lanes.Find(lane => lane.BranchId == branch.BranchId) is { } lane =>
                new SetLaneCommand(scenario.Id, lane with { Order = lane.Order + lanes }),
            ConnectorSelection selected when steps != 0 && scenario.Connectors.Find(connector => connector.Id == selected.ConnectorId) is { } connector =>
                new UpdateConnectorCommand(scenario.Id, connector with { Step = connector.Step + steps }),
            MarkerSelection selected when steps != 0
                && scenario.Markers.Find(marker => marker.Id == selected.MarkerId) is { Anchor: BranchStepAnchor anchor } marker =>
                new UpdateMarkerCommand(scenario.Id, marker with { Anchor = anchor with { Step = anchor.Step + steps } }),
            _ => null,
        };
    }

    public static double SnapDistance(double delta, double unit) => Snap(delta, unit) * unit;

    private static int Snap(double delta, double unit) => unit <= 0 ? 0 : (int)Math.Round(delta / unit, MidpointRounding.AwayFromZero);

    private static IEnumerable<string> MarkerKeys(Scenario scenario, Func<Marker, bool> isAttached) =>
        scenario.Markers.Where(isAttached).Select(marker => SceneKeys.Marker(marker.Id));

    private static void Add(Dictionary<string, SceneDragTarget> targets, DragAxis axis, IReadOnlyList<string> ownKeys, IEnumerable<string> attachedKeys)
    {
        SceneDragTarget target = new(axis, [.. ownKeys, .. attachedKeys]);
        foreach (string key in ownKeys)
        {
            targets[key] = target;
        }
    }
}
