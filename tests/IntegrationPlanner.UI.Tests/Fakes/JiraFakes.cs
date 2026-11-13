using IntegrationPlanner.Jira;

namespace IntegrationPlanner.UI.Tests.Fakes;

internal sealed class FakeJiraSettingsStore : IJiraSettingsStore
{
    public JiraSettings Settings { get; set; } = JiraSettings.Default;

    public JiraSettings? Saved { get; private set; }

    public Exception? LoadFailure { get; set; }

    public Task<JiraSettings> LoadAsync(CancellationToken cancellationToken) =>
        LoadFailure is null ? Task.FromResult(Settings) : Task.FromException<JiraSettings>(LoadFailure);

    public Task SaveAsync(JiraSettings settings, CancellationToken cancellationToken)
    {
        Saved = settings;
        return Task.CompletedTask;
    }
}

internal sealed class FakeJiraTokenStore : IJiraTokenStore
{
    public string? Token { get; set; }

    public string? Load() => Token;

    public void Save(string apiToken) => Token = apiToken;

    public void Clear() => Token = null;
}

internal sealed class FakeJiraClientFactory : IJiraClientFactory
{
    public string User { get; set; } = "user";

    public List<JiraIssue> Issues { get; } = [];

    public string? LastQuery { get; private set; }

    public JiraException? Failure { get; set; }

    public IJiraClient Create(JiraConnectionSettings settings) => new FakeClient(this);

    private sealed class FakeClient(FakeJiraClientFactory owner) : IJiraClient
    {
        public Task<string> GetCurrentUserAsync(CancellationToken cancellationToken) =>
            owner.Failure is null ? Task.FromResult(owner.User) : Task.FromException<string>(owner.Failure);

        public Task<IReadOnlyList<JiraIssue>> SearchIssuesAsync(string query, CancellationToken cancellationToken)
        {
            owner.LastQuery = query;
            return owner.Failure is null
                ? Task.FromResult<IReadOnlyList<JiraIssue>>([.. owner.Issues])
                : Task.FromException<IReadOnlyList<JiraIssue>>(owner.Failure);
        }

        public Task<JiraIssue> GetIssueAsync(string key, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
