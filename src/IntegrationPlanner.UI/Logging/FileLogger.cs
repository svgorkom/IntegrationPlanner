using Microsoft.Extensions.Logging;

namespace IntegrationPlanner.UI.Logging;

public sealed class FileLogger : ILogger
{
    private readonly string _category;
    private readonly FileLogWriter _writer;
    private readonly LogLevel _minimumLevel;

    public FileLogger(string category, FileLogWriter writer, LogLevel minimumLevel)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(writer);
        _category = category;
        _writer = writer;
        _minimumLevel = minimumLevel;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _minimumLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string message = $"[{logLevel}] {_category}: {formatter(state, exception)}";
        _writer.Write(exception is null ? message : $"{message}{Environment.NewLine}{exception}");
    }
}
