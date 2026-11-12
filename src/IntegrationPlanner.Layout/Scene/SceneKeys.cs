using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Layout.Scene;

// Stable element keys; they name the model item an element was drawn for.
public static class SceneKeys
{
    public const string Title = "title";

    public static string Lane(BranchId branchId) => $"lane/{branchId}";

    public static string Fork(BranchId branchId) => $"fork/{branchId}";

    public static string BranchLabel(BranchId branchId) => $"branch-label/{branchId}";

    public static string Connector(ConnectorId connectorId) => $"connector/{connectorId}";

    public static string ConnectorLabel(ConnectorId connectorId) => $"connector-label/{connectorId}";

    public static string Marker(MarkerId markerId) => $"marker/{markerId}";
}
