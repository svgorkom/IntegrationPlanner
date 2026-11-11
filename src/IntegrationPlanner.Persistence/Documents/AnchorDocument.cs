namespace IntegrationPlanner.Persistence.Documents;

internal sealed class AnchorDocument
{
    public string? BranchId { get; set; }

    public int? Step { get; set; }

    public string? ConnectorId { get; set; }
}
