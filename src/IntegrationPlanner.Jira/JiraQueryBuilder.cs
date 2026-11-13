using System.Text.RegularExpressions;

namespace IntegrationPlanner.Jira;

public static partial class JiraQueryBuilder
{
    public static bool IsIssueKey(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return IssueKeyPattern().IsMatch(text);
    }

    public static string Build(string query, string? projectKey)
    {
        ArgumentNullException.ThrowIfNull(query);
        string trimmed = query.Trim();
        string condition = IsIssueKey(trimmed)
            ? $"key = {trimmed.ToUpperInvariant()}"
            : $"text ~ \"{Escape(trimmed)}\"";

        string project = string.IsNullOrWhiteSpace(projectKey) ? string.Empty : $" AND project = \"{Escape(projectKey.Trim())}\"";
        return $"{condition}{project} ORDER BY updated DESC";
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9_]+-\d+$")]
    private static partial Regex IssueKeyPattern();
}
