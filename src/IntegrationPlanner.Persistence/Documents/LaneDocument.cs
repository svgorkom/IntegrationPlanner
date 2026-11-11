namespace IntegrationPlanner.Persistence.Documents;

internal sealed class LaneDocument
{
    public string? BranchId { get; set; }

    public int? Order { get; set; }

    public int? StartStep { get; set; }

    public int? EndStep { get; set; }

    public bool ShowFork { get; set; }

    public string? ForkFromBranchId { get; set; }

    public int? ForkStep { get; set; }
}
