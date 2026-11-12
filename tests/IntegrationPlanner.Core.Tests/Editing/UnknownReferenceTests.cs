using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Tests.Editing;

public sealed class UnknownReferenceTests
{
    public static IEnumerable<TheoryDataRow<IPlanCommand>> CommandsReferringToMissingItems =>
    [
        Row("update unknown branch", new UpdateBranchCommand(new Branch(TestPlan.Unknown, "X", string.Empty, false, false))),
        Row("delete unknown branch", new DeleteBranchCommand(TestPlan.Unknown)),
        Row("update unknown scenario", new UpdateScenarioCommand(new ScenarioId("missing"), "X", 1)),
        Row("delete unknown scenario", new DeleteScenarioCommand(new ScenarioId("missing"))),
        Row("lane for unknown branch", new SetLaneCommand(TestPlan.MainScenario, new Lane(TestPlan.Unknown, 1, 0, null, false, null, null))),
        Row("lane forked from unknown branch", new SetLaneCommand(TestPlan.MainScenario, new Lane(TestPlan.FeatureB, 1, 0, null, true, TestPlan.Unknown, 0))),
        Row("remove lane that does not exist", new RemoveLaneCommand(TestPlan.OtherScenario, TestPlan.FeatureB)),
        Row("connector from unknown branch", new AddConnectorCommand(TestPlan.MainScenario, new Connector(new ConnectorId("c"), ConnectorKind.Merge, TestPlan.Unknown, TestPlan.Develop, 1, null, ProgressState.Planned))),
        Row("connector to unknown branch", new AddConnectorCommand(TestPlan.MainScenario, new Connector(new ConnectorId("c"), ConnectorKind.Pull, TestPlan.Develop, TestPlan.Unknown, 1, null, ProgressState.Planned))),
        Row("update unknown connector", new UpdateConnectorCommand(TestPlan.MainScenario, new Connector(TestPlan.UnknownConnector, ConnectorKind.Merge, TestPlan.FeatureA, TestPlan.Develop, 1, null, ProgressState.Planned))),
        Row("delete unknown connector", new DeleteConnectorCommand(TestPlan.MainScenario, TestPlan.UnknownConnector)),
        Row("marker on unknown branch", new AddMarkerCommand(TestPlan.MainScenario, new Marker(new MarkerId("m"), MarkerKind.Custom, "X", new BranchStepAnchor(TestPlan.Unknown, 1), MarkerDirection.FromTop, ProgressState.Planned))),
        Row("marker on connector of other scenario", new AddMarkerCommand(TestPlan.MainScenario, new Marker(new MarkerId("m"), MarkerKind.Custom, "X", new ConnectorAnchor(TestPlan.OtherMergeAIntoDevelop), MarkerDirection.FromTop, ProgressState.Planned))),
        Row("update unknown marker", new UpdateMarkerCommand(TestPlan.MainScenario, new Marker(TestPlan.UnknownMarker, MarkerKind.Custom, "X", new BranchStepAnchor(TestPlan.Develop, 1), MarkerDirection.FromTop, ProgressState.Planned))),
        Row("delete unknown marker", new DeleteMarkerCommand(TestPlan.MainScenario, TestPlan.UnknownMarker)),
    ];

    [Theory]
    [MemberData(nameof(CommandsReferringToMissingItems))]
    public void Apply_ReferringToMissingItem_ThrowsKeyNotFound(IPlanCommand command)
    {
        Plan plan = TestPlan.Create();

        Assert.Throws<KeyNotFoundException>(() => command.Apply(plan));
    }

    private static TheoryDataRow<IPlanCommand> Row(string example, IPlanCommand command) =>
        new(command) { TestDisplayName = example };
}
