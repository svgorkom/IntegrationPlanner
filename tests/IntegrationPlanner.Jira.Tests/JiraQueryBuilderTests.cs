namespace IntegrationPlanner.Jira.Tests;

public sealed class JiraQueryBuilderTests
{
    [Theory]
    [InlineData("ad-12", null, "key = AD-12 ORDER BY updated DESC")]
    [InlineData("login bug", null, "text ~ \"login bug\" ORDER BY updated DESC")]
    [InlineData("say \"hi\"", "AD", "text ~ \"say \\\"hi\\\"\" AND project = \"AD\" ORDER BY updated DESC")]
    public void Build_ProducesTheExpectedJql(string query, string? project, string expected)
    {
        Assert.Equal(expected, JiraQueryBuilder.Build(query, project));
    }
}
