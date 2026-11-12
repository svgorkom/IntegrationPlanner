namespace IntegrationPlanner.UI.Editing;

public enum DragAxis
{
    Horizontal,
    Vertical,
}

// What dragging an element moves: along which axis, and which scene elements move with it.
public sealed record SceneDragTarget(DragAxis Axis, IReadOnlyList<string> Keys);

// A completed drag in scene units; the deltas are raw, the view model snaps them to steps or lanes.
public sealed record SceneMove(string Key, double DeltaX, double DeltaY);
