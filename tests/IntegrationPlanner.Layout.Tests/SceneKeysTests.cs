using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout.Tests;

public sealed class SceneKeysTests
{
    public static TheoryData<string, SceneElementKind, string> Keys => new()
    {
        { SceneKeys.Lane(new BranchId("develop")), SceneElementKind.Lane, "develop" },
        { SceneKeys.Fork(new BranchId("ad-1")), SceneElementKind.Fork, "ad-1" },
        { SceneKeys.BranchLabel(new BranchId("ad-2")), SceneElementKind.BranchLabel, "ad-2" },
        { SceneKeys.Connector(new ConnectorId("merge")), SceneElementKind.Connector, "merge" },
        { SceneKeys.ConnectorLabel(new ConnectorId("pull")), SceneElementKind.ConnectorLabel, "pull" },
        { SceneKeys.Marker(new MarkerId("release")), SceneElementKind.Marker, "release" },
        { SceneKeys.Title, SceneElementKind.Title, string.Empty },
    };

    [Theory]
    [MemberData(nameof(Keys))]
    public void TryParse_ReturnsTheElementAKeyWasMadeFor(string key, SceneElementKind kind, string id)
    {
        bool parsed = SceneKeys.TryParse(key, out SceneElementRef? element);

        Assert.True(parsed);
        Assert.Equal(new SceneElementRef(kind, id), element);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown/id")]
    [InlineData("lane/")]
    [InlineData("/id")]
    public void TryParse_RejectsKeysThatNameNoElement(string? key)
    {
        bool parsed = SceneKeys.TryParse(key, out SceneElementRef? element);

        Assert.False(parsed);
        Assert.Null(element);
    }
}
