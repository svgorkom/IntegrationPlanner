using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Integrity;

public sealed record DanglingReferenceRemoval(Plan Plan, IReadOnlyList<DroppedReference> DroppedReferences);
