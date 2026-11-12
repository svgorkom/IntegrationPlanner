namespace IntegrationPlanner.Persistence.Documents;

internal sealed class ScenarioDocument
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public int? StepCount { get; set; }

    public List<LaneDocument>? Lanes { get; set; }

    public List<ConnectorDocument>? Connectors { get; set; }

    public List<MarkerDocument>? Markers { get; set; }
}
