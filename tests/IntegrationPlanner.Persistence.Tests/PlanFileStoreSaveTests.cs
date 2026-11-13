using IntegrationPlanner.Core.Model;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationPlanner.Persistence.Tests;

public sealed class PlanFileStoreSaveTests : IDisposable
{
    private readonly TempDirectory _directory = new();
    private readonly PlanFileStore _store = new(NullLogger<PlanFileStore>.Instance);

    [Fact]
    public async Task SavedPlan_LoadsBackEquivalent()
    {
        Plan plan = SamplePlan.Create();
        string path = _directory.File("plan.iplan.json");
        await _store.SaveAsync(plan, path, TestContext.Current.CancellationToken);

        PlanLoadResult result = await _store.LoadAsync(path, TestContext.Current.CancellationToken);

        Assert.Equivalent(plan, result.Plan, strict: true);
    }

    [Theory]
    [InlineData("\"schemaVersion\": 1")]
    [InlineData("\"isMainline\": true")]
    [InlineData("\"kind\": \"releaseBuild\"")]
    [InlineData("\"angle\": 180")]
    [InlineData("Feature \\\"A\\\" with ^2 and <tags>")]
    public async Task SaveAsync_WritesReadableCamelCaseJson(string expectedFragment)
    {
        string path = _directory.File("plan.iplan.json");

        await _store.SaveAsync(SamplePlan.Create(), path, TestContext.Current.CancellationToken);

        Assert.Contains(expectedFragment, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_OverExistingFile_ReplacesItsContent()
    {
        string path = _directory.File("plan.iplan.json");
        await File.WriteAllTextAsync(path, "old content", TestContext.Current.CancellationToken);

        await _store.SaveAsync(SamplePlan.Create(), path, TestContext.Current.CancellationToken);

        Assert.StartsWith("{", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_WhenSuccessful_LeavesNoTemporaryFile()
    {
        string path = _directory.File("plan.iplan.json");

        await _store.SaveAsync(SamplePlan.Create(), path, TestContext.Current.CancellationToken);

        Assert.Empty(_directory.TemporaryFiles());
    }

    [Fact]
    public async Task SaveAsync_WhenReplacingFails_LeavesNoTemporaryFile()
    {
        string pathOccupiedByDirectory = _directory.File("plan.iplan.json");
        Directory.CreateDirectory(pathOccupiedByDirectory);

        await Record.ExceptionAsync(() => _store.SaveAsync(SamplePlan.Create(), pathOccupiedByDirectory, TestContext.Current.CancellationToken));

        Assert.Empty(_directory.TemporaryFiles());
    }

    [Fact]
    public async Task SaveAsync_WhenReplacingFails_Throws()
    {
        string pathOccupiedByDirectory = _directory.File("plan.iplan.json");
        Directory.CreateDirectory(pathOccupiedByDirectory);

        Exception? exception = await Record.ExceptionAsync(() => _store.SaveAsync(SamplePlan.Create(), pathOccupiedByDirectory, TestContext.Current.CancellationToken));

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task SaveAsync_WhenCancelled_LeavesOriginalFileIntact()
    {
        string path = _directory.File("plan.iplan.json");
        await File.WriteAllTextAsync(path, "original", TestContext.Current.CancellationToken);
        CancellationToken cancelled = new(canceled: true);

        await Record.ExceptionAsync(() => _store.SaveAsync(SamplePlan.Create(), path, cancelled));

        Assert.Equal("original", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_WhenCancelled_ThrowsOperationCanceled()
    {
        string path = _directory.File("plan.iplan.json");
        CancellationToken cancelled = new(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _store.SaveAsync(SamplePlan.Create(), path, cancelled));
    }

    public void Dispose() => _directory.Dispose();
}
