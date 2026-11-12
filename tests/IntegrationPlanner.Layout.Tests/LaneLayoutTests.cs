using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout.Tests;

public sealed class LaneLayoutTests
{
    [Fact]
    public void Mainline_IsDrawnAtYZero()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline()]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(0, scene.Line(SceneKeys.Lane(TestPlan.Develop)).From.Y);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(3)]
    public void Lane_IsStackedByItsOrder(int order)
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline(), TestPlan.LaneOf(TestPlan.Feature, order, 1, 5)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(TestPlan.Y(order), scene.Line(SceneKeys.Lane(TestPlan.Feature)).From.Y);
    }

    [Fact]
    public void Lane_SpansFromItsStartStepToItsEndStep()
    {
        Plan plan = TestPlan.Create([TestPlan.LaneOf(TestPlan.Feature, 1, 2, 5)]);

        DiagramScene scene = TestPlan.Layout(plan);

        SceneLine lane = scene.Line(SceneKeys.Lane(TestPlan.Feature));
        Assert.Equal((TestPlan.X(2), TestPlan.X(5)), (lane.From.X, lane.To.X));
    }

    [Fact]
    public void LaneWithoutEndStep_RunsToTheLastStep()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline()]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(TestPlan.X(TestPlan.StepCount - 1), scene.Line(SceneKeys.Lane(TestPlan.Develop)).To.X);
    }

    [Fact]
    public void LaneOfBranchMissingFromCatalogue_IsNotDrawn()
    {
        Plan plan = TestPlan.Create([TestPlan.LaneOf(new BranchId("unknown"), 1, 0, 3)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Empty(scene.Lines);
    }

    [Fact]
    public void ShownFork_SlantsFromTheParentLaneAtTheForkStep()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline(), TestPlan.ForkedLane(TestPlan.Sub, 2, TestPlan.Develop, 3, showFork: true)]);

        DiagramScene scene = TestPlan.Layout(plan);

        SceneLine fork = scene.Line(SceneKeys.Fork(TestPlan.Sub));
        Assert.Equal(new ScenePoint(TestPlan.X(3), TestPlan.Y(0)), fork.From);
        Assert.Equal(new ScenePoint(TestPlan.X(3) + (TestPlan.Y(2) * TestPlan.Options.Slope), TestPlan.Y(2)), fork.To);
    }

    [Fact]
    public void ForkedLane_StartsWhereItsForkEnds()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline(), TestPlan.ForkedLane(TestPlan.Sub, 2, TestPlan.Develop, 3, showFork: true)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(scene.Line(SceneKeys.Fork(TestPlan.Sub)).To, scene.Line(SceneKeys.Lane(TestPlan.Sub)).From);
    }

    [Fact]
    public void HiddenFork_IsNotDrawn()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline(), TestPlan.ForkedLane(TestPlan.Sub, 2, TestPlan.Develop, 3, showFork: false)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.DoesNotContain(scene.Lines, line => line.Style == LineStyle.Fork);
    }

    [Fact]
    public void CancelledBranch_KeepsItsLaneLine()
    {
        Plan plan = TestPlan.Create([TestPlan.LaneOf(TestPlan.Cancelled, 1, 0, 4)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(LineStyle.Lane, scene.Line(SceneKeys.Lane(TestPlan.Cancelled)).Style);
    }
}
