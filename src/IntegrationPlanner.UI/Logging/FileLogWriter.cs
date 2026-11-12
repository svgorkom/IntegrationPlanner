using System.Globalization;
using System.IO;

namespace IntegrationPlanner.UI.Logging;

// Single writer for all loggers: one lock serialises writes, day rollover and clean-up.
public sealed class FileLogWriter
{
    public const string FilePrefix = "integrationplanner-";
    public const string FileExtension = ".log";

    private readonly FileLoggerOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Action<string> _reportFailure;
    private readonly Lock _gate = new();
    private DateOnly? _currentDay;

    public FileLogWriter(FileLoggerOptions options, TimeProvider timeProvider, Action<string> reportFailure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(reportFailure);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Directory, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.RetainedFileCount, 1, nameof(options));
        _options = options;
        _timeProvider = timeProvider;
        _reportFailure = reportFailure;
    }

    public static string FileNameFor(DateOnly day) =>
        $"{FilePrefix}{day.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}{FileExtension}";

    public void Write(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (_gate)
        {
            // Logging must never take the application down; a failure is reported on the fallback channel instead.
            try
            {
                DateTimeOffset now = _timeProvider.GetLocalNow();
                DateOnly day = DateOnly.FromDateTime(now.DateTime);
                Directory.CreateDirectory(_options.Directory);
                if (_currentDay != day)
                {
                    _currentDay = day;
                    RemoveExpiredFiles(day);
                }

                string line = $"{now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture)} {message}{Environment.NewLine}";
                File.AppendAllText(Path.Combine(_options.Directory, FileNameFor(day)), line);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _reportFailure($"{nameof(FileLogWriter)} could not write to '{_options.Directory}': {exception.Message}");
            }
        }
    }

    private void RemoveExpiredFiles(DateOnly today)
    {
        string todayFile = FileNameFor(today);

        // Names embed yyyyMMdd, so ordinal order is chronological order.
        string[] expired = [.. Directory.GetFiles(_options.Directory, $"{FilePrefix}*{FileExtension}")
            .Where(path => !string.Equals(Path.GetFileName(path), todayFile, StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .SkipLast(_options.RetainedFileCount - 1)];
        foreach (string path in expired)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _reportFailure($"{nameof(FileLogWriter)} could not remove '{Path.GetFileName(path)}': {exception.Message}");
            }
        }
    }
}
