namespace IntegrationPlanner.Persistence.Tests;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "IntegrationPlanner.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public string[] TemporaryFiles() => Directory.GetFiles(Path, "*.tmp");

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
