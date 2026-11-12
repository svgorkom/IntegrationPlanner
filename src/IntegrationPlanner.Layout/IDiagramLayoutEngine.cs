using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout;

public interface IDiagramLayoutEngine
{
    DiagramScene Layout(Plan plan, ScenarioId scenarioId);
}
