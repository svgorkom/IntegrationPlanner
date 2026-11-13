using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.UI.Editing;
using IntegrationPlanner.UI.Localization;
using IntegrationPlanner.UI.Tests.Fakes;

namespace IntegrationPlanner.UI.Tests.Editing;

public sealed class PlanItemFactoryTests
{
    private static readonly Plan Plan = TestPlans.WithLanes();

    private static Scenario Scenario => Plan.Scenarios[0];

    [Fact]
    public void NewPlan_HasOneScenarioWithTheDefaultStepCount()
    {
        Plan plan = PlanItemFactory.NewPlan();

        Assert.Equal(PlanItemFactory.DefaultStepCount, Assert.Single(plan.Scenarios).StepCount);
    }

    [Fact]
    public void NewScenario_IsNumberedAfterTheExistingScenarios()
    {
        Scenario scenario = PlanItemFactory.NewScenario(Plan, Scenario);

        Assert.Equal(UiText.Format(UiText.DefaultScenarioName, 2), scenario.Name);
    }

    [Fact]
    public void Duplicate_CopiesTheLanesUnderANewId()
    {
        Scenario copy = PlanItemFactory.Duplicate(Scenario);

        Assert.Equal((false, Scenario.Lanes), (copy.Id == Scenario.Id, copy.Lanes));
    }

    [Fact]
    public void NewLane_IsPlacedBelowTheExistingLanes()
    {
        Lane lane = PlanItemFactory.NewLane(Scenario, new BranchId("new"));

        Assert.Equal(2, lane.Order);
    }

    [Fact]
    public void AddBranch_AddsTheBranchAndItsLaneInOneUndoStep()
    {
        PlanEditor editor = new(Plan);
        BranchId branchId = new("new");

        editor.Execute(PlanItemFactory.AddBranch(Plan, TestPlans.First, branchId));
        editor.Undo();

        Assert.Same(Plan, editor.Current);
    }

    [Fact]
    public void AddBranch_ShowsTheBranchInTheCurrentScenario()
    {
        PlanEditor editor = new(Plan);
        BranchId branchId = new("new");

        editor.Execute(PlanItemFactory.AddBranch(Plan, TestPlans.First, branchId));

        Assert.True(editor.Current.Scenarios[0].Lanes.Exists(lane => lane.BranchId == branchId));
    }

    [Fact]
    public void NewConnector_RunsFromTheSelectedBranchToTheMainline()
    {
        Connector? connector = PlanItemFactory.NewConnector(Plan, Scenario, ConnectorKind.Merge, TestPlans.Feature);

        Assert.Equal((TestPlans.Feature, TestPlans.Main), (connector?.FromBranchId, connector?.ToBranchId));
    }

    [Fact]
    public void NewConnector_IsNullWithoutLanes()
    {
        Scenario empty = Scenario.Create(new ScenarioId("empty"), "Empty", 4);

        Connector? connector = PlanItemFactory.NewConnector(Plan, empty, ConnectorKind.Pull, null);

        Assert.Null(connector);
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(99, 5)]
    public void NewConnector_StartsAtTheRequestedStepWithinTheScenario(int step, int expected)
    {
        Connector? connector = PlanItemFactory.NewConnector(Plan, Scenario, ConnectorKind.Merge, TestPlans.Feature, step);

        Assert.Equal(expected, connector?.Step);
    }

    [Fact]
    public void NewMarker_AnchorsToTheSelectedConnector()
    {
        Marker? marker = PlanItemFactory.NewMarker(Scenario, new ConnectorSelection(TestPlans.Pull));

        Assert.Equal(new ConnectorAnchor(TestPlans.Pull), marker?.Anchor);
    }

    [Fact]
    public void NewMarker_AnchorsToTheStartOfTheSelectedLane()
    {
        Marker? marker = PlanItemFactory.NewMarker(Scenario, new BranchSelection(TestPlans.Feature));

        Assert.Equal(new BranchStepAnchor(TestPlans.Feature, 1), marker?.Anchor);
    }

    [Fact]
    public void NewMarker_WithAMarkerSelected_StaysOnItsBranchOneStepFurther()
    {
        Marker? marker = PlanItemFactory.NewMarker(Scenario, new MarkerSelection(TestPlans.Release));

        Assert.Equal(new BranchStepAnchor(TestPlans.Feature, 4), marker?.Anchor);
    }

    [Fact]
    public void NewMarker_WithoutSelectionAnchorsToTheFirstLane()
    {
        Marker? marker = PlanItemFactory.NewMarker(Scenario, null);

        Assert.Equal(new BranchStepAnchor(TestPlans.Main, 0), marker?.Anchor);
    }
}
