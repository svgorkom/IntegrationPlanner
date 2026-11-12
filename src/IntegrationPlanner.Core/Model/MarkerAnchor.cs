namespace IntegrationPlanner.Core.Model;

public abstract record MarkerAnchor;

public sealed record BranchStepAnchor(BranchId BranchId, int Step) : MarkerAnchor;

public sealed record ConnectorAnchor(ConnectorId ConnectorId) : MarkerAnchor;
