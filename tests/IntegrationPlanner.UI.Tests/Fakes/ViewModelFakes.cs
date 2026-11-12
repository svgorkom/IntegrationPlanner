using System.Collections.Immutable;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.Persistence;
using IntegrationPlanner.UI.Export;
using IntegrationPlanner.UI.Services;

namespace IntegrationPlanner.UI.Tests.Fakes;

internal sealed class FakePlanFileStore : IPlanFileStore
{
    public PlanLoadResult? Result { get; set; }

    public Exception? Failure { get; set; }

    public Task<PlanLoadResult> LoadAsync(string path, CancellationToken cancellationToken) =>
        Failure is null ? Task.FromResult(Result!) : Task.FromException<PlanLoadResult>(Failure);

    public Task SaveAsync(Plan plan, string path, CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class FakeLayoutEngine : IDiagramLayoutEngine
{
    public static readonly DiagramScene EmptyScene = new(new SceneRect(0, 0, 10, 10), [], [], []);

    public List<ScenarioId> LaidOut { get; } = [];

    public DiagramScene Layout(Plan plan, ScenarioId scenarioId)
    {
        LaidOut.Add(scenarioId);
        return EmptyScene;
    }
}

internal sealed class FakeFileDialogService : IFileDialogService
{
    public string? PlanToOpen { get; set; }

    public string? ExportTarget { get; set; }

    public string? LastSuggestedFileName { get; private set; }

    public string? PickPlanToOpen() => PlanToOpen;

    public string? PickExportTarget(ExportFormat format, string suggestedFileName)
    {
        LastSuggestedFileName = suggestedFileName;
        return ExportTarget;
    }
}

internal sealed class FakeMessageService : IMessageService
{
    public List<string> Errors { get; } = [];

    public List<string> Informations { get; } = [];

    public void ShowError(string message) => Errors.Add(message);

    public void ShowInformation(string message) => Informations.Add(message);
}

internal sealed class FakeSceneExporter : ISceneExporter
{
    public Exception? Failure { get; set; }

    public List<(ExportFormat Format, string Path)> Exports { get; } = [];

    public Task ExportAsync(DiagramScene scene, ExportFormat format, string path, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException(Failure);
        }

        Exports.Add((format, path));
        return Task.CompletedTask;
    }

    public void CopyToClipboard(DiagramScene scene)
    {
        if (Failure is not null)
        {
            throw Failure;
        }
    }
}

internal static class TestPlans
{
    public static readonly ScenarioId First = new("first");
    public static readonly ScenarioId Second = new("second");

    public static Plan TwoScenarios() =>
        Plan.Create("Plan: title") with
        {
            Scenarios = ImmutableList.Create(Scenario.Create(First, "First scenario", 4), Scenario.Create(Second, "Second scenario", 4)),
        };
}
