namespace IntegrationPlanner.Jira;

public interface IJiraClient
{
    Task<string> GetCurrentUserAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraIssue>> SearchIssuesAsync(string query, CancellationToken cancellationToken);

    Task<JiraIssue> GetIssueAsync(string key, CancellationToken cancellationToken);
}
