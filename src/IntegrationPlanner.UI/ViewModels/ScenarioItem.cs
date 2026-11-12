using CommunityToolkit.Mvvm.ComponentModel;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.UI.ViewModels;

// A class rather than a record: the selected instance must survive a rename so the selection is kept.
public sealed partial class ScenarioItem : ObservableObject
{
    [ObservableProperty]
    private string _name;

    public ScenarioItem(ScenarioId id, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Id = id;
        _name = name;
    }

    public ScenarioId Id { get; }
}