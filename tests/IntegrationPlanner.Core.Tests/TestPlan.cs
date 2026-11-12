using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Tests;

internal static class TestPlan
{
    public static readonly BranchId Develop = new("develop");
    public static readonly BranchId FeatureA = new("feature-a");
    public static readonly BranchId FeatureB = new("feature-b");
    public static readonly BranchId SubOfA = new("sub-of-a");
    public static readonly BranchId Unknown = new("unknown");

    public static readonly ScenarioId MainScenario = new("main");
    public static readonly ScenarioId OtherScenario = new("other");

    public static readonly ConnectorId MergeAIntoDevelop = new("merge-a-into-develop");
    public static readonly ConnectorId PullDevelopIntoB = new("pull-develop-into-b");
    public static readonly ConnectorId MergeSubIntoA = new("merge-sub-into-a");
    public static readonly ConnectorId OtherMergeAIntoDevelop = new("other-merge-a-into-develop");
    public static readonly ConnectorId UnknownConnector = new("unknown-connector");

    public static readonly MarkerId ReleaseOnDevelop = new("release-on-develop");
    public static readonly MarkerId TestingOnFeatureA = new("testing-on-feature-a");
    public static readonly MarkerId TestingAtMergeA = new("testing-at-merge-a");
    public static readonly MarkerId UnknownMarker = new("unknown-marker");

    public static Plan Create() =>
        Plan.Create("Branching strategy.") with
        {
            Branches =
            [
                new Branch(Develop, "Develop", string.Empty, IsMainline: true, IsCancelled: false),
                new Branch(FeatureA, "AD-1", "Feature A", IsMainline: false, IsCancelled: false),
                new Branch(FeatureB, "AD-2", "Feature B", IsMainline: false, IsCancelled: false),
                new Branch(SubOfA, "AD-3", "Sub of A", IsMainline: false, IsCancelled: true),
            ],
            Scenarios =
            [
                Scenario.Create(MainScenario, "Scenario 1", 10) with
                {
                    Lanes =
                    [
                        new Lane(Develop, 0, 0, null, false, null, null),
                        new Lane(FeatureA, 1, 0, 8, false, null, null),
                        new Lane(FeatureB, -1, 0, null, false, null, null),
                        new Lane(SubOfA, 2, 2, 5, true, FeatureA, 2),
                    ],
                    Connectors =
                    [
                        new Connector(MergeAIntoDevelop, ConnectorKind.Merge, FeatureA, Develop, 8, "Merge to develop", ProgressState.Planned),
                        new Connector(PullDevelopIntoB, ConnectorKind.Pull, Develop, FeatureB, 4, null, ProgressState.Done),
                        new Connector(MergeSubIntoA, ConnectorKind.Merge, SubOfA, FeatureA, 5, null, ProgressState.Planned),
                    ],
                    Markers =
                    [
                        new Marker(ReleaseOnDevelop, MarkerKind.ReleaseBuild, "Release build", new BranchStepAnchor(Develop, 3), MarkerDirection.FromTop, ProgressState.Planned),
                        new Marker(TestingOnFeatureA, MarkerKind.Testing, "Test A", new BranchStepAnchor(FeatureA, 6), MarkerDirection.FromBottom, ProgressState.Planned),
                        new Marker(TestingAtMergeA, MarkerKind.Testing, "Lots of testing!", new ConnectorAnchor(MergeAIntoDevelop), MarkerDirection.FromRight, ProgressState.Planned),
                    ],
                },
                Scenario.Create(OtherScenario, "Scenario 2", 5) with
                {
                    Lanes =
                    [
                        new Lane(Develop, 0, 0, null, false, null, null),
                        new Lane(FeatureA, 1, 0, null, false, null, null),
                    ],
                    Connectors =
                    [
                        new Connector(OtherMergeAIntoDevelop, ConnectorKind.Merge, FeatureA, Develop, 4, null, ProgressState.Planned),
                    ],
                },
            ],
        };

    public static Scenario Primary(this Plan plan) => plan.GetScenario(MainScenario);

    public static Scenario Secondary(this Plan plan) => plan.GetScenario(OtherScenario);
}
