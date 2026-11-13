using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.UI.Tests.Fakes;
using IntegrationPlanner.UI.ViewModels.Properties;

namespace IntegrationPlanner.UI.Tests.Properties;

public sealed class PropertiesViewModelTests
{
    private readonly PlanEditor _editor = new(TestPlans.WithLanes());

    private Scenario Scenario => _editor.Current.Scenarios[0];

    [Fact]
    public void PlanTitle_ChangesThePlanTitle()
    {
        PlanPropertiesViewModel properties = new(_editor, TestPlans.First);

        properties.Title = "Renamed";

        Assert.Equal("Renamed", _editor.Current.Title);
    }

    [Fact]
    public void PlanStepCount_ChangesTheScenarioStepCount()
    {
        PlanPropertiesViewModel properties = new(_editor, TestPlans.First);

        properties.StepCount = 12;

        Assert.Equal(12, Scenario.StepCount);
    }

    [Fact]
    public void BranchName_ChangesTheCatalogueBranch()
    {
        BranchPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Feature);

        properties.Name = "feature/login";

        Assert.Equal("feature/login", _editor.Current.Branches.Find(branch => branch.Id == TestPlans.Feature)!.Name);
    }

    [Fact]
    public async Task OpenInJira_PassesTheTicketKeyToTheOpener()
    {
        _editor.Execute(new UpdateBranchCommand(_editor.Current.Branches.Find(branch => branch.Id == TestPlans.Feature)! with { JiraKey = "AD-7" }));
        string? opened = null;
        BranchPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Feature, key =>
        {
            opened = key;
            return Task.CompletedTask;
        });

        await properties.OpenInJiraCommand.ExecuteAsync(null);

        Assert.Equal(("AD-7", true), (opened, properties.HasJiraKey));
    }

    [Fact]
    public void OpenInJira_IsDisabledForABranchWithoutATicket()
    {
        BranchPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Feature, _ => Task.CompletedTask);

        Assert.False(properties.OpenInJiraCommand.CanExecute(null));
    }

    [Fact]
    public void BranchName_IsOneUndoStep()
    {
        Plan original = _editor.Current;
        BranchPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Feature);

        properties.Name = "feature/login";
        _editor.Undo();

        Assert.Equal((original, false), (_editor.Current, _editor.CanUndo));
    }

    [Fact]
    public void BranchOrder_ChangesTheLaneInTheCurrentScenario()
    {
        BranchPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Feature);

        properties.Order = 5;

        Assert.Equal(5, Scenario.Lanes.Find(lane => lane.BranchId == TestPlans.Feature)!.Order);
    }

    [Fact]
    public void BranchForkSourceNone_ClearsTheForkSource()
    {
        BranchPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Feature);

        properties.ForkFromId = string.Empty;

        Assert.Null(Scenario.Lanes.Find(lane => lane.BranchId == TestPlans.Feature)!.ForkFromBranchId);
    }

    [Fact]
    public void BranchNotInScenario_RemovesItsLane()
    {
        BranchPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Feature);

        properties.IsInScenario = false;

        Assert.False(Scenario.Lanes.Exists(lane => lane.BranchId == TestPlans.Feature));
    }

    [Fact]
    public void BranchForkSources_ExcludeTheBranchItself()
    {
        BranchPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Feature);

        Assert.DoesNotContain(properties.ForkSources, choice => choice.Id == TestPlans.Feature.Value);
    }

    [Fact]
    public void Refresh_LoadsOutsideChangesWithoutAddingAnUndoStep()
    {
        BranchPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Feature);
        _editor.Execute(new UpdateBranchCommand(_editor.Current.Branches[1] with { Name = "outside" }));

        properties.Refresh();
        _editor.Undo();

        Assert.Equal(("outside", false), (properties.Name, _editor.CanUndo));
    }

    [Fact]
    public void Refresh_ReportsADeletedBranch()
    {
        BranchPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Feature);
        _editor.Execute(new DeleteBranchCommand(TestPlans.Feature));

        bool exists = properties.Refresh();

        Assert.False(exists);
    }

    [Fact]
    public void ConnectorStep_ChangesTheConnector()
    {
        ConnectorPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Pull);

        properties.Step = 4;

        Assert.Equal(4, Scenario.Connectors[0].Step);
    }

    [Fact]
    public void ConnectorBlankLabel_RemovesTheLabel()
    {
        _editor.Execute(new UpdateConnectorCommand(TestPlans.First, Scenario.Connectors[0] with { Label = "sync" }));
        ConnectorPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Pull);

        properties.Label = string.Empty;

        Assert.Null(Scenario.Connectors[0].Label);
    }

    [Fact]
    public void MarkerConnectorAnchor_AnchorsToTheFirstConnector()
    {
        MarkerPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Release);

        properties.AnchorKind = MarkerAnchorKind.Connector;

        Assert.Equal(new ConnectorAnchor(TestPlans.Pull), Scenario.Markers[0].Anchor);
    }

    [Fact]
    public void MarkerAnchorStep_MovesTheMarkerAlongItsBranch()
    {
        MarkerPropertiesViewModel properties = new(_editor, TestPlans.First, TestPlans.Release);

        properties.AnchorStep = 5;

        Assert.Equal(new BranchStepAnchor(TestPlans.Feature, 5), Scenario.Markers[0].Anchor);
    }
}
