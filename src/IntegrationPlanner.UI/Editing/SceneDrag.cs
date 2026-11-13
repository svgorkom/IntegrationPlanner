using System.Windows;

namespace IntegrationPlanner.UI.Editing;

public enum DragAxis
{
    Horizontal,
    Vertical,
    Both,
}

public enum ConnectorEnd
{
    From,
    To,
}

// What dragging an element moves: along which axis, and which scene elements move with it.
public sealed record SceneDragTarget(DragAxis Axis, IReadOnlyList<string> Keys);

// A completed drag in scene units; the deltas are raw, the view model snaps them to steps or lanes.
public sealed record SceneMove(string Key, double DeltaX, double DeltaY);

// A lane dragged onto another lane: the connector runs from the first to the second at the scene x where it was dropped.
public sealed record SceneConnect(string FromKey, string ToKey, double X, bool IsPull);

// One end of a pull or merge dragged onto a lane; the From end also takes the step at the scene x where it was dropped.
public sealed record SceneRetarget(string ConnectorKey, ConnectorEnd End, string LaneKey, double X);

public sealed record SceneRename(string Key, string Text);

public enum ReshapePhase
{
    Preview,
    Commit,
    Cancel,
}

// A marker's tail handle dragged to a scene position; Preview shows the result, Commit makes it one undoable edit.
public sealed record SceneReshape(string Key, double X, double Y, ReshapePhase Phase);

// Asks for an element to be renamed in place; the bounds are in canvas coordinates.
public sealed class SceneRenameRequestedEventArgs(string key, Rect bounds) : EventArgs
{
    public string Key { get; } = key;

    public Rect Bounds { get; } = bounds;
}
