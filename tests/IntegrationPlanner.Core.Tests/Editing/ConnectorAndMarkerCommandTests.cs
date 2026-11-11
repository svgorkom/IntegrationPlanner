using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Tests.Editing;

public sealed class ConnectorAndMarkerCommandTests
{
    private readonly Plan _plan = TestPlan.Create();

    [Fact]
    public void AddConnector_AppendsConnector()
    {
        Connector connector = new(new ConnectorId("pull-into-a"), ConnectorKind.Pull, TestPlan.Develop, TestPlan.FeatureA, 3, "Pull develop", ProgressState.Planned);
        AddConnectorCommand command = new(TestPlan.MainScenario, connector);

        Plan result = command.Apply(_plan);

        Assert.Equal(connector, result.Primary().Connectors[^1]);
    }

    [Fact]
    public void AddConnector_BetweenSiblingBranches_IsAccepted()
    {
        Connector connector = new(new ConnectorId("merge-b-into-a"), ConnectorKind.Merge, TestPlan.FeatureB, TestPlan.FeatureA, 7, null, ProgressState.Planned);
        AddConnectorCommand command = new(TestPlan.MainScenario, connector);

        Plan result = command.Apply(_plan);

        Assert.Contains(connector, result.Primary().Connectors);
    }

    [Fact]
    public void AddConnector_FromCancelledBranch_IsAccepted()
    {
        Connector connector = new(new ConnectorId("merge-sub-into-develop"), ConnectorKind.Merge, TestPlan.SubOfA, TestPlan.Develop, 6, null, ProgressState.Planned);
        AddConnectorCommand command = new(TestPlan.MainScenario, connector);

        Plan result = command.Apply(_plan);

        Assert.Contains(connector, result.Primary().Connectors);
    }

    [Fact]
    public void UpdateConnector_ChangesState()
    {
        Connector done = _plan.Primary().Connectors[0] with { State = ProgressState.Done };
        UpdateConnectorCommand command = new(TestPlan.MainScenario, done);

        Plan result = command.Apply(_plan);

        Assert.Equal(ProgressState.Done, result.Primary().Connectors[0].State);
    }

    [Fact]
    public void DeleteConnector_RemovesConnector()
    {
        DeleteConnectorCommand command = new(TestPlan.MainScenario, TestPlan.MergeAIntoDevelop);

        Plan result = command.Apply(_plan);

        Assert.DoesNotContain(result.Primary().Connectors, connector => connector.Id == TestPlan.MergeAIntoDevelop);
    }

    [Fact]
    public void DeleteConnector_RemovesMarkersAnchoredToIt()
    {
        DeleteConnectorCommand command = new(TestPlan.MainScenario, TestPlan.MergeAIntoDevelop);

        Plan result = command.Apply(_plan);

        Assert.Equal([TestPlan.ReleaseOnDevelop, TestPlan.TestingOnFeatureA], result.Primary().Markers.Select(marker => marker.Id));
    }

    [Fact]
    public void AddMarker_AnchoredToConnector_AppendsMarker()
    {
        Marker marker = new(new MarkerId("integration-test"), MarkerKind.IntegrationTest, "Start integration test", new ConnectorAnchor(TestPlan.PullDevelopIntoB), MarkerDirection.FromLeft, ProgressState.Planned);
        AddMarkerCommand command = new(TestPlan.MainScenario, marker);

        Plan result = command.Apply(_plan);

        Assert.Equal(marker, result.Primary().Markers[^1]);
    }

    [Fact]
    public void UpdateMarker_ChangesLabel()
    {
        Marker relabelled = _plan.Primary().Markers[0] with { Label = "Release 1.2" };
        UpdateMarkerCommand command = new(TestPlan.MainScenario, relabelled);

        Plan result = command.Apply(_plan);

        Assert.Equal("Release 1.2", result.Primary().Markers[0].Label);
    }

    [Fact]
    public void DeleteMarker_RemovesMarker()
    {
        DeleteMarkerCommand command = new(TestPlan.MainScenario, TestPlan.ReleaseOnDevelop);

        Plan result = command.Apply(_plan);

        Assert.DoesNotContain(result.Primary().Markers, marker => marker.Id == TestPlan.ReleaseOnDevelop);
    }
}
