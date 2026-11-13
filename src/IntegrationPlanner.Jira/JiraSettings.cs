namespace IntegrationPlanner.Jira;

public sealed record JiraSettings(string BaseUrl, string ProjectKey, string MainlineBranchName)
{
    public const string DefaultMainlineBranchName = "develop";

    public static JiraSettings Default { get; } = new(string.Empty, string.Empty, DefaultMainlineBranchName);

    public bool TryCreateConnection(string personalAccessToken, out JiraConnectionSettings? connection)
    {
        connection = null;
        if (string.IsNullOrWhiteSpace(personalAccessToken))
        {
            return false;
        }

        if (!Uri.TryCreate(BaseUrl.Trim(), UriKind.Absolute, out Uri? url)
            || (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        connection = new JiraConnectionSettings(url, personalAccessToken.Trim(), string.IsNullOrWhiteSpace(ProjectKey) ? null : ProjectKey.Trim());
        return true;
    }
}
