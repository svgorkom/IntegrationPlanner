using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing.Commands;

// Several commands applied as one edit, so they form a single undo step.
public sealed class CompositePlanCommand : IPlanCommand
{
    private readonly IReadOnlyList<IPlanCommand> _commands;

    public CompositePlanCommand(IReadOnlyList<IPlanCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        if (commands.Any(command => command is null))
        {
            throw new ArgumentException("Commands must not contain null.", nameof(commands));
        }

        _commands = [.. commands];
    }

    public Plan Apply(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return _commands.Aggregate(plan, (current, command) => command.Apply(current));
    }
}
