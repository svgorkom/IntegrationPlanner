namespace IntegrationPlanner.Layout.Scene;

public readonly record struct SceneRect(double Left, double Top, double Width, double Height)
{
    private const double Tolerance = 1e-6;

    public double Right => Left + Width;

    public double Bottom => Top + Height;

    public static SceneRect FromPoints(ScenePoint first, ScenePoint second)
    {
        double left = Math.Min(first.X, second.X);
        double top = Math.Min(first.Y, second.Y);
        return new SceneRect(left, top, Math.Abs(first.X - second.X), Math.Abs(first.Y - second.Y));
    }

    public SceneRect Union(SceneRect other)
    {
        double left = Math.Min(Left, other.Left);
        double top = Math.Min(Top, other.Top);
        return new SceneRect(left, top, Math.Max(Right, other.Right) - left, Math.Max(Bottom, other.Bottom) - top);
    }

    public SceneRect Inflate(double amount) => new(Left - amount, Top - amount, Width + (2 * amount), Height + (2 * amount));

    // Touching edges do not count: stacked text lines share an edge by design, give or take rounding.
    public bool Overlaps(SceneRect other) =>
        Left < other.Right - Tolerance && other.Left < Right - Tolerance && Top < other.Bottom - Tolerance && other.Top < Bottom - Tolerance;
}
