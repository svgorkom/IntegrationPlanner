using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Persistence.Documents;

internal sealed class MarkerDocument
{
    public string? Id { get; set; }

    public MarkerKind? Kind { get; set; }

    public string? Label { get; set; }

    public AnchorDocument? Anchor { get; set; }

    public MarkerDirection? Direction { get; set; }

    public ProgressState? State { get; set; }
}
