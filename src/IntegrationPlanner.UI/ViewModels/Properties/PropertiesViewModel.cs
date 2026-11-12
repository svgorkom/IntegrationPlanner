using CommunityToolkit.Mvvm.ComponentModel;
using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.UI.ViewModels.Properties;

// One property-panel page. Each committed field change becomes one undoable command (D10).
// Refresh reloads the fields from the current plan without turning them into commands.
public abstract class PropertiesViewModel : ObservableObject
{
    protected PropertiesViewModel(PlanEditor editor, ScenarioId? scenarioId)
    {
        ArgumentNullException.ThrowIfNull(editor);
        Editor = editor;
        ScenarioId = scenarioId;
    }

    public bool HasScenario => ScenarioId is not null;

    protected PlanEditor Editor { get; }

    protected ScenarioId? ScenarioId { get; }

    protected bool IsRefreshing { get; private set; }

    protected Scenario? CurrentScenario =>
        ScenarioId is ScenarioId id ? Editor.Current.Scenarios.Find(scenario => scenario.Id == id) : null;

    // Returns false when the item no longer exists, for example after an undo.
    public bool Refresh()
    {
        IsRefreshing = true;
        try
        {
            return Load(Editor.Current);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    protected abstract bool Load(Plan plan);

    protected void Execute(IPlanCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (IsRefreshing)
        {
            return;
        }

        Editor.Execute(command);
    }

    // Replacing an ItemsSource makes a combo box briefly clear its selection, so only replace real changes.
    protected bool SetChoices<T>(ref IReadOnlyList<T> field, IReadOnlyList<T> value, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (field.SequenceEqual(value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
