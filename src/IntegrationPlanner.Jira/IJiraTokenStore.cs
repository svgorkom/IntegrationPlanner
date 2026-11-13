namespace IntegrationPlanner.Jira;

public interface IJiraTokenStore
{
    string? Load();

    void Save(string apiToken);

    void Clear();
}
