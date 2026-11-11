namespace IntegrationPlanner.Core.Model;

public sealed record Branch(BranchId Id, string Name, string Summary, bool IsMainline, bool IsCancelled);
