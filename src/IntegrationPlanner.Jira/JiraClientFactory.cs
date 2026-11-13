namespace IntegrationPlanner.Jira;

public interface IJiraClientFactory
{
    IJiraClient Create(JiraConnectionSettings settings);
}

public sealed class JiraClientFactory : IJiraClientFactory
{
    private static readonly HttpClient SharedHttp = new() { Timeout = TimeSpan.FromSeconds(30) };

    public IJiraClient Create(JiraConnectionSettings settings) => new JiraClient(SharedHttp, settings);
}
