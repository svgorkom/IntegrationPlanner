namespace IntegrationPlanner.Persistence.Documents;

internal sealed class PlanDocument
{
    public int? SchemaVersion { get; set; }

    public string? Title { get; set; }

    public List<BranchDocument>? Branches { get; set; }

    public List<ScenarioDocument>? Scenarios { get; set; }
}
