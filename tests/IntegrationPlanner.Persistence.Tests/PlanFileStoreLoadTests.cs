using IntegrationPlanner.Core.Integrity;
using IntegrationPlanner.Core.Model;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationPlanner.Persistence.Tests;

public sealed class PlanFileStoreLoadTests : IDisposable
{
    private const string NewerSchemaFile = """{ "schemaVersion": 2, "title": "From the future", "somethingNew": [] }""";

    private const string FileWithDanglingLane = """
        {
          "schemaVersion": 1,
          "title": "Plan",
          "branches": [ { "id": "develop", "name": "Develop", "isMainline": true } ],
          "scenarios": [
            {
              "id": "s1", "name": "Scenario", "stepCount": 4,
              "lanes": [
                { "branchId": "develop", "order": 0, "startStep": 0 },
                { "branchId": "deleted-branch", "order": 1, "startStep": 0 }
              ]
            }
          ]
        }
        """;

    private readonly TempDirectory _directory = new();
    private readonly PlanFileStore _store = new(NullLogger<PlanFileStore>.Instance);

    public static TheoryData<string, string> CorruptFiles => new()
    {
        { "invalid JSON", "{ \"schemaVersion\": 1, " },
        { "not an object", "[]" },
        { "missing schemaVersion", """{ "title": "Plan" }""" },
        { "schemaVersion zero", """{ "schemaVersion": 0, "title": "Plan" }""" },
        { "missing branch id", """{ "schemaVersion": 1, "branches": [ { "name": "Develop" } ] }""" },
        { "unknown connector kind", """{ "schemaVersion": 1, "scenarios": [ { "id": "s", "name": "S", "stepCount": 1, "connectors": [ { "id": "c", "kind": "rebase", "fromBranchId": "a", "toBranchId": "b", "step": 0, "state": "done" } ] } ] }""" },
        { "marker without anchor", """{ "schemaVersion": 1, "scenarios": [ { "id": "s", "name": "S", "stepCount": 1, "markers": [ { "id": "m", "kind": "testing", "direction": "fromTop", "state": "done" } ] } ] }""" },
    };

    [Theory]
    [MemberData(nameof(CorruptFiles))]
    public async Task LoadAsync_CorruptFile_FailsAsCorrupt(string description, string content)
    {
        string path = await WriteAsync($"{description}.iplan.json", content);

        PlanLoadResult result = await _store.LoadAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(PlanLoadError.Corrupt, result.Error);
    }

    [Fact]
    public async Task LoadAsync_MarkerWithLegacyDirection_UsesItsAngleAndDefaultLength()
    {
        const string legacyFile = """{ "schemaVersion": 1, "branches": [ { "id": "develop", "name": "Develop" } ], "scenarios": [ { "id": "s", "name": "S", "stepCount": 2, "lanes": [ { "branchId": "develop", "order": 0, "startStep": 0 } ], "markers": [ { "id": "m", "kind": "testing", "anchor": { "branchId": "develop", "step": 1 }, "direction": "fromBottom", "state": "done" } ] } ] }""";
        string path = await WriteAsync("legacy.iplan.json", legacyFile);

        PlanLoadResult result = await _store.LoadAsync(path, TestContext.Current.CancellationToken);

        Marker marker = Assert.Single(Assert.Single(result.Plan!.Scenarios).Markers);
        Assert.Equal(MarkerDirection.FromBottom, marker.Angle);
        Assert.Equal(Marker.DefaultLength, marker.Length);
    }

    [Fact]
    public async Task LoadAsync_NewerSchemaVersion_FailsAsUnsupported()
    {
        string path = await WriteAsync("newer.iplan.json", NewerSchemaFile);

        PlanLoadResult result = await _store.LoadAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(PlanLoadError.UnsupportedSchemaVersion, result.Error);
    }

    [Fact]
    public async Task LoadAsync_NewerSchemaVersion_LeavesFileUntouched()
    {
        string path = await WriteAsync("newer.iplan.json", NewerSchemaFile);

        await _store.LoadAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(NewerSchemaFile, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoadAsync_DanglingReference_IsDroppedFromThePlan()
    {
        string path = await WriteAsync("dangling.iplan.json", FileWithDanglingLane);

        PlanLoadResult result = await _store.LoadAsync(path, TestContext.Current.CancellationToken);

        Assert.Single(Assert.Single(result.Plan!.Scenarios).Lanes);
    }

    [Fact]
    public async Task LoadAsync_DanglingReference_IsReported()
    {
        string path = await WriteAsync("dangling.iplan.json", FileWithDanglingLane);

        PlanLoadResult result = await _store.LoadAsync(path, TestContext.Current.CancellationToken);

        DroppedReference dropped = Assert.Single(result.DroppedReferences);
        Assert.Equal((DroppedItemKind.Lane, "deleted-branch"), (dropped.ItemKind, dropped.MissingId));
    }

    [Fact]
    public async Task LoadAsync_MissingFile_Throws()
    {
        string path = _directory.File("missing.iplan.json");

        await Assert.ThrowsAsync<FileNotFoundException>(() => _store.LoadAsync(path, TestContext.Current.CancellationToken));
    }

    public void Dispose() => _directory.Dispose();

    private async Task<string> WriteAsync(string name, string content)
    {
        string path = _directory.File(name);
        await File.WriteAllTextAsync(path, content, TestContext.Current.CancellationToken);
        return path;
    }
}
