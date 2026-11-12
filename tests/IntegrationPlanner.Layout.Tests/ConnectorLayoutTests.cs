using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout.Tests;

public sealed class ConnectorLayoutTests
{
    private static readonly ConnectorId MergeId = new("merge");

    [Fact]
    public void Connector_StartsOnTheSourceLaneAtItsStep()
    {
        Plan plan = PlanWithMerge(label: null, ProgressState.Done);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(new ScenePoint(TestPlan.X(4), TestPlan.Y(-2)), scene.Line(SceneKeys.Connector(MergeId)).From);
    }

    [Fact]
    public void Connector_EndsOnTheTargetLaneShiftedBySlopeTimesDrop()
    {
        Plan plan = PlanWithMerge(label: null, ProgressState.Done);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(new ScenePoint(TestPlan.X(4) + (TestPlan.Y(2) * TestPlan.Options.Slope), TestPlan.Y(0)), scene.Line(SceneKeys.Connector(MergeId)).To);
    }

    [Theory]
    [InlineData(ProgressState.Done, LineStyle.Connector)]
    [InlineData(ProgressState.Planned, LineStyle.PlannedConnector)]
    public void Connector_IsStyledByItsProgressState(ProgressState state, LineStyle expected)
    {
        Plan plan = PlanWithMerge(label: null, state);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(expected, scene.Line(SceneKeys.Connector(MergeId)).Style);
    }

    [Fact]
    public void ConnectorToBranchWithoutLane_IsNotDrawn()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline()], [TestPlan.Merge("merge", TestPlan.NoLane, TestPlan.Develop, 3)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.DoesNotContain(scene.Lines, line => line.Key == SceneKeys.Connector(MergeId));
    }

    [Fact]
    public void ShortLabel_IsCentredOnTheConnector()
    {
        Plan plan = PlanWithMerge("Merge", ProgressState.Done);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(scene.Line(SceneKeys.Connector(MergeId)).Midpoint(), scene.Text(SceneKeys.ConnectorLabel(MergeId)).Center);
    }

    [Fact]
    public void ShortLabel_IsRotatedAlongTheConnector()
    {
        Plan plan = PlanWithMerge("Merge", ProgressState.Done);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(Math.Atan2(1, TestPlan.Options.Slope) * 180 / Math.PI, scene.Text(SceneKeys.ConnectorLabel(MergeId)).Angle, 6);
    }

    [Fact]
    public void ShortLabel_HasABackgroundThatBreaksTheLine()
    {
        Plan plan = PlanWithMerge("Merge", ProgressState.Done);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.True(scene.Text(SceneKeys.ConnectorLabel(MergeId)).HasBackground);
    }

    [Fact]
    public void LabelLongerThanTheConnector_IsPlacedBesideItUnrotated()
    {
        Plan plan = PlanWithMerge("A label that is much longer than the connector it describes", ProgressState.Done);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.All(scene.Texts(SceneKeys.ConnectorLabel(MergeId)), text => Assert.Equal(0, text.Angle));
    }

    [Fact]
    public void LabelLongerThanTheConnector_DoesNotCrossIt()
    {
        Plan plan = PlanWithMerge("A label that is much longer than the connector it describes", ProgressState.Done);

        DiagramScene scene = TestPlan.Layout(plan);

        SceneRect label = scene.Texts(SceneKeys.ConnectorLabel(MergeId)).BoundsOf();
        Assert.True(label.Left > scene.Line(SceneKeys.Connector(MergeId)).RightmostXBetween(label.Top, label.Bottom));
    }

    [Fact]
    public void LabelsOfConnectorsAtTheSameStep_DoNotOverlap()
    {
        Plan plan = TestPlan.Create(
            [TestPlan.Mainline(), TestPlan.LaneOf(TestPlan.Feature, -2, 0, 4), TestPlan.LaneOf(TestPlan.Sub, -2, 0, 4)],
            [TestPlan.Merge("first", TestPlan.Feature, TestPlan.Develop, 4, "Merge"), TestPlan.Merge("second", TestPlan.Sub, TestPlan.Develop, 4, "Merge")]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Empty(scene.OverlappingLabels());
    }

    private static Plan PlanWithMerge(string? label, ProgressState state) =>
        TestPlan.Create(
            [TestPlan.Mainline(), TestPlan.LaneOf(TestPlan.Feature, -2, 0, 4)],
            [TestPlan.Merge("merge", TestPlan.Feature, TestPlan.Develop, 4, label, state)]);
}
