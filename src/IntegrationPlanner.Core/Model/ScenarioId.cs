namespace IntegrationPlanner.Core.Model;

public readonly record struct ScenarioId
{
    public ScenarioId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static ScenarioId New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}
