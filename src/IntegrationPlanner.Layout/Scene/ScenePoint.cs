namespace IntegrationPlanner.Layout.Scene;

public readonly record struct ScenePoint(double X, double Y)
{
    public ScenePoint Offset(double dx, double dy) => new(X + dx, Y + dy);
}
