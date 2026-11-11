namespace IntegrationPlanner.Core.Model;

public readonly record struct MarkerId
{
    public MarkerId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static MarkerId New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}
