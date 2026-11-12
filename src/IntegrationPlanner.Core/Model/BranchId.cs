namespace IntegrationPlanner.Core.Model;

public readonly record struct BranchId
{
    public BranchId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static BranchId New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}
