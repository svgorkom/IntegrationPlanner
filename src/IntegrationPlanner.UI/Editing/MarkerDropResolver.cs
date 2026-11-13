using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Rendering;

namespace IntegrationPlanner.UI.Editing;

// Where a dragged or newly placed marker lands, and the point it then points at.
public sealed record MarkerDrop(string TargetKey, ScenePoint Anchor);

// A point near a pull or merge but off every lane attaches to it; otherwise the nearest lane takes it at the nearest
// step. A point on a lane always wins, so a marker can sit at the step where a pull or merge starts.
public static class MarkerDropResolver
{
    public const double ConnectorTolerance = 12;

    public static MarkerDrop? Resolve(DiagramScene scene, ScenePoint point, double stepWidth, double laneSpacing)
    {
        ArgumentNullException.ThrowIfNull(scene);
        string? laneKey = SceneHitTester.HitLane(scene, point, ConnectorTolerance);
        if (laneKey is null)
        {
            string? connectorKey = SceneHitTester.HitConnector(scene, point, ConnectorTolerance);
            if (connectorKey is not null && LineOf(scene, connectorKey) is SceneLine connector)
            {
                return new MarkerDrop(connectorKey, new ScenePoint((connector.From.X + connector.To.X) / 2, (connector.From.Y + connector.To.Y) / 2));
            }

            laneKey = SceneHitTester.HitLane(scene, point, Math.Max(laneSpacing / 2, ConnectorTolerance));
        }

        if (laneKey is null || LineOf(scene, laneKey) is not SceneLine lane)
        {
            return null;
        }

        double x = stepWidth <= 0 ? point.X : Math.Max(0, Math.Round(point.X / stepWidth, MidpointRounding.AwayFromZero)) * stepWidth;
        return new MarkerDrop(laneKey, new ScenePoint(x, lane.From.Y));
    }

    private static SceneLine? LineOf(DiagramScene scene, string key) => scene.Lines.FirstOrDefault(line => line.Key == key);
}
