using CommunityToolkit.Mvvm.ComponentModel;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.UI.ViewModels;

// See ScenarioItem: the selected instance must survive a rename.
public sealed partial class BranchItem : ObservableObject
{
    [ObservableProperty]
    private string _name;

    public BranchItem(BranchId id, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Id = id;
        _name = name;
    }

    public BranchId Id { get; }
}