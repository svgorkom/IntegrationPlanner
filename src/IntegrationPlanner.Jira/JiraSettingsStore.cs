using System.Text.Json;

namespace IntegrationPlanner.Jira;

public sealed class JiraSettingsStore : IJiraSettingsStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _path;

    public JiraSettingsStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
    }

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IntegrationPlanner",
        "jira.json");

    public async Task<JiraSettings> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            return JiraSettings.Default;
        }

        Stream stream = File.OpenRead(_path);
        await using (stream.ConfigureAwait(false))
        {
            try
            {
                JiraSettings? settings = await JsonSerializer.DeserializeAsync<JiraSettings>(stream, Options, cancellationToken).ConfigureAwait(false);
                return Normalize(settings);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("The Jira settings file is not valid.", exception);
            }
        }
    }

    public async Task SaveAsync(JiraSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        Stream stream = File.Create(_path);
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, settings, Options, cancellationToken).ConfigureAwait(false);
        }
    }

    // Missing or null properties (hand-edited file) fall back to defaults.
    private static JiraSettings Normalize(JiraSettings? settings) =>
        settings is null
            ? JiraSettings.Default
            : new JiraSettings(
                settings.BaseUrl ?? string.Empty,
                settings.ProjectKey ?? string.Empty,
                string.IsNullOrWhiteSpace(settings.MainlineBranchName) ? JiraSettings.DefaultMainlineBranchName : settings.MainlineBranchName);
}
