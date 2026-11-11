namespace IntegrationPlanner.Core.Model;

public sealed record Connector(
    ConnectorId Id,
    ConnectorKind Kind,
    BranchId FromBranchId,
    BranchId ToBranchId,
    int Step,
    string? Label,
    ProgressState State)
{
    public bool Involves(BranchId branchId) => FromBranchId == branchId || ToBranchId == branchId;
}
