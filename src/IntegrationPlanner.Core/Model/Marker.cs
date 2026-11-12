namespace IntegrationPlanner.Core.Model;

public sealed record Marker(
    MarkerId Id,
    MarkerKind Kind,
    string Label,
    MarkerAnchor Anchor,
    MarkerDirection Direction,
    ProgressState State)
{
    public bool IsAnchoredTo(BranchId branchId) => Anchor is BranchStepAnchor anchor && anchor.BranchId == branchId;

    public bool IsAnchoredTo(ConnectorId connectorId) => Anchor is ConnectorAnchor anchor && anchor.ConnectorId == connectorId;
}
