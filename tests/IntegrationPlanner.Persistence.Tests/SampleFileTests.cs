using IntegrationPlanner.Core.Model;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationPlanner.Persistence.Tests;

public sealed class SampleFileTests
{
    private static readonly string SamplePath = Path.Combine(AppContext.BaseDirectory, "Samples", "scenario3.iplan.json");

    private readonly PlanFileStore _store = new(NullLogger<PlanFileStore>.Instance);

    [Fact]
    public async Task Scenario3Sample_Loads()
    {
        PlanLoadResult result = await _store.LoadAsync(SamplePath, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Scenario3Sample_HasNoDanglingReferences()
    {
        PlanLoadResult result = await _store.LoadAsync(SamplePath, TestContext.Current.CancellationToken);

        Assert.Empty(result.DroppedReferences);
    }

    [Fact]
    public async Task Scenario3Sample_HasALaneForEveryCatalogueBranch()
    {
        PlanLoadResult result = await _store.LoadAsync(SamplePath, TestContext.Current.CancellationToken);

        Assert.Equivalent(result.Plan!.Branches.Select(branch => branch.Id), Assert.Single(result.Plan.Scenarios).Lanes.Select(lane => lane.BranchId));
    }

    [Fact]
    public async Task Scenario3Sample_AnchorsTestingMarkerToFinalMerge()
    {
        PlanLoadResult result = await _store.LoadAsync(SamplePath, TestContext.Current.CancellationToken);

        Marker marker = Assert.Single(Assert.Single(result.Plan!.Scenarios).Markers, candidate => candidate.Label == "Lots of testing!");
        Assert.Equal(new ConnectorAnchor(new ConnectorId("merge-15777-into-develop")), marker.Anchor);
    }
}
