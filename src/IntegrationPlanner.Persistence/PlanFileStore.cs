using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntegrationPlanner.Core.Integrity;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Persistence.Documents;
using Microsoft.Extensions.Logging;

namespace IntegrationPlanner.Persistence;

public sealed class PlanFileStore : IPlanFileStore
{
    public const int CurrentSchemaVersion = 1;

    private const int BufferSize = 4096;
    private const string SchemaVersionProperty = "schemaVersion";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        // Plan files are read by people and diffed in git, never embedded in HTML.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    private readonly ILogger<PlanFileStore> _logger;

    public PlanFileStore(ILogger<PlanFileStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task<PlanLoadResult> LoadAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);

        await using FileStream stream = new(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous);
        PlanLoadResult result;
        try
        {
            using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            result = Read(json);
        }
        catch (Exception exception) when (exception is JsonException or PlanFormatException)
        {
            _logger.LogWarning(exception, "{Operation} rejected the file: {Error}", nameof(LoadAsync), PlanLoadError.Corrupt);
            return PlanLoadResult.Failure(PlanLoadError.Corrupt);
        }

        LogLoadOutcome(result);
        return result;
    }

    public async Task SaveAsync(Plan plan, string path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException($"'{path}' has no parent directory.", nameof(path));

        // Same directory as the target so the final move is a rename on the same volume, never a copy.
        string tempPath = Path.Combine(directory, $"{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        PlanDocument document = PlanDocumentMapper.ToDocument(plan, CurrentSchemaVersion);
        bool replaced = false;
        try
        {
            await using (FileStream stream = new(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(tempPath, fullPath, overwrite: true);
            replaced = true;
            _logger.LogInformation("{Operation} completed", nameof(SaveAsync));
        }
        finally
        {
            if (!replaced)
            {
                DeleteTempFile(tempPath);
            }
        }
    }

    private static PlanLoadResult Read(JsonDocument json)
    {
        if (json.RootElement.ValueKind != JsonValueKind.Object
            || !json.RootElement.TryGetProperty(SchemaVersionProperty, out JsonElement versionElement)
            || !versionElement.TryGetInt32(out int schemaVersion)
            || schemaVersion < 1)
        {
            return PlanLoadResult.Failure(PlanLoadError.Corrupt);
        }

        if (schemaVersion > CurrentSchemaVersion)
        {
            return PlanLoadResult.Failure(PlanLoadError.UnsupportedSchemaVersion);
        }

        PlanDocument document = json.Deserialize<PlanDocument>(SerializerOptions)
            ?? throw new PlanFormatException("The plan document is empty.");
        DanglingReferenceRemoval removal = DanglingReferenceRemover.Remove(PlanDocumentMapper.ToPlan(document));
        return PlanLoadResult.Success(removal.Plan, removal.DroppedReferences);
    }

    private void LogLoadOutcome(PlanLoadResult result)
    {
        if (!result.IsSuccess)
        {
            _logger.LogWarning("{Operation} rejected the file: {Error}", nameof(LoadAsync), result.Error);
            return;
        }

        if (result.DroppedReferences.Count > 0)
        {
            _logger.LogWarning("{Operation} dropped {Count} dangling references", nameof(LoadAsync), result.DroppedReferences.Count);
            return;
        }

        _logger.LogInformation("{Operation} completed", nameof(LoadAsync));
    }

    private void DeleteTempFile(string tempPath)
    {
        try
        {
            File.Delete(tempPath);
        }
        catch (IOException exception)
        {
            // The original save failure is the one that matters to the caller; a leftover temp file is only logged.
            _logger.LogWarning(exception, "{Operation} could not remove its temporary file", nameof(SaveAsync));
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "{Operation} could not remove its temporary file", nameof(SaveAsync));
        }
    }
}
