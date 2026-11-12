using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class UpdateBranchCommand : IPlanCommand
{
    private readonly Branch _branch;

    public UpdateBranchCommand(Branch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        _branch = branch;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan with { Branches = plan.Branches.ReplaceById(_branch, branch => branch.Id) };
    }
}
