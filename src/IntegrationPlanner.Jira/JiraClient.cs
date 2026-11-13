using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace IntegrationPlanner.Jira;

public sealed class JiraClient : IJiraClient
{
    private const int MaxResults = 50;
    private const string IssueFields = "summary,status";

    private readonly HttpClient _http;
    private readonly string? _projectKey;
    private readonly Uri _baseUrl;
    private readonly AuthenticationHeaderValue _authorization;

    public JiraClient(HttpClient http, JiraConnectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(settings);
        _http = http;
        _projectKey = settings.ProjectKey;
        _baseUrl = new Uri(settings.BaseUrl.AbsoluteUri.TrimEnd('/') + "/");
        _authorization = new AuthenticationHeaderValue("Bearer", settings.PersonalAccessToken);
    }

    public async Task<string> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        using JsonDocument document = await GetJsonAsync("rest/api/2/myself", cancellationToken).ConfigureAwait(false);
        return ReadString(document.RootElement, "displayName");
    }

    public async Task<IReadOnlyList<JiraIssue>> SearchIssuesAsync(string query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        string jql = JiraQueryBuilder.Build(query, _projectKey);
        string path = $"rest/api/2/search?jql={Uri.EscapeDataString(jql)}&fields={IssueFields}&maxResults={MaxResults}";
        using JsonDocument document = await GetJsonAsync(path, cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("issues", out JsonElement issues) || issues.ValueKind != JsonValueKind.Array)
        {
            throw new JiraException(JiraFailure.InvalidResponse, "The Jira response contains no issue list.");
        }

        return [.. issues.EnumerateArray().Select(ToIssue)];
    }

    public async Task<JiraIssue> GetIssueAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!JiraQueryBuilder.IsIssueKey(key.Trim()))
        {
            throw new ArgumentException("Not a valid Jira issue key.", nameof(key));
        }

        using JsonDocument document = await GetJsonAsync($"rest/api/2/issue/{Uri.EscapeDataString(key.Trim())}?fields={IssueFields}", cancellationToken).ConfigureAwait(false);
        return ToIssue(document.RootElement);
    }

    private async Task<JsonDocument> GetJsonAsync(string path, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri(_baseUrl, path));
        request.Headers.Authorization = _authorization;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        try
        {
            response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw new JiraException(JiraFailure.Unreachable, "Jira could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new JiraException(JiraFailure.Unreachable, "Jira did not respond in time.", exception);
        }

        using (response)
        {
            ThrowIfFailed(response);
            try
            {
                Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using (stream.ConfigureAwait(false))
                {
                    return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            catch (JsonException exception)
            {
                throw new JiraException(JiraFailure.InvalidResponse, "Jira returned an unreadable response.", exception);
            }
        }
    }

    private static void ThrowIfFailed(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        JiraFailure failure = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => JiraFailure.Unauthorized,
            HttpStatusCode.NotFound => JiraFailure.NotFound,
            HttpStatusCode.BadRequest => JiraFailure.InvalidRequest,
            _ => JiraFailure.ServerError,
        };
        throw new JiraException(failure, $"Jira answered {(int)response.StatusCode} {response.ReasonPhrase}.");
    }

    private JiraIssue ToIssue(JsonElement element)
    {
        string key = ReadString(element, "key");
        if (!element.TryGetProperty("fields", out JsonElement fields) || fields.ValueKind != JsonValueKind.Object)
        {
            throw new JiraException(JiraFailure.InvalidResponse, "The Jira issue contains no fields.");
        }

        string status = fields.TryGetProperty("status", out JsonElement statusElement) && statusElement.ValueKind == JsonValueKind.Object
            ? ReadString(statusElement, "name")
            : string.Empty;
        return new JiraIssue(key, ReadString(fields, "summary"), status, new Uri(_baseUrl, $"browse/{key}"));
    }

    private static string ReadString(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        throw new JiraException(JiraFailure.InvalidResponse, $"The Jira response lacks '{property}'.");
    }
}
