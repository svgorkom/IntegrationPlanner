using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Editing;

// Undo/redo keeps whole-plan snapshots: the model is immutable, so a snapshot is just a reference.
public sealed class PlanEditor
{
    private readonly Stack<Plan> _undoStack = new();
    private readonly Stack<Plan> _redoStack = new();

    public PlanEditor(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Current = plan;
    }

    public event EventHandler? Changed;

    public Plan Current { get; private set; }

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public void Execute(IPlanCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Plan next = command.Apply(Current);
        _undoStack.Push(Current);
        _redoStack.Clear();
        SetCurrent(next);
    }

    public bool Undo()
    {
        if (!CanUndo)
        {
            return false;
        }

        _redoStack.Push(Current);
        SetCurrent(_undoStack.Pop());
        return true;
    }

    public bool Redo()
    {
        if (!CanRedo)
        {
            return false;
        }

        _undoStack.Push(Current);
        SetCurrent(_redoStack.Pop());
        return true;
    }

    private void SetCurrent(Plan plan)
    {
        Current = plan;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
