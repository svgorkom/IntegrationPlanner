using System.IO;
using Microsoft.Extensions.Logging;

namespace IntegrationPlanner.UI.Logging;

public sealed record FileLoggerOptions(string Directory, int RetainedFileCount = 7, LogLevel MinimumLevel = LogLevel.Information)
{
    public static string DefaultDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IntegrationPlanner", "logs");
}
