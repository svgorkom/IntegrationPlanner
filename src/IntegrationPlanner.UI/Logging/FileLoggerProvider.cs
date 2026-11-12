using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace IntegrationPlanner.UI.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly FileLogWriter _writer;
    private readonly LogLevel _minimumLevel;

    public FileLoggerProvider(FileLoggerOptions options, TimeProvider timeProvider)
        : this(options, timeProvider, message => Debug.WriteLine(message))
    {
    }

    public FileLoggerProvider(FileLoggerOptions options, TimeProvider timeProvider, Action<string> reportFailure)
    {
        ArgumentNullException.ThrowIfNull(options);
        _writer = new FileLogWriter(options, timeProvider, reportFailure);
        _minimumLevel = options.MinimumLevel;
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _writer, _minimumLevel);

    public void Dispose()
    {
        // Nothing to release: every write opens and closes the file.
    }
}
