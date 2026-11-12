namespace IntegrationPlanner.Persistence.Documents;

internal sealed class BranchDocument
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Summary { get; set; }

    public bool IsMainline { get; set; }

    public bool IsCancelled { get; set; }
}
