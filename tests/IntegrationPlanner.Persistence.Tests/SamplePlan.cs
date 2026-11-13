using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Persistence.Tests;

internal static class SamplePlan
{
    public static readonly BranchId Develop = new("develop");
    public static readonly BranchId FeatureA = new("feature-a");
    public static readonly BranchId SubOfA = new("sub-of-a");
    public static readonly ConnectorId MergeAIntoDevelop = new("merge-a-into-develop");

    public static Plan Create() =>
        Plan.Create("Branching strategy.") with
        {
            Branches =
            [
                new Branch(Develop, "Develop", string.Empty, IsMainline: true, IsCancelled: false),
                new Branch(FeatureA, "AD-1", "Feature \"A\" with ^2 and <tags>", IsMainline: false, IsCancelled: false, JiraKey: "AD-1"),
                new Branch(SubOfA, "AD-3", "Sub of A", IsMainline: false, IsCancelled: true),
            ],
            Scenarios =
            [
                Scenario.Create(new ScenarioId("main"), "Scenario 1", 10) with
                {
                    Lanes =
                    [
                        new Lane(Develop, 0, 0, null, false, null, null),
                        new Lane(FeatureA, -1, 0, 8, false, null, null),
                        new Lane(SubOfA, 2, 2, 5, true, FeatureA, 2),
                    ],
                    Connectors =
                    [
                        new Connector(MergeAIntoDevelop, ConnectorKind.Merge, FeatureA, Develop, 8, "Merge to develop", ProgressState.Planned),
                        new Connector(new ConnectorId("pull-develop-into-a"), ConnectorKind.Pull, Develop, FeatureA, 4, null, ProgressState.Done),
                    ],
                    Markers =
                    [
                        new Marker(new MarkerId("release"), MarkerKind.ReleaseBuild, "Release build", new BranchStepAnchor(Develop, 3), MarkerDirection.FromTop, ProgressState.Done),
                        new Marker(new MarkerId("testing"), MarkerKind.Testing, "Lots of testing!", new ConnectorAnchor(MergeAIntoDevelop), MarkerDirection.FromRight, ProgressState.Planned),
                    ],
                },
                Scenario.Create(new ScenarioId("empty"), "Scenario 2", 3),
            ],
        };
}
