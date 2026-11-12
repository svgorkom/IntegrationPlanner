namespace IntegrationPlanner.Core.Model;

public sealed record Lane(
    BranchId BranchId,
    int Order,
    int StartStep,
    int? EndStep,
    bool ShowFork,
    BranchId? ForkFromBranchId,
    int? ForkStep)
{
    public Lane WithoutFork() => this with { ShowFork = false, ForkFromBranchId = null, ForkStep = null };
}
