using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Editing;

namespace IntegrationPlanner.UI.Tests.Editing;

public sealed class MarkerReshaperTests
{
    private const double Gap = 2;
    private static readonly ScenePoint Anchor = new(100, 100);
    private static readonly Marker Original = new(
        new MarkerId("m"),
        MarkerKind.Custom,
        "M",
        new BranchStepAnchor(new BranchId("b"), 1),
        MarkerDirection.FromTop,
        ProgressState.Planned);

    [Theory]
    [InlineData(100, 0, 90)]
    [InlineData(0, 100, 0)]
    [InlineData(200, 100, 180)]
    [InlineData(100, 200, 270)]
    [InlineData(200, 0, 135)]
    public void Reshape_TurnsTheArrowToPointFromThePointerToTheAnchor(double pointerX, double pointerY, double expectedAngle)
    {
        Marker? reshaped = MarkerReshaper.Reshape(Original, Anchor, new ScenePoint(pointerX, pointerY), Gap);

        Assert.Equal(expectedAngle, reshaped!.Angle, 6);
    }

    [Fact]
    public void Reshape_StretchesTheArrowToThePointer()
    {
        Marker? reshaped = MarkerReshaper.Reshape(Original, Anchor, new ScenePoint(100, -50), Gap);

        Assert.Equal(150 - Gap, reshaped!.Length, 6);
    }

    [Fact]
    public void Reshape_NeverShrinksBelowTheMinimumLength()
    {
        Marker? reshaped = MarkerReshaper.Reshape(Original, Anchor, new ScenePoint(100, 90), Gap);

        Assert.Equal(Marker.MinimumLength, reshaped!.Length);
    }

    [Fact]
    public void Reshape_PointerOnTheAnchor_ChangesNothing()
    {
        Assert.Null(MarkerReshaper.Reshape(Original, Anchor, Anchor, Gap));
    }

    [Fact]
    public void AnchorOf_RecoversTheAnchorFromTheDrawnArrow()
    {
        // Arrow from the top: tip a gap above the anchor, tail 200 further up.
        ScenePoint[] outline = [new(100, 98), new(70, 68), new(86, 68), new(86, -102), new(114, -102), new(114, 68), new(130, 68)];
        DiagramScene scene = new(new SceneRect(0, 0, 200, 200), [], [], [new SceneArrow("marker/m", [.. outline], ArrowStyle.Planned)]);

        ScenePoint? anchor = MarkerReshaper.AnchorOf(scene, "marker/m", Gap);

        Assert.Equal(100, anchor!.Value.X, 6);
        Assert.Equal(100, anchor.Value.Y, 6);
    }
}
