using System.Text.RegularExpressions;

namespace IntegrationPlanner.Jira;

public static partial class JiraBranchNamer
{
    private const string Prefix = "feature/";
    private const int MaxSlugLength = 40;

    public static string Suggest(JiraIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        string slug = NonAlphanumeric().Replace(issue.Summary.ToLowerInvariant(), "-").Trim('-');
        if (slug.Length > MaxSlugLength)
        {
            slug = slug[..MaxSlugLength].TrimEnd('-');
        }

        return slug.Length == 0 ? Prefix + issue.Key : $"{Prefix}{issue.Key}-{slug}";
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
