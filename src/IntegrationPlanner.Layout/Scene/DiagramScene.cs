using System.Collections.Immutable;

namespace IntegrationPlanner.Layout.Scene;

public sealed record DiagramScene(
    SceneRect Bounds,
    ImmutableArray<SceneLine> Lines,
    ImmutableArray<SceneText> Texts,
    ImmutableArray<SceneArrow> Arrows);
