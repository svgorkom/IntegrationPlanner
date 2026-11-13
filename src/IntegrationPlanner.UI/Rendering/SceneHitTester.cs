using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.UI.Rendering;

// Finds the scene element under a point. Arrows lie on top, then text, then lines; the title comes last
// so that it never hides an element that overlaps it.
public static class SceneHitTester
{
    public static string? HitTest(DiagramScene scene, ScenePoint point, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentOutOfRangeException.ThrowIfNegative(tolerance);

        SceneArrow? arrow = scene.Arrows.LastOrDefault(candidate => Contains(candidate.Outline, point));
        if (arrow is not null)
        {
            return arrow.Key;
        }

        SceneText? text = scene.Texts.LastOrDefault(candidate => candidate.Key != SceneKeys.Title && Contains(candidate, point, tolerance));
        if (text is not null)
        {
            return text.Key;
        }

        SceneLine? line = scene.Lines
            .Select(candidate => (Line: candidate, Distance: DistanceToSegment(point, candidate.From, candidate.To)))
            .Where(candidate => candidate.Distance <= tolerance)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Line)
            .FirstOrDefault();
        if (line is not null)
        {
            return line.Key;
        }

        return scene.Texts.Any(candidate => candidate.Key == SceneKeys.Title && Contains(candidate, point, tolerance)) ? SceneKeys.Title : null;
    }

    // The lane line nearest to the point, if any lies within the tolerance.
    public static string? HitLane(DiagramScene scene, ScenePoint point, double tolerance) =>
        HitLine(scene, point, tolerance, SceneElementKind.Lane);

    // The pull or merge line nearest to the point, if any lies within the tolerance.
    public static string? HitConnector(DiagramScene scene, ScenePoint point, double tolerance) =>
        HitLine(scene, point, tolerance, SceneElementKind.Connector);

    private static string? HitLine(DiagramScene scene, ScenePoint point, double tolerance, SceneElementKind kind)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentOutOfRangeException.ThrowIfNegative(tolerance);
        return scene.Lines
            .Where(line => SceneKeys.TryParse(line.Key, out SceneElementRef? element) && element.Kind == kind)
            .Select(line => (line.Key, Distance: DistanceToSegment(point, line.From, line.To)))
            .Where(candidate => candidate.Distance <= tolerance)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Key)
            .FirstOrDefault();
    }

    private static bool Contains(SceneText text, ScenePoint point, double tolerance)
    {
        ScenePoint local = SceneGeometry.Rotate(point, text.Center, -text.Angle);
        return Math.Abs(local.X - text.Center.X) <= (text.Size.Width / 2) + tolerance
            && Math.Abs(local.Y - text.Center.Y) <= (text.Size.Height / 2) + tolerance;
    }

    // Even-odd ray casting.
    private static bool Contains(IReadOnlyList<ScenePoint> polygon, ScenePoint point)
    {
        bool inside = false;
        for (int index = 0, previous = polygon.Count - 1; index < polygon.Count; previous = index++)
        {
            ScenePoint a = polygon[index];
            ScenePoint b = polygon[previous];
            bool crosses = (a.Y > point.Y) != (b.Y > point.Y)
                && point.X < ((b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y)) + a.X;
            inside ^= crosses;
        }

        return inside;
    }

    private static double DistanceToSegment(ScenePoint point, ScenePoint from, ScenePoint to)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double lengthSquared = (dx * dx) + (dy * dy);
        double fraction = lengthSquared == 0 ? 0 : Math.Clamp((((point.X - from.X) * dx) + ((point.Y - from.Y) * dy)) / lengthSquared, 0, 1);
        double nearestX = from.X + (fraction * dx);
        double nearestY = from.Y + (fraction * dy);
        return Math.Sqrt(((point.X - nearestX) * (point.X - nearestX)) + ((point.Y - nearestY) * (point.Y - nearestY)));
    }
}
