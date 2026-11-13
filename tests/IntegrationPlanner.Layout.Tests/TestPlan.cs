using System.Collections.Immutable;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout.Tests;

internal static class TestPlan
{
    public const int StepCount = 10;

    public static readonly ScenarioId ScenarioId = new("scenario");
    public static readonly BranchId Develop = new("develop");
    public static readonly BranchId Feature = new("feature");
    public static readonly BranchId Sub = new("sub");
    public static readonly BranchId Cancelled = new("cancelled");
    public static readonly BranchId NoLane = new("no-lane");

    public static readonly LayoutOptions Options = LayoutOptions.Default;

    public static Plan Create(ImmutableList<Lane> lanes, ImmutableList<Connector>? connectors = null, ImmutableList<Marker>? markers = null) =>
        Plan.Create("Plan title") with
        {
            Branches =
            [
                new Branch(Develop, "Develop", string.Empty, IsMainline: true, IsCancelled: false),
                new Branch(Feature, "AD-1", "A summary that is long enough to be wrapped over more than one line of text", IsMainline: false, IsCancelled: false),
                new Branch(Sub, "AD-2", "Sub branch", IsMainline: false, IsCancelled: false),
                new Branch(Cancelled, "AD-3", "Cancelled work", IsMainline: false, IsCancelled: true),
                new Branch(NoLane, "AD-4", "Not in the scenario", IsMainline: false, IsCancelled: false),
            ],
            Scenarios =
            [
                Scenario.Create(ScenarioId, "Scenario name", StepCount) with
                {
                    Lanes = lanes,
                    Connectors = connectors ?? [],
                    Markers = markers ?? [],
                },
            ],
        };

    public static Lane Mainline() => new(Develop, 0, 0, null, false, null, null);

    public static Lane LaneOf(BranchId branchId, int order, int startStep, int? endStep) => new(branchId, order, startStep, endStep, false, null, null);

    public static Lane ForkedLane(BranchId branchId, int order, BranchId parentId, int forkStep, bool showFork) =>
        new(branchId, order, forkStep, null, showFork, parentId, forkStep);

    public static Connector Merge(string id, BranchId from, BranchId to, int step, string? label = null, ProgressState state = ProgressState.Done) =>
        new(new ConnectorId(id), ConnectorKind.Merge, from, to, step, label, state);

    public static Marker MarkerOn(MarkerAnchor anchor, double angle, ProgressState state = ProgressState.Done) =>
        new(new MarkerId("marker"), MarkerKind.ReleaseBuild, "Release build", anchor, angle, state);

    public static DiagramScene Layout(Plan plan) =>
        new DiagramLayoutEngine(new FixedWidthTextMeasurer(), Options).Layout(plan, ScenarioId);

    public static double X(int step) => step * Options.StepWidth;

    public static double Y(int order) => order * Options.LaneSpacing;
}
