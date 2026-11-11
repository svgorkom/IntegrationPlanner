using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Tests.Editing;

public sealed class PlanEditorTests
{
    private readonly Plan _original = TestPlan.Create();
    private readonly PlanEditor _editor;

    public PlanEditorTests()
    {
        _editor = new PlanEditor(_original);
    }

    [Fact]
    public void Execute_AppliesCommandToCurrentPlan()
    {
        SetPlanTitleCommand command = new("Renamed");

        _editor.Execute(command);

        Assert.Equal("Renamed", _editor.Current.Title);
    }

    [Fact]
    public void Execute_MakesUndoAvailable()
    {
        SetPlanTitleCommand command = new("Renamed");

        _editor.Execute(command);

        Assert.True(_editor.CanUndo);
    }

    [Fact]
    public void Execute_RaisesChanged()
    {
        SetPlanTitleCommand command = new("Renamed");

        Assert.Raises<EventArgs>(
            handler => _editor.Changed += handler.Invoke,
            handler => _editor.Changed -= handler.Invoke,
            () => _editor.Execute(command));
    }

    [Fact]
    public void Execute_WhenCommandFails_KeepsCurrentPlan()
    {
        DeleteBranchCommand command = new(TestPlan.Unknown);

        Assert.Throws<KeyNotFoundException>(() => _editor.Execute(command));

        Assert.Same(_original, _editor.Current);
    }

    [Fact]
    public void Execute_WhenCommandFails_DoesNotRecordUndoStep()
    {
        DeleteBranchCommand command = new(TestPlan.Unknown);

        Assert.Throws<KeyNotFoundException>(() => _editor.Execute(command));

        Assert.False(_editor.CanUndo);
    }

    [Fact]
    public void Undo_AfterExecute_RestoresPreviousPlan()
    {
        _editor.Execute(new SetPlanTitleCommand("Renamed"));

        bool undone = _editor.Undo();

        Assert.True(undone);
        Assert.Same(_original, _editor.Current);
    }

    [Fact]
    public void Undo_AfterExecute_MakesRedoAvailable()
    {
        _editor.Execute(new SetPlanTitleCommand("Renamed"));

        _editor.Undo();

        Assert.True(_editor.CanRedo);
    }

    [Fact]
    public void Undo_WithoutHistory_ReturnsFalse()
    {
        bool undone = _editor.Undo();

        Assert.False(undone);
    }

    [Fact]
    public void Redo_AfterUndo_ReappliesUndoneChange()
    {
        _editor.Execute(new SetPlanTitleCommand("Renamed"));
        _editor.Undo();

        bool redone = _editor.Redo();

        Assert.True(redone);
        Assert.Equal("Renamed", _editor.Current.Title);
    }

    [Fact]
    public void Redo_WithoutUndoneChange_ReturnsFalse()
    {
        bool redone = _editor.Redo();

        Assert.False(redone);
    }

    [Fact]
    public void Execute_AfterUndo_DiscardsRedoHistory()
    {
        _editor.Execute(new SetPlanTitleCommand("First"));
        _editor.Undo();

        _editor.Execute(new SetPlanTitleCommand("Second"));

        Assert.False(_editor.CanRedo);
    }

    [Fact]
    public void Undo_AfterCascadingDelete_RestoresEverythingThatWasRemoved()
    {
        _editor.Execute(new DeleteBranchCommand(TestPlan.FeatureA));

        _editor.Undo();

        Assert.Same(_original, _editor.Current);
    }
}
