namespace IntegrationPlanner.Layout.Scene;

public static class SceneGeometry
{
    public static SceneRect RotatedBounds(ScenePoint center, SceneSize size, double angleDegrees)
    {
        double radians = angleDegrees * Math.PI / 180.0;
        double cos = Math.Abs(Math.Cos(radians));
        double sin = Math.Abs(Math.Sin(radians));
        double width = (size.Width * cos) + (size.Height * sin);
        double height = (size.Width * sin) + (size.Height * cos);
        return new SceneRect(center.X - (width / 2), center.Y - (height / 2), width, height);
    }

    public static SceneRect BoundsOf(IReadOnlyList<ScenePoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            throw new ArgumentException("At least one point is required.", nameof(points));
        }

        double left = points.Min(point => point.X);
        double top = points.Min(point => point.Y);
        return new SceneRect(left, top, points.Max(point => point.X) - left, points.Max(point => point.Y) - top);
    }

    public static ScenePoint Rotate(ScenePoint point, ScenePoint origin, double angleDegrees)
    {
        double radians = angleDegrees * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        double dx = point.X - origin.X;
        double dy = point.Y - origin.Y;
        return new ScenePoint(origin.X + (dx * cos) - (dy * sin), origin.Y + (dx * sin) + (dy * cos));
    }
}
