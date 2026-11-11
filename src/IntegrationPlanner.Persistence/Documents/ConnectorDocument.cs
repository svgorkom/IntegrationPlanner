using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Persistence.Documents;

internal sealed class ConnectorDocument
{
    public string? Id { get; set; }

    public ConnectorKind? Kind { get; set; }

    public string? FromBranchId { get; set; }

    public string? ToBranchId { get; set; }

    public int? Step { get; set; }

    public string? Label { get; set; }

    public ProgressState? State { get; set; }
}
