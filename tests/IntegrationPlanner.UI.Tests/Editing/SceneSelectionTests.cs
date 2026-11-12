using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Editing;
using IntegrationPlanner.UI.Tests.Fakes;

namespace IntegrationPlanner.UI.Tests.Editing;

public sealed class SceneSelectionTests
{
    [Fact]
    public void RenameKeyOf_BranchEditsItsLabel()
    {
        string? key = SceneSelection.RenameKeyOf(new BranchSelection(TestPlans.Feature));

        Assert.Equal(SceneKeys.BranchLabel(TestPlans.Feature), key);
    }

    [Fact]
    public void FromKey_ForkSelectsItsBranch()
    {
        EditorSelection? selection = SceneSelection.FromKey(SceneKeys.Fork(TestPlans.Feature));

        Assert.Equal(new BranchSelection(TestPlans.Feature), selection);
    }

    [Fact]
    public void FromKey_ConnectorLabelSelectsItsConnector()
    {
        EditorSelection? selection = SceneSelection.FromKey(SceneKeys.ConnectorLabel(TestPlans.Pull));

        Assert.Equal(new ConnectorSelection(TestPlans.Pull), selection);
    }

    [Fact]
    public void FromKey_MarkerSelectsTheMarker()
    {
        EditorSelection? selection = SceneSelection.FromKey(SceneKeys.Marker(TestPlans.Release));

        Assert.Equal(new MarkerSelection(TestPlans.Release), selection);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("title")]
    [InlineData("unknown/x")]
    public void FromKey_OtherKeysSelectNothing(string? key)
    {
        EditorSelection? selection = SceneSelection.FromKey(key);

        Assert.Null(selection);
    }

    [Fact]
    public void KeysOf_BranchHighlightsLaneForkAndLabel()
    {
        IReadOnlyList<string> keys = SceneSelection.KeysOf(new BranchSelection(TestPlans.Feature));

        Assert.Equal(
            new HashSet<string> { SceneKeys.Lane(TestPlans.Feature), SceneKeys.Fork(TestPlans.Feature), SceneKeys.BranchLabel(TestPlans.Feature) },
            keys.ToHashSet());
    }

    [Fact]
    public void KeysOf_NothingSelectedHighlightsNothing()
    {
        IReadOnlyList<string> keys = SceneSelection.KeysOf(null);

        Assert.Empty(keys);
    }
}
