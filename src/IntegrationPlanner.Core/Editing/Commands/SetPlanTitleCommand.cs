using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

public sealed class SetPlanTitleCommand : IPlanCommand
{
    private readonly string _title;

    public SetPlanTitleCommand(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        _title = title;
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan with { Title = _title };
    }
}
