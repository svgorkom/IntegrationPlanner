namespace IntegrationPlanner.Jira;

public interface IJiraSettingsStore
{
    Task<JiraSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(JiraSettings settings, CancellationToken cancellationToken);
}
