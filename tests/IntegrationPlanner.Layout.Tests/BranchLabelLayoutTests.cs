using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout.Tests;

public sealed class BranchLabelLayoutTests
{
    [Fact]
    public void BranchLabel_StartsWithTheBranchNameInTheLargeStyle()
    {
        Plan plan = TestPlan.Create([TestPlan.LaneOf(TestPlan.Feature, 1, 2, 6)]);

        DiagramScene scene = TestPlan.Layout(plan);

        SceneText name = scene.Texts(SceneKeys.BranchLabel(TestPlan.Feature))[0];
        Assert.Equal(("AD-1", TextStyle.BranchName, TestPlan.Options.BranchNameFontSize), (name.Text, name.Style, name.FontSize));
    }

    [Fact]
    public void BranchSummary_IsWrappedToTheWrapWidth()
    {
        Plan plan = TestPlan.Create([TestPlan.LaneOf(TestPlan.Feature, 1, 2, 6)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.All(scene.Texts(SceneKeys.BranchLabel(TestPlan.Feature)).Skip(1), line => Assert.True(line.Size.Width <= TestPlan.Options.SummaryWrapWidth));
    }

    [Fact]
    public void LongBranchSummary_SpansSeveralLines()
    {
        Plan plan = TestPlan.Create([TestPlan.LaneOf(TestPlan.Feature, 1, 2, 6)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.True(scene.Texts(SceneKeys.BranchLabel(TestPlan.Feature)).Length > 2);
    }

    [Fact]
    public void TicketBranch_ShowsOnlyItsKeyWithTheFullNameAsToolTip()
    {
        Plan plan = TestPlan.Create([TestPlan.LaneOf(TestPlan.Feature, 1, 2, 6)]);
        plan = plan with
        {
            Branches = plan.Branches.ConvertAll(branch => branch.Id == TestPlan.Feature ? branch with { Name = "feature/AD-1-long-name", JiraKey = "AD-1" } : branch),
        };

        DiagramScene scene = TestPlan.Layout(plan);

        SceneText label = Assert.Single(scene.Texts(SceneKeys.BranchLabel(TestPlan.Feature)));
        Assert.Equal(("AD-1", true), (label.Text, label.ToolTip!.StartsWith("feature/AD-1-long-name", StringComparison.Ordinal)));
    }

    [Fact]
    public void BranchWithoutSummary_IsLabelledWithItsNameOnly()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline()]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal("Develop", Assert.Single(scene.Texts(SceneKeys.BranchLabel(TestPlan.Develop))).Text);
    }

    [Fact]
    public void BranchLabelWithSummary_SitsAboveItsLaneAfterTheLaneStart()
    {
        Plan plan = TestPlan.Create([TestPlan.LaneOf(TestPlan.Feature, 1, 2, 6)]);

        DiagramScene scene = TestPlan.Layout(plan);

        SceneRect label = scene.Texts(SceneKeys.BranchLabel(TestPlan.Feature)).BoundsOf();
        Assert.True(label.Bottom < TestPlan.Y(1) && label.Left > TestPlan.X(2));
    }

    [Fact]
    public void NameOnlyBranchLabel_EndsLeftOfTheLaneStart()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline()]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.True(scene.Text(SceneKeys.BranchLabel(TestPlan.Develop)).Bounds.Right < TestPlan.X(0));
    }

    [Fact]
    public void BranchSummary_IsWrappedToTheLaneLength()
    {
        Plan plan = TestPlan.Create([TestPlan.LaneOf(TestPlan.Feature, 1, 2, 4)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.All(scene.Texts(SceneKeys.BranchLabel(TestPlan.Feature)).Skip(1), line => Assert.True(line.Size.Width <= TestPlan.X(4) - TestPlan.X(2)));
    }

    [Fact]
    public void MainlineLabel_IsVerticallyCentredOnTheLane()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline()]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(TestPlan.Y(0), scene.Text(SceneKeys.BranchLabel(TestPlan.Develop)).Center.Y);
    }

    [Fact]
    public void ForkedBranchLabel_SitsAboveItsLaneAfterTheFork()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline(), TestPlan.ForkedLane(TestPlan.Sub, 2, TestPlan.Develop, 3, showFork: true)]);

        DiagramScene scene = TestPlan.Layout(plan);

        SceneRect label = scene.Texts(SceneKeys.BranchLabel(TestPlan.Sub)).BoundsOf();
        SceneLine lane = scene.Line(SceneKeys.Lane(TestPlan.Sub));
        Assert.True(label.Bottom < lane.From.Y && label.Left > lane.From.X);
    }

    [Fact]
    public void CancelledBranchLabel_UsesTheCancelledStyles()
    {
        Plan plan = TestPlan.Create([TestPlan.LaneOf(TestPlan.Cancelled, 1, 0, 4)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal([TextStyle.CancelledBranchName, TextStyle.CancelledBranchSummary], scene.Texts(SceneKeys.BranchLabel(TestPlan.Cancelled)).Select(text => text.Style));
    }

    [Fact]
    public void LabelsOfLanesStartingAtTheSameSpot_DoNotOverlap()
    {
        Plan plan = TestPlan.Create([TestPlan.LaneOf(TestPlan.Feature, 1, 2, 6), TestPlan.LaneOf(TestPlan.Sub, 1, 2, 6)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Empty(scene.OverlappingLabels());
    }
}
