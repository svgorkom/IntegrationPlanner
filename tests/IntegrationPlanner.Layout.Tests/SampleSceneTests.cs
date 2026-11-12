using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationPlanner.Layout.Tests;

public sealed class SampleSceneTests
{
    private static readonly string SamplePath = Path.Combine(AppContext.BaseDirectory, "Samples", "scenario3.iplan.json");
    private static readonly ScenarioId Scenario3 = new("scenario-3");

    private readonly DiagramLayoutEngine _engine = new(new FixedWidthTextMeasurer(), TestPlan.Options);

    [Fact]
    public async Task Scenario3_HasNoOverlappingLabels()
    {
        Plan plan = await LoadSampleAsync();

        DiagramScene scene = _engine.Layout(plan, Scenario3);

        Assert.Empty(scene.OverlappingLabels());
    }

    [Fact]
    public async Task Scenario3_IsLaidOutIdenticallyEveryTime()
    {
        Plan plan = await LoadSampleAsync();

        string first = SceneSnapshot.Write(_engine.Layout(plan, Scenario3));
        string second = SceneSnapshot.Write(_engine.Layout(plan, Scenario3));

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Scenario3_MatchesTheGoldenScene()
    {
        Plan plan = await LoadSampleAsync();

        string actual = SceneSnapshot.Write(_engine.Layout(plan, Scenario3)).ReplaceLineEndings();

        Assert.Equal(GoldenFile.Read("scenario3.scene.txt", actual), actual);
    }

    private static async Task<Plan> LoadSampleAsync()
    {
        PlanLoadResult result = await new PlanFileStore(NullLogger<PlanFileStore>.Instance).LoadAsync(SamplePath, TestContext.Current.CancellationToken);
        return result.Plan ?? throw new InvalidOperationException($"{nameof(SamplePath)} did not load: {result.Error}");
    }
}
