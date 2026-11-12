using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class AddBranchCommand : IPlanCommand
{
    private readonly Branch _branch;

    public AddBranchCommand(Branch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        _branch = branch;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan with { Branches = plan.Branches.AddWithUniqueId(_branch, branch => branch.Id) };
    }
}
