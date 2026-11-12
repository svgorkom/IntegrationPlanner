using System.IO;

namespace IntegrationPlanner.UI.Tests.Fakes;

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "IntegrationPlanner.UI.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string[] FileNames() => [.. Directory.GetFiles(Path).Select(file => System.IO.Path.GetFileName(file)).Order(StringComparer.Ordinal)];

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
