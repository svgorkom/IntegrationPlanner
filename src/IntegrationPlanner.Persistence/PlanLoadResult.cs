using System.Diagnostics.CodeAnalysis;
using IntegrationPlanner.Core.Integrity;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Persistence;

public sealed class PlanLoadResult
{
    private PlanLoadResult(Plan? plan, PlanLoadError? error, IReadOnlyList<DroppedReference> droppedReferences)
    {
        Plan = plan;
        Error = error;
        DroppedReferences = droppedReferences;
    }

    public Plan? Plan { get; }

    public PlanLoadError? Error { get; }

    public IReadOnlyList<DroppedReference> DroppedReferences { get; }

    [MemberNotNullWhen(true, nameof(Plan))]
    public bool IsSuccess => Plan is not null;

    public static PlanLoadResult Success(Plan plan, IReadOnlyList<DroppedReference> droppedReferences)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(droppedReferences);
        return new PlanLoadResult(plan, null, droppedReferences);
    }

    public static PlanLoadResult Failure(PlanLoadError error) => new(null, error, []);
}
