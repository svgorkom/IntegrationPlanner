namespace IntegrationPlanner.Jira;

public sealed record JiraConnectionSettings(Uri BaseUrl, string PersonalAccessToken, string? ProjectKey = null);
