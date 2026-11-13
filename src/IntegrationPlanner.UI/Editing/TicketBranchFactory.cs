using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Jira;

namespace IntegrationPlanner.UI.Editing;

public enum TicketBranchError
{
    None,
    DuplicateTicket,
    DuplicateName,
}

public sealed record TicketBranchResult(IPlanCommand? Command, BranchId BranchId, TicketBranchError Error);

// A ticket branch starts from the mainline: the branch gets a lane and a pull from the mainline lane.
public static class TicketBranchFactory
{
    private const int PullStep = 1;

    public static TicketBranchResult Create(Plan plan, Scenario? scenario, JiraIssue issue, string branchName, string mainlineBranchName)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(issue);
        ArgumentException.ThrowIfNullOrWhiteSpace(branchName);
        string name = branchName.Trim();
        if (plan.Branches.Any(branch => string.Equals(branch.JiraKey, issue.Key, StringComparison.OrdinalIgnoreCase)))
        {
            return Rejected(TicketBranchError.DuplicateTicket);
        }

        if (plan.Branches.Any(branch => string.Equals(branch.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return Rejected(TicketBranchError.DuplicateName);
        }

        BranchId branchId = BranchId.New();
        List<IPlanCommand> commands = [new AddBranchCommand(new Branch(branchId, name, issue.Summary, IsMainline: false, IsCancelled: false, issue.Key))];
        if (scenario is not null)
        {
            AddLaneAndPull(commands, plan, scenario, branchId, mainlineBranchName);
        }

        return new TicketBranchResult(new CompositePlanCommand(commands), branchId, TicketBranchError.None);
    }

    private static TicketBranchResult Rejected(TicketBranchError error) => new(null, default, error);

    private static void AddLaneAndPull(List<IPlanCommand> commands, Plan plan, Scenario scenario, BranchId branchId, string mainlineBranchName)
    {
        string mainlineName = string.IsNullOrWhiteSpace(mainlineBranchName) ? JiraSettings.DefaultMainlineBranchName : mainlineBranchName.Trim();
        Branch? existing = plan.Branches.Find(branch => string.Equals(branch.Name, mainlineName, StringComparison.OrdinalIgnoreCase));
        BranchId mainlineId = existing?.Id ?? BranchId.New();
        if (existing is null)
        {
            commands.Insert(0, new AddBranchCommand(new Branch(mainlineId, mainlineName, string.Empty, IsMainline: true, IsCancelled: false)));
        }

        Scenario working = scenario;
        if (!working.Lanes.Exists(lane => lane.BranchId == mainlineId))
        {
            Lane mainlineLane = PlanItemFactory.NewLane(working, mainlineId);
            commands.Add(new SetLaneCommand(scenario.Id, mainlineLane));
            working = working.WithLane(mainlineLane);
        }

        int step = Math.Clamp(PullStep, 0, Math.Max(scenario.StepCount - 1, 0));
        commands.Add(new SetLaneCommand(scenario.Id, PlanItemFactory.NewLane(working, branchId) with { StartStep = step }));
        commands.Add(new AddConnectorCommand(
            scenario.Id,
            new Connector(ConnectorId.New(), ConnectorKind.Pull, mainlineId, branchId, step, null, ProgressState.Planned)));
    }
}
