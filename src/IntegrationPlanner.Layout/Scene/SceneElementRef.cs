namespace IntegrationPlanner.Layout.Scene;

public enum SceneElementKind
{
    Title,
    Lane,
    Fork,
    BranchLabel,
    Connector,
    ConnectorLabel,
    Marker,
}

// The model item a scene element was drawn for; Id is empty for the title.
public sealed record SceneElementRef(SceneElementKind Kind, string Id);
