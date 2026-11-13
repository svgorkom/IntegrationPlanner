using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Jira;
using IntegrationPlanner.UI.Editing;
using IntegrationPlanner.UI.Tests.Fakes;

namespace IntegrationPlanner.UI.Tests;

public sealed class TicketBranchFactoryTests
{
    private static readonly JiraIssue Issue = new("AD-42", "Fix the thing", "Open", new Uri("https://jira.example.com/browse/AD-42"));

    [Fact]
    public void Create_AddsTheTicketBranchWithItsKeyAndSummary()
    {
        Plan plan = TestPlans.WithLanes();

        Plan result = Apply(plan, "feature/AD-42-fix");

        Branch branch = Assert.Single(result.Branches, b => b.JiraKey == "AD-42");
        Assert.Equal(("feature/AD-42-fix", "Fix the thing", false), (branch.Name, branch.Summary, branch.IsMainline));
    }

    [Fact]
    public void Create_AddsAPullFromTheMainlineToTheNewBranch()
    {
        Plan plan = TestPlans.WithLanes();

        Plan result = Apply(plan, "feature/AD-42-fix");

        Branch ticket = result.Branches.Single(b => b.JiraKey == "AD-42");
        Connector pull = Assert.Single(result.Scenarios[0].Connectors, c => c.ToBranchId == ticket.Id);
        Branch from = result.Branches.Single(b => b.Id == pull.FromBranchId);
        Assert.Equal((ConnectorKind.Pull, "main"), (pull.Kind, from.Name));
    }

    [Fact]
    public void Create_GivesTheNewBranchALaneInTheScenario()
    {
        Plan plan = TestPlans.WithLanes();

        Plan result = Apply(plan, "feature/AD-42-fix");

        Branch ticket = result.Branches.Single(b => b.JiraKey == "AD-42");
        Assert.Contains(result.Scenarios[0].Lanes, lane => lane.BranchId == ticket.Id);
    }

    [Fact]
    public void Create_RejectsATicketThatAlreadyHasABranch()
    {
        Plan plan = Apply(TestPlans.WithLanes(), "feature/AD-42-fix");

        TicketBranchResult result = TicketBranchFactory.Create(plan, plan.Scenarios[0], Issue, "feature/other", "main");

        Assert.Equal((TicketBranchError.DuplicateTicket, null), (result.Error, result.Command));
    }

    [Fact]
    public void Create_RejectsAnExistingBranchName()
    {
        Plan plan = Apply(TestPlans.WithLanes(), "feature/AD-42-fix");
        JiraIssue other = Issue with { Key = "AD-43" };

        TicketBranchResult result = TicketBranchFactory.Create(plan, plan.Scenarios[0], other, "FEATURE/ad-42-fix", "main");

        Assert.Equal(TicketBranchError.DuplicateName, result.Error);
    }

    private static Plan Apply(Plan plan, string name)
    {
        TicketBranchResult result = TicketBranchFactory.Create(plan, plan.Scenarios[0], Issue, name, "main");
        return result.Command!.Apply(plan);
    }
}
