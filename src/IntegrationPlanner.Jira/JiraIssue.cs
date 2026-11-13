namespace IntegrationPlanner.Jira;

public sealed record JiraIssue(string Key, string Summary, string Status, Uri Url);
