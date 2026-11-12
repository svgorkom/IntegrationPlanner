using System.Collections.Immutable;

namespace IntegrationPlanner.Layout.Scene;

public sealed record SceneArrow(string Key, ImmutableArray<ScenePoint> Outline, ArrowStyle Style)
{
    public SceneRect Bounds => SceneGeometry.BoundsOf(Outline);
}
