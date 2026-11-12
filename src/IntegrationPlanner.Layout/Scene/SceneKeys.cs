using System.Diagnostics.CodeAnalysis;
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

    public static bool TryParse(string? key, [NotNullWhen(true)] out SceneElementRef? element)
    {
        element = null;
        if (key == Title)
        {
            element = new SceneElementRef(SceneElementKind.Title, string.Empty);
            return true;
        }

        int separator = key?.IndexOf('/', StringComparison.Ordinal) ?? -1;
        if (key is null || separator <= 0 || separator == key.Length - 1)
        {
            return false;
        }

        SceneElementKind? kind = key[..separator] switch
        {
            "lane" => SceneElementKind.Lane,
            "fork" => SceneElementKind.Fork,
            "branch-label" => SceneElementKind.BranchLabel,
            "connector" => SceneElementKind.Connector,
            "connector-label" => SceneElementKind.ConnectorLabel,
            "marker" => SceneElementKind.Marker,
            _ => null,
        };
        if (kind is null)
        {
            return false;
        }

        element = new SceneElementRef(kind.Value, key[(separator + 1)..]);
        return true;
    }
}
