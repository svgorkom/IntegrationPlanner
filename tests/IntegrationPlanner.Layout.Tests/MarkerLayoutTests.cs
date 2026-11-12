using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout.Tests;

public sealed class MarkerLayoutTests
{
    private static readonly string MarkerKey = SceneKeys.Marker(new MarkerId("marker"));
    private static readonly double Gap = TestPlan.Options.MarkerGap;

    public static TheoryData<MarkerDirection, double, double> TipOffsets => new()
    {
        { MarkerDirection.FromTop, 0, -Gap },
        { MarkerDirection.FromBottom, 0, Gap },
        { MarkerDirection.FromLeft, -Gap, 0 },
        { MarkerDirection.FromRight, Gap, 0 },
    };

    [Theory]
    [MemberData(nameof(TipOffsets))]
    public void MarkerOnBranchStep_PointsAtTheLaneAtThatStep(MarkerDirection direction, double offsetX, double offsetY)
    {
        Plan plan = PlanWithMarker(new BranchStepAnchor(TestPlan.Develop, 3), direction);

        DiagramScene scene = TestPlan.Layout(plan);

        ScenePoint tip = scene.Arrow(MarkerKey).Outline[0];
        Assert.Equal(TestPlan.X(3) + offsetX, tip.X, 6);
        Assert.Equal(TestPlan.Y(0) + offsetY, tip.Y, 6);
    }

    [Fact]
    public void MarkerFromTop_LiesAboveItsAnchor()
    {
        Plan plan = PlanWithMarker(new BranchStepAnchor(TestPlan.Develop, 3), MarkerDirection.FromTop);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.True(scene.Arrow(MarkerKey).Bounds.Bottom <= TestPlan.Y(0));
    }

    [Fact]
    public void MarkerFromRight_LiesRightOfItsAnchor()
    {
        Plan plan = PlanWithMarker(new BranchStepAnchor(TestPlan.Develop, 3), MarkerDirection.FromRight);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.True(scene.Arrow(MarkerKey).Bounds.Left >= TestPlan.X(3));
    }

    [Fact]
    public void MarkerOnConnector_PointsAtTheConnectorMidpoint()
    {
        Plan plan = PlanWithMarker(new ConnectorAnchor(new ConnectorId("merge")), MarkerDirection.FromLeft);

        DiagramScene scene = TestPlan.Layout(plan);

        ScenePoint midpoint = scene.Line(SceneKeys.Connector(new ConnectorId("merge"))).Midpoint();
        Assert.Equal(new ScenePoint(midpoint.X - Gap, midpoint.Y), scene.Arrow(MarkerKey).Outline[0]);
    }

    [Fact]
    public void MarkerOnBranchWithoutLane_IsNotDrawn()
    {
        Plan plan = PlanWithMarker(new BranchStepAnchor(TestPlan.NoLane, 3), MarkerDirection.FromTop);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Empty(scene.Arrows);
    }

    [Theory]
    [InlineData(ProgressState.Done, ArrowStyle.Done)]
    [InlineData(ProgressState.Planned, ArrowStyle.Planned)]
    public void Marker_IsStyledByItsProgressState(ProgressState state, ArrowStyle expected)
    {
        Plan plan = PlanWithMarker(new BranchStepAnchor(TestPlan.Develop, 3), MarkerDirection.FromTop, state);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(expected, scene.Arrow(MarkerKey).Style);
    }

    [Fact]
    public void MarkerLabel_SitsInsideTheArrow()
    {
        Plan plan = PlanWithMarker(new BranchStepAnchor(TestPlan.Develop, 3), MarkerDirection.FromTop);

        DiagramScene scene = TestPlan.Layout(plan);

        SceneRect arrow = scene.Arrow(MarkerKey).Bounds;
        SceneRect label = scene.Text(MarkerKey).Bounds;
        Assert.True(label.Left >= arrow.Left && label.Right <= arrow.Right && label.Top >= arrow.Top && label.Bottom <= arrow.Bottom);
    }

    [Theory]
    [InlineData(MarkerDirection.FromTop, -90)]
    [InlineData(MarkerDirection.FromBottom, -90)]
    [InlineData(MarkerDirection.FromLeft, 0)]
    [InlineData(MarkerDirection.FromRight, 0)]
    public void MarkerLabel_RunsAlongTheShaft(MarkerDirection direction, double expectedAngle)
    {
        Plan plan = PlanWithMarker(new BranchStepAnchor(TestPlan.Develop, 3), direction);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(expectedAngle, scene.Text(MarkerKey).Angle);
    }

    private static Plan PlanWithMarker(MarkerAnchor anchor, MarkerDirection direction, ProgressState state = ProgressState.Done) =>
        TestPlan.Create(
            [TestPlan.Mainline(), TestPlan.LaneOf(TestPlan.Feature, -2, 0, 4)],
            [TestPlan.Merge("merge", TestPlan.Feature, TestPlan.Develop, 4)],
            [TestPlan.MarkerOn(anchor, direction, state)]);
}
