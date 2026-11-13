using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.UI.Editing;

// Dragging the handle at a marker's tail turns and stretches the arrow around the point it is anchored to.
public static class MarkerReshaper
{
    private const double MinimumPointerDistance = 1e-6;

    // The arrow outline starts at the tip and has its tail corners at indexes 3 and 4; the tip sits one gap before the anchor.
    public static ScenePoint? AnchorOf(DiagramScene scene, string key, double gap)
    {
        ArgumentNullException.ThrowIfNull(scene);
        SceneArrow? arrow = scene.Arrows.FirstOrDefault(candidate => candidate.Key == key);
        if (arrow is null || arrow.Outline.Length < 5)
        {
            return null;
        }

        ScenePoint tip = arrow.Outline[0];
        ScenePoint tail = TailCenter(arrow);
        double length = Math.Sqrt(Math.Pow(tip.X - tail.X, 2) + Math.Pow(tip.Y - tail.Y, 2));
        return length < MinimumPointerDistance
            ? null
            : new ScenePoint(tip.X + (gap * (tip.X - tail.X) / length), tip.Y + (gap * (tip.Y - tail.Y) / length));
    }

    public static ScenePoint TailCenter(SceneArrow arrow)
    {
        ArgumentNullException.ThrowIfNull(arrow);
        return new ScenePoint((arrow.Outline[3].X + arrow.Outline[4].X) / 2, (arrow.Outline[3].Y + arrow.Outline[4].Y) / 2);
    }

    public static Marker? Reshape(Marker marker, ScenePoint anchor, ScenePoint pointer, double gap)
    {
        ArgumentNullException.ThrowIfNull(marker);
        double dx = pointer.X - anchor.X;
        double dy = pointer.Y - anchor.Y;
        double distance = Math.Sqrt((dx * dx) + (dy * dy));
        if (distance < MinimumPointerDistance)
        {
            return null;
        }

        // The tail lies opposite to where the arrow points.
        double angle = MarkerDirection.Normalize(Math.Atan2(-dy, -dx) * 180 / Math.PI);
        return marker with { Angle = angle, Length = Math.Max(distance - gap, Marker.MinimumLength) };
    }
}
