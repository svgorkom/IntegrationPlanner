using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Tests.Editing;

public sealed class BranchCommandTests
{
    private readonly Plan _plan = TestPlan.Create();

    [Fact]
    public void SetPlanTitle_ChangesTitle()
    {
        SetPlanTitleCommand command = new("Scenario planning");

        Plan result = command.Apply(_plan);

        Assert.Equal("Scenario planning", result.Title);
    }

    [Fact]
    public void AddBranch_AppendsBranchToCatalogue()
    {
        Branch branch = new(new BranchId("ad-4"), "AD-4", "New", IsMainline: false, IsCancelled: false);
        AddBranchCommand command = new(branch);

        Plan result = command.Apply(_plan);

        Assert.Equal(branch, result.Branches[^1]);
    }

    [Fact]
    public void AddBranch_WithIdAlreadyInCatalogue_Throws()
    {
        Branch duplicate = new(TestPlan.FeatureA, "AD-1", "Duplicate", IsMainline: false, IsCancelled: false);
        AddBranchCommand command = new(duplicate);

        Assert.Throws<ArgumentException>(() => command.Apply(_plan));
    }

    [Fact]
    public void UpdateBranch_ReplacesBranchWithSameId()
    {
        Branch cancelled = _plan.Branches[1] with { IsCancelled = true };
        UpdateBranchCommand command = new(cancelled);

        Plan result = command.Apply(_plan);

        Assert.Equal(cancelled, result.Branches[1]);
    }

    [Fact]
    public void UpdateBranch_ToCancelled_KeepsItsConnectors()
    {
        Branch cancelled = _plan.Branches[1] with { IsCancelled = true };
        UpdateBranchCommand command = new(cancelled);

        Plan result = command.Apply(_plan);

        Assert.Equal(_plan.Primary().Connectors, result.Primary().Connectors);
    }

    [Fact]
    public void DeleteBranch_RemovesBranchFromCatalogue()
    {
        DeleteBranchCommand command = new(TestPlan.FeatureA);

        Plan result = command.Apply(_plan);

        Assert.DoesNotContain(result.Branches, branch => branch.Id == TestPlan.FeatureA);
    }

    [Fact]
    public void DeleteBranch_RemovesItsLaneFromEveryScenario()
    {
        DeleteBranchCommand command = new(TestPlan.FeatureA);

        Plan result = command.Apply(_plan);

        Assert.All(result.Scenarios, scenario => Assert.DoesNotContain(scenario.Lanes, lane => lane.BranchId == TestPlan.FeatureA));
    }

    [Fact]
    public void DeleteBranch_RemovesConnectorsFromAndToIt()
    {
        DeleteBranchCommand command = new(TestPlan.FeatureA);

        Plan result = command.Apply(_plan);

        Assert.Equal([TestPlan.PullDevelopIntoB], result.Primary().Connectors.Select(connector => connector.Id));
    }

    [Fact]
    public void DeleteBranch_RemovesMarkersAnchoredToItOrToItsConnectors()
    {
        DeleteBranchCommand command = new(TestPlan.FeatureA);

        Plan result = command.Apply(_plan);

        Assert.Equal([TestPlan.ReleaseOnDevelop], result.Primary().Markers.Select(marker => marker.Id));
    }

    [Fact]
    public void DeleteBranch_ClearsForkOfLanesForkedFromIt()
    {
        DeleteBranchCommand command = new(TestPlan.FeatureA);

        Plan result = command.Apply(_plan);

        Assert.Equal(new Lane(TestPlan.SubOfA, 2, 2, 5, false, null, null), result.Primary().Lanes.Single(lane => lane.BranchId == TestPlan.SubOfA));
    }
}
