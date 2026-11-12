using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout;

public sealed class DiagramLayoutEngine : IDiagramLayoutEngine
{
    private readonly ITextMeasurer _measurer;
    private readonly LayoutOptions _options;

    public DiagramLayoutEngine(ITextMeasurer measurer, LayoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(measurer);
        ArgumentNullException.ThrowIfNull(options);
        _measurer = measurer;
        _options = options;
    }

    public DiagramScene Layout(Plan plan, ScenarioId scenarioId)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Scenario scenario = plan.GetScenario(scenarioId);
        return new SceneBuilder(plan, scenario, new TextBlockFactory(_measurer, _options), _options).Build();
    }
}
