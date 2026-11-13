using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.UI.Editing;

// Canvas drag (D10, D19): lanes move vertically between lane rows, connectors move horizontally between steps and
// markers move freely onto any lane step or pull/merge. Everything else follows the item it is attached to.
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

        foreach (Marker marker in scenario.Markers)
        {
            Add(targets, DragAxis.Both, SceneSelection.KeysOf(new MarkerSelection(marker.Id)), []);
        }

        return targets;
    }

    public static IPlanCommand? CommandFor(Scenario scenario, DiagramScene? scene, SceneMove move, LayoutOptions options)
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
            MarkerSelection selected when scene is not null && scenario.Markers.Find(marker => marker.Id == selected.MarkerId) is { } marker =>
                MarkerMoveCommand(scenario, scene, marker, move, options),
            _ => null,
        };
    }

    // The marker's anchor point moves with the pointer and then snaps to wherever it was dropped.
    public static MarkerDrop? MarkerDropFor(DiagramScene scene, string markerKey, double deltaX, double deltaY, LayoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(options);
        return MarkerReshaper.AnchorOf(scene, markerKey, options.MarkerGap) is ScenePoint anchor
            ? MarkerDropResolver.Resolve(scene, new ScenePoint(anchor.X + deltaX, anchor.Y + deltaY), options.StepWidth, options.LaneSpacing)
            : null;
    }

    public static MarkerAnchor? AnchorFor(Scenario scenario, MarkerDrop drop, double stepWidth)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(drop);
        return SceneSelection.FromKey(drop.TargetKey) switch
        {
            BranchSelection branch when scenario.Lanes.Exists(lane => lane.BranchId == branch.BranchId) =>
                new BranchStepAnchor(branch.BranchId, StepAt(scenario, drop.Anchor.X, stepWidth)),
            ConnectorSelection connector when scenario.Connectors.Exists(item => item.Id == connector.ConnectorId) =>
                new ConnectorAnchor(connector.ConnectorId),
            _ => null,
        };
    }

    public static IPlanCommand? RetargetCommand(Scenario scenario, SceneRetarget retarget, LayoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(retarget);
        ArgumentNullException.ThrowIfNull(options);
        if (SceneSelection.FromKey(retarget.ConnectorKey) is not ConnectorSelection selected
            || SceneSelection.FromKey(retarget.LaneKey) is not BranchSelection lane
            || !scenario.Lanes.Exists(item => item.BranchId == lane.BranchId)
            || scenario.Connectors.Find(item => item.Id == selected.ConnectorId) is not Connector connector)
        {
            return null;
        }

        Connector updated = retarget.End == ConnectorEnd.From
            ? connector with { FromBranchId = lane.BranchId, Step = StepAt(scenario, retarget.X, options.StepWidth) }
            : connector with { ToBranchId = lane.BranchId };
        return updated == connector || updated.FromBranchId == updated.ToBranchId ? null : new UpdateConnectorCommand(scenario.Id, updated);
    }

    public static int StepAt(Scenario scenario, double x, double stepWidth)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        return Math.Clamp(Snap(x, stepWidth), 0, Math.Max(0, scenario.StepCount - 1));
    }

    public static double SnapDistance(double delta, double unit) => Snap(delta, unit) * unit;

    private static IPlanCommand? MarkerMoveCommand(Scenario scenario, DiagramScene scene, Marker marker, SceneMove move, LayoutOptions options)
    {
        MarkerDrop? drop = MarkerDropFor(scene, SceneKeys.Marker(marker.Id), move.DeltaX, move.DeltaY, options);
        MarkerAnchor? anchor = drop is null ? null : AnchorFor(scenario, drop, options.StepWidth);
        return anchor is null || anchor == marker.Anchor ? null : new UpdateMarkerCommand(scenario.Id, marker with { Anchor = anchor });
    }

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
