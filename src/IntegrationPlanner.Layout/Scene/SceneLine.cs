namespace IntegrationPlanner.Layout.Scene;

public sealed record SceneLine(string Key, ScenePoint From, ScenePoint To, LineStyle Style)
{
    public SceneRect Bounds => SceneRect.FromPoints(From, To);
}
