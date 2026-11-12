using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class DeleteBranchCommand : IPlanCommand
{
    private readonly BranchId _branchId;

    public DeleteBranchCommand(BranchId branchId)
    {
        _branchId = branchId;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Branches.EnsureContains(_branchId, branch => branch.Id);
        return plan with
        {
            Branches = plan.Branches.RemoveAll(branch => branch.Id == _branchId),
            Scenarios = plan.Scenarios.ConvertAll(scenario => scenario.WithoutBranch(_branchId)),
        };
    }
}
