using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Tests.Editing;

public sealed class CompositePlanCommandTests
{
    private static readonly BranchId NewBranch = new("new-branch");

    [Fact]
    public void Apply_AppliesEveryCommandInOrder()
    {
        CompositePlanCommand command = new([
            new AddBranchCommand(new Branch(NewBranch, "AD-9", string.Empty, IsMainline: false, IsCancelled: false)),
            new SetLaneCommand(TestPlan.MainScenario, new Lane(NewBranch, 3, 0, null, false, null, null)),
        ]);

        Plan result = command.Apply(TestPlan.Create());

        Assert.Contains(result.Primary().Lanes, lane => lane.BranchId == NewBranch);
    }

    [Fact]
    public void Undo_AfterComposite_RestoresThePlanInOneStep()
    {
        Plan original = TestPlan.Create();
        PlanEditor editor = new(original);
        editor.Execute(new CompositePlanCommand([new SetPlanTitleCommand("First"), new SetPlanTitleCommand("Second")]));

        editor.Undo();

        Assert.Same(original, editor.Current);
    }

    [Fact]
    public void Execute_WhenALaterCommandFails_KeepsThePlanUnchanged()
    {
        Plan original = TestPlan.Create();
        PlanEditor editor = new(original);
        CompositePlanCommand command = new([new SetPlanTitleCommand("Renamed"), new DeleteBranchCommand(TestPlan.Unknown)]);

        Assert.Throws<KeyNotFoundException>(() => editor.Execute(command));

        Assert.Same(original, editor.Current);
    }
}
