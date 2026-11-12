using System.IO;
using IntegrationPlanner.UI.Logging;
using IntegrationPlanner.UI.Tests.Fakes;

namespace IntegrationPlanner.UI.Tests;

public sealed class FileLogWriterTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 3, 10);

    private readonly TemporaryDirectory _directory = new();
    private readonly FixedTimeProvider _time = new(Day);
    private readonly List<string> _failures = [];

    [Fact]
    public void Write_AppendsTheMessageToTheFileOfTheDay()
    {
        FileLogWriter writer = CreateWriter();

        writer.Write("hello");

        Assert.EndsWith($"hello{Environment.NewLine}", File.ReadAllText(Path.Combine(_directory.Path, FileLogWriter.FileNameFor(Day))));
    }

    [Fact]
    public void Write_CreatesTheLogDirectoryWhenItIsMissing()
    {
        string nested = Path.Combine(_directory.Path, "logs");
        FileLogWriter writer = new(new FileLoggerOptions(nested), _time, _failures.Add);

        writer.Write("hello");

        Assert.True(File.Exists(Path.Combine(nested, FileLogWriter.FileNameFor(Day))));
    }

    [Fact]
    public void Write_StartsANewFileWhenTheDayChanges()
    {
        FileLogWriter writer = CreateWriter();
        writer.Write("first day");
        _time.MoveTo(Day.AddDays(1));

        writer.Write("second day");

        Assert.Equal([FileLogWriter.FileNameFor(Day), FileLogWriter.FileNameFor(Day.AddDays(1))], _directory.FileNames());
    }

    [Fact]
    public void Write_KeepsOnlyTheRetainedNumberOfFiles()
    {
        CreateOldLogFiles(5);
        FileLogWriter writer = new(new FileLoggerOptions(_directory.Path, RetainedFileCount: 3), _time, _failures.Add);

        writer.Write("today");

        Assert.Equal([FileLogWriter.FileNameFor(Day.AddDays(-2)), FileLogWriter.FileNameFor(Day.AddDays(-1)), FileLogWriter.FileNameFor(Day)], _directory.FileNames());
    }

    [Fact]
    public void Write_LeavesUnrelatedFilesAlone()
    {
        File.WriteAllText(Path.Combine(_directory.Path, "notes.txt"), "keep");
        FileLogWriter writer = new(new FileLoggerOptions(_directory.Path, RetainedFileCount: 1), _time, _failures.Add);

        writer.Write("today");

        Assert.Equal([FileLogWriter.FileNameFor(Day), "notes.txt"], _directory.FileNames());
    }

    [Fact]
    public void Write_ReportsAFailureInsteadOfThrowing()
    {
        string blocked = Path.Combine(_directory.Path, "blocked");
        File.WriteAllText(blocked, "a file where the directory should be");
        FileLogWriter writer = new(new FileLoggerOptions(blocked), _time, _failures.Add);

        writer.Write("hello");

        Assert.Single(_failures);
    }

    public void Dispose() => _directory.Dispose();

    private FileLogWriter CreateWriter() => new(new FileLoggerOptions(_directory.Path), _time, _failures.Add);

    private void CreateOldLogFiles(int count)
    {
        foreach (int daysAgo in Enumerable.Range(1, count))
        {
            File.WriteAllText(Path.Combine(_directory.Path, FileLogWriter.FileNameFor(Day.AddDays(-daysAgo))), "old");
        }
    }
}
