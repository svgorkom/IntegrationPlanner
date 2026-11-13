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

    private static DiagramScene Scene => new DiagramLayoutEngine(new FixedWidthTextMeasurer(), Options).Layout(TestPlans.WithLanes(), Scenario.Id);

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
    public void TargetsFor_MarkerMovesFreely()
    {
        IReadOnlyDictionary<string, SceneDragTarget> targets = DragPlanner.TargetsFor(Scenario);

        Assert.Equal(DragAxis.Both, targets[SceneKeys.Marker(TestPlans.Release)].Axis);
    }

    [Fact]
    public void CommandFor_LaneDragChangesTheLaneOrderBySnappedRows()
    {
        SceneMove move = new(SceneKeys.Lane(TestPlans.Feature), 0, Options.LaneSpacing * 1.6);

        Plan result = Apply(DragPlanner.CommandFor(Scenario, null, move, Options));

        Assert.Equal(3, result.Scenarios[0].Lanes.Find(lane => lane.BranchId == TestPlans.Feature)!.Order);
    }

    [Fact]
    public void CommandFor_ConnectorDragChangesTheStepBySnappedColumns()
    {
        SceneMove move = new(SceneKeys.Connector(TestPlans.Pull), -Options.StepWidth, 0);

        Plan result = Apply(DragPlanner.CommandFor(Scenario, null, move, Options));

        Assert.Equal(1, result.Scenarios[0].Connectors[0].Step);
    }

    [Fact]
    public void CommandFor_MarkerDragChangesTheAnchorStep()
    {
        SceneMove move = new(SceneKeys.Marker(TestPlans.Release), Options.StepWidth * 2, 0);

        Plan result = Apply(DragPlanner.CommandFor(Scenario, Scene, move, Options));

        Assert.Equal(new BranchStepAnchor(TestPlans.Feature, 5), result.Scenarios[0].Markers[0].Anchor);
    }

    [Fact]
    public void CommandFor_MarkerDragOntoAnotherLaneMovesItToThatBranch()
    {
        SceneMove move = new(SceneKeys.Marker(TestPlans.Release), 10, -Options.LaneSpacing + 8);

        Plan result = Apply(DragPlanner.CommandFor(Scenario, Scene, move, Options));

        Assert.Equal(new BranchStepAnchor(TestPlans.Main, 3), result.Scenarios[0].Markers[0].Anchor);
    }

    [Fact]
    public void CommandFor_MarkerDragOntoAPullAttachesItToThePull()
    {
        SceneLine pull = Scene.Lines.First(line => line.Key == SceneKeys.Connector(TestPlans.Pull));
        ScenePoint anchor = new(3 * Options.StepWidth, Options.LaneSpacing);
        ScenePoint middle = new((pull.From.X + pull.To.X) / 2, (pull.From.Y + pull.To.Y) / 2);
        SceneMove move = new(SceneKeys.Marker(TestPlans.Release), middle.X - anchor.X, middle.Y - anchor.Y);

        Plan result = Apply(DragPlanner.CommandFor(Scenario, Scene, move, Options));

        Assert.Equal(new ConnectorAnchor(TestPlans.Pull), result.Scenarios[0].Markers[0].Anchor);
    }

    [Fact]
    public void CommandFor_MarkerDroppedFarFromEveryLaneDoesNothing()
    {
        SceneMove move = new(SceneKeys.Marker(TestPlans.Release), 0, Options.LaneSpacing * 5);

        IPlanCommand? command = DragPlanner.CommandFor(Scenario, Scene, move, Options);

        Assert.Null(command);
    }

    [Fact]
    public void CommandFor_MoveBelowHalfASnapUnitDoesNothing()
    {
        SceneMove move = new(SceneKeys.Connector(TestPlans.Pull), Options.StepWidth * 0.4, 0);

        IPlanCommand? command = DragPlanner.CommandFor(Scenario, null, move, Options);

        Assert.Null(command);
    }

    [Fact]
    public void CommandFor_IgnoresMovementAcrossTheDragAxis()
    {
        SceneMove move = new(SceneKeys.Connector(TestPlans.Pull), 0, Options.LaneSpacing * 3);

        IPlanCommand? command = DragPlanner.CommandFor(Scenario, null, move, Options);

        Assert.Null(command);
    }

    [Fact]
    public void RetargetCommand_FromEndTakesTheStepWhereItWasDropped()
    {
        SceneRetarget retarget = new(SceneKeys.Connector(TestPlans.Pull), ConnectorEnd.From, SceneKeys.Lane(TestPlans.Main), Options.StepWidth * 4.2);

        Plan result = Apply(DragPlanner.RetargetCommand(Scenario, retarget, Options));

        Assert.Equal(4, result.Scenarios[0].Connectors[0].Step);
    }

    [Fact]
    public void RetargetCommand_ToEndMovesToTheDroppedLane()
    {
        BranchId hotfix = BranchId.New();
        PlanEditor editor = new(TestPlans.WithLanes());
        editor.Execute(PlanItemFactory.AddBranch(editor.Current, Scenario.Id, hotfix));
        SceneRetarget retarget = new(SceneKeys.Connector(TestPlans.Pull), ConnectorEnd.To, SceneKeys.Lane(hotfix), 0);

        editor.Execute(DragPlanner.RetargetCommand(editor.Current.Scenarios[0], retarget, Options)!);

        Assert.Equal(hotfix, editor.Current.Scenarios[0].Connectors[0].ToBranchId);
    }

    [Fact]
    public void RetargetCommand_EndOnTheOtherEndsLaneDoesNothing()
    {
        SceneRetarget retarget = new(SceneKeys.Connector(TestPlans.Pull), ConnectorEnd.To, SceneKeys.Lane(TestPlans.Main), 0);

        IPlanCommand? command = DragPlanner.RetargetCommand(Scenario, retarget, Options);

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
