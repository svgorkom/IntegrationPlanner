using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Persistence;

public interface IPlanFileStore
{
    Task<PlanLoadResult> LoadAsync(string path, CancellationToken cancellationToken);

    Task SaveAsync(Plan plan, string path, CancellationToken cancellationToken);
}
