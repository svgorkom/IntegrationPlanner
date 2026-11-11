using IntegrationPlanner.Core.Integrity;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Tests.Integrity;

public sealed class DanglingReferenceRemoverTests
{
    private static readonly BranchId Ghost = new("ghost");
    private static readonly ConnectorId GhostConnector = new("ghost-connector");

    [Fact]
    public void Remove_OnConsistentPlan_DropsNothing()
    {
        Plan plan = TestPlan.Create();

        DanglingReferenceRemoval removal = DanglingReferenceRemover.Remove(plan);

        Assert.Empty(removal.DroppedReferences);
    }

    [Fact]
    public void Remove_LaneOfUnknownBranch_DropsLaneAndReportsIt()
    {
        Plan plan = WithMainScenario(scenario => scenario with { Lanes = scenario.Lanes.Add(new Lane(Ghost, 5, 0, null, false, null, null)) });

        DanglingReferenceRemoval removal = DanglingReferenceRemover.Remove(plan);

        Assert.DoesNotContain(removal.Plan.Primary().Lanes, lane => lane.BranchId == Ghost);
        Assert.Equal([new DroppedReference(TestPlan.MainScenario, DroppedItemKind.Lane, "ghost", "ghost")], removal.DroppedReferences);
    }

    [Fact]
    public void Remove_LaneForkedFromUnknownBranch_KeepsLaneWithoutFork()
    {
        Plan plan = WithMainScenario(scenario => scenario.WithLane(new Lane(TestPlan.FeatureB, -1, 2, null, true, Ghost, 2)));

        DanglingReferenceRemoval removal = DanglingReferenceRemover.Remove(plan);

        Assert.Equal(new Lane(TestPlan.FeatureB, -1, 2, null, false, null, null), removal.Plan.Primary().Lanes.Single(lane => lane.BranchId == TestPlan.FeatureB));
        Assert.Equal([new DroppedReference(TestPlan.MainScenario, DroppedItemKind.LaneFork, "feature-b", "ghost")], removal.DroppedReferences);
    }

    [Fact]
    public void Remove_ConnectorToUnknownBranch_DropsConnectorAndReportsIt()
    {
        Connector dangling = new(GhostConnector, ConnectorKind.Merge, TestPlan.FeatureB, Ghost, 3, null, ProgressState.Planned);
        Plan plan = WithMainScenario(scenario => scenario with { Connectors = scenario.Connectors.Add(dangling) });

        DanglingReferenceRemoval removal = DanglingReferenceRemover.Remove(plan);

        Assert.DoesNotContain(dangling, removal.Plan.Primary().Connectors);
        Assert.Equal([new DroppedReference(TestPlan.MainScenario, DroppedItemKind.Connector, "ghost-connector", "ghost")], removal.DroppedReferences);
    }

    [Fact]
    public void Remove_MarkerOnUnknownBranch_DropsMarkerAndReportsIt()
    {
        Marker dangling = new(new MarkerId("ghost-marker"), MarkerKind.Custom, "X", new BranchStepAnchor(Ghost, 1), MarkerDirection.FromTop, ProgressState.Planned);
        Plan plan = WithMainScenario(scenario => scenario with { Markers = scenario.Markers.Add(dangling) });

        DanglingReferenceRemoval removal = DanglingReferenceRemover.Remove(plan);

        Assert.DoesNotContain(dangling, removal.Plan.Primary().Markers);
        Assert.Equal([new DroppedReference(TestPlan.MainScenario, DroppedItemKind.Marker, "ghost-marker", "ghost")], removal.DroppedReferences);
    }

    [Fact]
    public void Remove_MarkerOnDroppedConnector_DropsMarkerToo()
    {
        Connector dangling = new(GhostConnector, ConnectorKind.Merge, TestPlan.FeatureB, Ghost, 3, null, ProgressState.Planned);
        Marker marker = new(new MarkerId("on-ghost-connector"), MarkerKind.Testing, "X", new ConnectorAnchor(GhostConnector), MarkerDirection.FromRight, ProgressState.Planned);
        Plan plan = WithMainScenario(scenario => scenario with { Connectors = scenario.Connectors.Add(dangling), Markers = scenario.Markers.Add(marker) });

        DanglingReferenceRemoval removal = DanglingReferenceRemover.Remove(plan);

        Assert.DoesNotContain(marker, removal.Plan.Primary().Markers);
        Assert.Contains(new DroppedReference(TestPlan.MainScenario, DroppedItemKind.Marker, "on-ghost-connector", "ghost-connector"), removal.DroppedReferences);
    }

    private static Plan WithMainScenario(Func<Scenario, Scenario> update) =>
        TestPlan.Create().WithScenario(TestPlan.MainScenario, update);
}
