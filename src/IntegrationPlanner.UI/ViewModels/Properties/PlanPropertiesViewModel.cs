using CommunityToolkit.Mvvm.ComponentModel;
using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.UI.ViewModels.Properties;

public sealed partial class PlanPropertiesViewModel : PropertiesViewModel
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _scenarioName = string.Empty;

    [ObservableProperty]
    private int _stepCount;

    public PlanPropertiesViewModel(PlanEditor editor, ScenarioId? scenarioId)
        : base(editor, scenarioId)
    {
        Refresh();
    }

    protected override bool Load(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Title = plan.Title;
        if (CurrentScenario is Scenario scenario)
        {
            ScenarioName = scenario.Name;
            StepCount = scenario.StepCount;
        }

        return true;
    }

    partial void OnTitleChanged(string value)
    {
        if (value != Editor.Current.Title)
        {
            Execute(new SetPlanTitleCommand(value));
        }
    }

    partial void OnScenarioNameChanged(string value) => CommitScenario();

    partial void OnStepCountChanged(int value) => CommitScenario();

    private void CommitScenario()
    {
        if (CurrentScenario is not Scenario scenario || (scenario.Name == ScenarioName && scenario.StepCount == StepCount))
        {
            return;
        }

        Execute(new UpdateScenarioCommand(scenario.Id, ScenarioName, StepCount));
    }
}
