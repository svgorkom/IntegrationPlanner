using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.UI.Editing;

// What the property panel shows; no selection means the plan and current scenario.
public abstract record EditorSelection;

public sealed record BranchSelection(BranchId BranchId) : EditorSelection;

public sealed record ConnectorSelection(ConnectorId ConnectorId) : EditorSelection;

public sealed record MarkerSelection(MarkerId MarkerId) : EditorSelection;

public static class SceneSelection
{
    public static EditorSelection? FromKey(string? key)
    {
        if (!SceneKeys.TryParse(key, out SceneElementRef? element))
        {
            return null;
        }

        return element.Kind switch
        {
            SceneElementKind.Lane or SceneElementKind.Fork or SceneElementKind.BranchLabel => new BranchSelection(new BranchId(element.Id)),
            SceneElementKind.Connector or SceneElementKind.ConnectorLabel => new ConnectorSelection(new ConnectorId(element.Id)),
            SceneElementKind.Marker => new MarkerSelection(new MarkerId(element.Id)),
            _ => null,
        };
    }

    // The element whose text is edited in place to rename the selection.
    public static string? RenameKeyOf(EditorSelection? selection) => selection switch
    {
        BranchSelection branch => SceneKeys.BranchLabel(branch.BranchId),
        ConnectorSelection connector => SceneKeys.ConnectorLabel(connector.ConnectorId),
        MarkerSelection marker => SceneKeys.Marker(marker.MarkerId),
        _ => null,
    };

    public static IReadOnlyList<string> KeysOf(EditorSelection? selection) => selection switch
    {
        BranchSelection branch => [SceneKeys.Lane(branch.BranchId), SceneKeys.Fork(branch.BranchId), SceneKeys.BranchLabel(branch.BranchId)],
        ConnectorSelection connector => [SceneKeys.Connector(connector.ConnectorId), SceneKeys.ConnectorLabel(connector.ConnectorId)],
        MarkerSelection marker => [SceneKeys.Marker(marker.MarkerId)],
        _ => [],
    };
}
