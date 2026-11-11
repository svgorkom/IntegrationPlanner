using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing;

public interface IPlanCommand
{
    Plan Apply(Plan plan);
}
