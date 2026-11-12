namespace IntegrationPlanner.Core.Model;

public readonly record struct ConnectorId
{
    public ConnectorId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static ConnectorId New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}
