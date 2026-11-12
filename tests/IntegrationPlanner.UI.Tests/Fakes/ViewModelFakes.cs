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

    public Exception? SaveFailure { get; set; }

    public List<(Plan Plan, string Path)> Saves { get; } = [];

    public Task SaveAsync(Plan plan, string path, CancellationToken cancellationToken)
    {
        if (SaveFailure is not null)
        {
            return Task.FromException(SaveFailure);
        }

        Saves.Add((plan, path));
        return Task.CompletedTask;
    }
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

    public string? SaveTarget { get; set; }

    public string? LastSuggestedFileName { get; private set; }

    public int OpenPicks { get; private set; }

    public string? PickPlanToOpen()
    {
        OpenPicks++;
        return PlanToOpen;
    }

    public string? PickPlanSaveTarget(string suggestedFileName)
    {
        LastSuggestedFileName = suggestedFileName;
        return SaveTarget;
    }

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

    public SaveChangesAnswer SaveChangesAnswer { get; set; } = SaveChangesAnswer.Cancel;

    public int SaveChangesQuestions { get; private set; }

    public SaveChangesAnswer AskSaveChanges()
    {
        SaveChangesQuestions++;
        return SaveChangesAnswer;
    }

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
    public static readonly BranchId Main = new("main");
    public static readonly BranchId Feature = new("feature");
    public static readonly ConnectorId Pull = new("pull");
    public static readonly MarkerId Release = new("release");

    public static Plan TwoScenarios() =>
        Plan.Create("Plan: title") with
        {
            Scenarios = ImmutableList.Create(Scenario.Create(First, "First scenario", 4), Scenario.Create(Second, "Second scenario", 4)),
        };

    // Mainline "main" and branch "feature" in one scenario: a pull from main into feature and a marker on feature.
    public static Plan WithLanes() =>
        new(
            "Plan",
            [new Branch(Main, "main", string.Empty, IsMainline: true, IsCancelled: false), new Branch(Feature, "feature", string.Empty, IsMainline: false, IsCancelled: false)],
            [
                new Scenario(
                    First,
                    "First scenario",
                    6,
                    [new Lane(Main, 0, 0, null, false, null, null), new Lane(Feature, 1, 1, null, true, Main, 1)],
                    [new Connector(Pull, ConnectorKind.Pull, Main, Feature, 2, null, ProgressState.Planned)],
                    [new Marker(Release, MarkerKind.ReleaseBuild, "R1", new BranchStepAnchor(Feature, 3), MarkerDirection.FromTop, ProgressState.Done)]),
            ]);
}
