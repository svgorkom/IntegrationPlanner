using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Persistence.Documents;

internal sealed class MarkerDocument
{
    public string? Id { get; set; }

    public MarkerKind? Kind { get; set; }

    public string? Label { get; set; }

    public AnchorDocument? Anchor { get; set; }

    public double? Angle { get; set; }

    public double? Length { get; set; }

    // Files written before free rotation only carry one of four directions.
    public LegacyMarkerDirection? Direction { get; set; }

    public ProgressState? State { get; set; }
}

internal enum LegacyMarkerDirection
{
    FromTop,
    FromBottom,
    FromLeft,
    FromRight,
}
