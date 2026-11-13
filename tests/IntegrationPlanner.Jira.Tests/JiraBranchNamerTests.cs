namespace IntegrationPlanner.Jira.Tests;

public sealed class JiraBranchNamerTests
{
    private static JiraIssue Issue(string summary) => new("AD-123", summary, "Open", new Uri("https://jira.example.com/browse/AD-123"));

    [Theory]
    [InlineData("Fix login page", "feature/AD-123-fix-login-page")]
    [InlineData("  Fix: the (login) -- page!  ", "feature/AD-123-fix-the-login-page")]
    [InlineData("UPPER Case", "feature/AD-123-upper-case")]
    [InlineData("???", "feature/AD-123")]
    [InlineData("", "feature/AD-123")]
    public void Suggest_BuildsAReadableName(string summary, string expected)
    {
        Assert.Equal(expected, JiraBranchNamer.Suggest(Issue(summary)));
    }

    [Fact]
    public void Suggest_TruncatesALongSummaryWithoutATrailingDash()
    {
        string name = JiraBranchNamer.Suggest(Issue("one two three four five six seven eight nine ten eleven"));

        Assert.Equal("feature/AD-123-one-two-three-four-five-six-seven-eight", name);
    }
}
