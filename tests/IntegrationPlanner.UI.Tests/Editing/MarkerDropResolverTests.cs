using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Editing;

namespace IntegrationPlanner.UI.Tests.Editing;

public sealed class MarkerDropResolverTests
{
    private const double StepWidth = 90;
    private const double LaneSpacing = 70;
    private const string MainKey = "lane/main";
    private const string FeatureKey = "lane/feature";
    private const string PullKey = "connector/pull";

    private static readonly DiagramScene Scene = new(
        new SceneRect(0, 0, 500, 100),
        [
            new SceneLine(MainKey, new ScenePoint(0, 0), new ScenePoint(450, 0), LineStyle.Lane),
            new SceneLine(FeatureKey, new ScenePoint(90, 70), new ScenePoint(450, 70), LineStyle.Lane),
            new SceneLine(PullKey, new ScenePoint(180, 0), new ScenePoint(201, 70), LineStyle.Connector),
        ],
        [],
        []);

    [Fact]
    public void Resolve_PointNearALaneSnapsToItsNearestStep()
    {
        MarkerDrop? drop = MarkerDropResolver.Resolve(Scene, new ScenePoint(310, 60), StepWidth, LaneSpacing);

        Assert.Equal(new MarkerDrop(FeatureKey, new ScenePoint(270, 70)), drop);
    }

    [Fact]
    public void Resolve_PointOnAPullOffTheLanesAttachesToItsMiddle()
    {
        MarkerDrop? drop = MarkerDropResolver.Resolve(Scene, new ScenePoint(191, 36), StepWidth, LaneSpacing);

        Assert.Equal(new MarkerDrop(PullKey, new ScenePoint(190.5, 35)), drop);
    }

    [Fact]
    public void Resolve_LaneWinsWhereAPullStarts()
    {
        MarkerDrop? drop = MarkerDropResolver.Resolve(Scene, new ScenePoint(181, 3), StepWidth, LaneSpacing);

        Assert.Equal(MainKey, drop?.TargetKey);
    }

    [Fact]
    public void Resolve_PointFarFromEveryLaneIsNotADrop()
    {
        MarkerDrop? drop = MarkerDropResolver.Resolve(Scene, new ScenePoint(300, 200), StepWidth, LaneSpacing);

        Assert.Null(drop);
    }
}
