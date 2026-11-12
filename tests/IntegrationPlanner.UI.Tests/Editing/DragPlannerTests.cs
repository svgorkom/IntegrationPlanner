using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Editing;
using IntegrationPlanner.UI.Tests.Fakes;

namespace IntegrationPlanner.UI.Tests.Editing;

public sealed class DragPlannerTests
{
    private static readonly LayoutOptions Options = LayoutOptions.Default;

    private static Scenario Scenario => TestPlans.WithLanes().Scenarios[0];

    [Fact]
    public void TargetsFor_LaneMovesVertically()
    {
        IReadOnlyDictionary<string, SceneDragTarget> targets = DragPlanner.TargetsFor(Scenario);

        Assert.Equal(DragAxis.Vertical, targets[SceneKeys.Lane(TestPlans.Feature)].Axis);
    }

    [Fact]
    public void TargetsFor_LaneDragCarriesItsBranchMarkers()
    {
        IReadOnlyDictionary<string, SceneDragTarget> targets = DragPlanner.TargetsFor(Scenario);

        Assert.Contains(SceneKeys.Marker(TestPlans.Release), targets[SceneKeys.Lane(TestPlans.Feature)].Keys);
    }

    [Fact]
    public void TargetsFor_ConnectorMovesHorizontally()
    {
        IReadOnlyDictionary<string, SceneDragTarget> targets = DragPlanner.TargetsFor(Scenario);

        Assert.Equal(DragAxis.Horizontal, targets[SceneKeys.Connector(TestPlans.Pull)].Axis);
    }

    [Fact]
    public void TargetsFor_BranchAnchoredMarkerMovesHorizontally()
    {
        IReadOnlyDictionary<string, SceneDragTarget> targets = DragPlanner.TargetsFor(Scenario);

        Assert.Equal(DragAxis.Horizontal, targets[SceneKeys.Marker(TestPlans.Release)].Axis);
    }

    [Fact]
    public void CommandFor_LaneDragChangesTheLaneOrderBySnappedRows()
    {
        SceneMove move = new(SceneKeys.Lane(TestPlans.Feature), 0, Options.LaneSpacing * 1.6);

        Plan result = Apply(DragPlanner.CommandFor(Scenario, move, Options));

        Assert.Equal(3, result.Scenarios[0].Lanes.Find(lane => lane.BranchId == TestPlans.Feature)!.Order);
    }

    [Fact]
    public void CommandFor_ConnectorDragChangesTheStepBySnappedColumns()
    {
        SceneMove move = new(SceneKeys.Connector(TestPlans.Pull), -Options.StepWidth, 0);

        Plan result = Apply(DragPlanner.CommandFor(Scenario, move, Options));

        Assert.Equal(1, result.Scenarios[0].Connectors[0].Step);
    }

    [Fact]
    public void CommandFor_MarkerDragChangesTheAnchorStep()
    {
        SceneMove move = new(SceneKeys.Marker(TestPlans.Release), Options.StepWidth * 2, 0);

        Plan result = Apply(DragPlanner.CommandFor(Scenario, move, Options));

        Assert.Equal(new BranchStepAnchor(TestPlans.Feature, 5), result.Scenarios[0].Markers[0].Anchor);
    }

    [Fact]
    public void CommandFor_MoveBelowHalfASnapUnitDoesNothing()
    {
        SceneMove move = new(SceneKeys.Connector(TestPlans.Pull), Options.StepWidth * 0.4, 0);

        IPlanCommand? command = DragPlanner.CommandFor(Scenario, move, Options);

        Assert.Null(command);
    }

    [Fact]
    public void CommandFor_IgnoresMovementAcrossTheDragAxis()
    {
        SceneMove move = new(SceneKeys.Connector(TestPlans.Pull), 0, Options.LaneSpacing * 3);

        IPlanCommand? command = DragPlanner.CommandFor(Scenario, move, Options);

        Assert.Null(command);
    }

    [Theory]
    [InlineData(44, 0)]
    [InlineData(45, 90)]
    [InlineData(-45, -90)]
    [InlineData(200, 180)]
    public void SnapDistance_RoundsToWholeUnits(double delta, double expected)
    {
        double distance = DragPlanner.SnapDistance(delta, 90);

        Assert.Equal(expected, distance);
    }

    private static Plan Apply(IPlanCommand? command)
    {
        Assert.NotNull(command);
        PlanEditor editor = new(TestPlans.WithLanes());
        editor.Execute(command);
        return editor.Current;
    }
}
