using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Tests.Editing;

public sealed class ScenarioCommandTests
{
    private readonly Plan _plan = TestPlan.Create();

    [Fact]
    public void AddScenario_AppendsScenario()
    {
        Scenario scenario = Scenario.Create(new ScenarioId("third"), "Scenario 3", 18);
        AddScenarioCommand command = new(scenario);

        Plan result = command.Apply(_plan);

        Assert.Same(scenario, result.Scenarios[^1]);
    }

    [Fact]
    public void AddScenario_SharesTheBranchCatalogue()
    {
        Scenario scenario = Scenario.Create(new ScenarioId("third"), "Scenario 3", 18);
        AddScenarioCommand command = new(scenario);

        Plan result = command.Apply(_plan);

        Assert.Same(_plan.Branches, result.Branches);
    }

    [Fact]
    public void UpdateScenario_ChangesNameAndStepCount()
    {
        UpdateScenarioCommand command = new(TestPlan.MainScenario, "Renamed", 20);

        Plan result = command.Apply(_plan);

        Assert.Equal(("Renamed", 20), (result.Primary().Name, result.Primary().StepCount));
    }

    [Fact]
    public void DeleteScenario_RemovesOnlyThatScenario()
    {
        DeleteScenarioCommand command = new(TestPlan.MainScenario);

        Plan result = command.Apply(_plan);

        Assert.Equal([TestPlan.OtherScenario], result.Scenarios.Select(scenario => scenario.Id));
    }

    [Fact]
    public void SetLane_ForBranchWithoutLane_AddsLane()
    {
        Lane lane = new(TestPlan.FeatureB, -1, 1, null, true, TestPlan.Develop, 1);
        SetLaneCommand command = new(TestPlan.OtherScenario, lane);

        Plan result = command.Apply(_plan);

        Assert.Equal(lane, result.Secondary().Lanes[^1]);
    }

    [Fact]
    public void SetLane_ForBranchWithLane_ReplacesLane()
    {
        Lane lane = new(TestPlan.FeatureA, 3, 1, 4, false, null, null);
        SetLaneCommand command = new(TestPlan.OtherScenario, lane);

        Plan result = command.Apply(_plan);

        Assert.Equal([TestPlan.Develop, TestPlan.FeatureA], result.Secondary().Lanes.Select(existing => existing.BranchId));
        Assert.Equal(lane, result.Secondary().Lanes[1]);
    }

    [Fact]
    public void RemoveLane_RemovesConnectorsOfThatBranchInThatScenario()
    {
        RemoveLaneCommand command = new(TestPlan.OtherScenario, TestPlan.FeatureA);

        Plan result = command.Apply(_plan);

        Assert.Empty(result.Secondary().Connectors);
    }

    [Fact]
    public void RemoveLane_LeavesOtherScenariosUntouched()
    {
        RemoveLaneCommand command = new(TestPlan.OtherScenario, TestPlan.FeatureA);

        Plan result = command.Apply(_plan);

        Assert.Same(_plan.Primary(), result.Primary());
    }

    [Fact]
    public void RemoveLane_KeepsBranchInCatalogue()
    {
        RemoveLaneCommand command = new(TestPlan.OtherScenario, TestPlan.FeatureA);

        Plan result = command.Apply(_plan);

        Assert.Same(_plan.Branches, result.Branches);
    }
}
