namespace IntegrationPlanner.Core.Model;

public sealed record Marker(
    MarkerId Id,
    MarkerKind Kind,
    string Label,
    MarkerAnchor Anchor,
    double Angle,
    ProgressState State,
    double Length = Marker.DefaultLength)
{
    public const double DefaultLength = 200;

    public const double MinimumLength = 60;

    public bool IsAnchoredTo(BranchId branchId) => Anchor is BranchStepAnchor anchor && anchor.BranchId == branchId;

    public bool IsAnchoredTo(ConnectorId connectorId) => Anchor is ConnectorAnchor anchor && anchor.ConnectorId == connectorId;
}
