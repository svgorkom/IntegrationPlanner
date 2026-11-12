using System.Collections.Immutable;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Rendering;

namespace IntegrationPlanner.UI.Tests.Rendering;

public sealed class SceneHitTesterTests
{
    private const double Tolerance = 5;
    private const string LaneKey = "lane/main";
    private const string ArrowKey = "marker/release";
    private const string LabelKey = "connector-label/pull";

    private static readonly DiagramScene Scene = new(
        new SceneRect(0, 0, 400, 200),
        [new SceneLine(LaneKey, new ScenePoint(0, 100), new ScenePoint(400, 100), LineStyle.Lane)],
        [
            new SceneText(SceneKeys.Title, "Plan", new ScenePoint(200, 20), new SceneSize(400, 40), 0, 20, TextStyle.Title, false),
            new SceneText(LabelKey, "label", new ScenePoint(300, 150), new SceneSize(20, 60), 90, 10, TextStyle.ConnectorLabel, false),
        ],
        [new SceneArrow(ArrowKey, ImmutableArray.Create(new ScenePoint(90, 80), new ScenePoint(110, 80), new ScenePoint(100, 105)), ArrowStyle.Done)]);

    [Fact]
    public void HitTest_PointNearALineSelectsTheLine()
    {
        string? key = SceneHitTester.HitTest(Scene, new ScenePoint(250, 104), Tolerance);

        Assert.Equal(LaneKey, key);
    }

    [Fact]
    public void HitTest_ArrowLiesOnTopOfTheLine()
    {
        string? key = SceneHitTester.HitTest(Scene, new ScenePoint(100, 98), Tolerance);

        Assert.Equal(ArrowKey, key);
    }

    [Fact]
    public void HitTest_RotatedTextUsesItsRotatedBounds()
    {
        string? key = SceneHitTester.HitTest(Scene, new ScenePoint(325, 150), Tolerance);

        Assert.Equal(LabelKey, key);
    }

    [Fact]
    public void HitTest_TitleIsFoundWhenNothingElseIsHit()
    {
        string? key = SceneHitTester.HitTest(Scene, new ScenePoint(200, 20), Tolerance);

        Assert.Equal(SceneKeys.Title, key);
    }

    [Fact]
    public void HitTest_EmptySpaceHitsNothing()
    {
        string? key = SceneHitTester.HitTest(Scene, new ScenePoint(50, 180), Tolerance);

        Assert.Null(key);
    }
}
