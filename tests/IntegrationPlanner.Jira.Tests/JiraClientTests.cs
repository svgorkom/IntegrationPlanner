using System.Net;

namespace IntegrationPlanner.Jira.Tests;

public sealed class JiraClientTests
{
    private const string SearchJson = """
        {"issues":[{"key":"AD-7","fields":{"summary":"Fix login","status":{"name":"In Progress"}}}]}
        """;

    private static JiraClient CreateClient(StubHandler handler) =>
        new(new HttpClient(handler), new JiraConnectionSettings(new Uri("https://jira.example.com/jira"), "token", "AD"));

    [Fact]
    public async Task SearchIssues_ReturnsTheMatchingIssues()
    {
        JiraClient client = CreateClient(StubHandler.Json(SearchJson));

        IReadOnlyList<JiraIssue> issues = await client.SearchIssuesAsync("login", TestContext.Current.CancellationToken);

        JiraIssue issue = Assert.Single(issues);
        Assert.Equal(new JiraIssue("AD-7", "Fix login", "In Progress", new Uri("https://jira.example.com/jira/browse/AD-7")), issue);
    }

    [Fact]
    public async Task SearchIssues_SendsBearerTokenOnTheV2ApiAndTheProjectFilter()
    {
        StubHandler handler = StubHandler.Json(SearchJson);

        await CreateClient(handler).SearchIssuesAsync("login", TestContext.Current.CancellationToken);

        Assert.Equal("Bearer token", handler.LastRequest!.Headers.Authorization!.ToString());
        Assert.StartsWith("/jira/rest/api/2/search", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("project", Uri.UnescapeDataString(handler.LastRequest.RequestUri!.Query), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetIssue_ReturnsTheIssue()
    {
        JiraClient client = CreateClient(StubHandler.Json("""{"key":"AD-7","fields":{"summary":"Fix login","status":{"name":"Done"}}}"""));

        JiraIssue issue = await client.GetIssueAsync("AD-7", TestContext.Current.CancellationToken);

        Assert.Equal("Done", issue.Status);
    }

    [Fact]
    public async Task GetIssue_WithAnInvalidKey_Throws()
    {
        JiraClient client = CreateClient(StubHandler.Json("{}"));

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetIssueAsync("not a key", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, JiraFailure.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, JiraFailure.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, JiraFailure.NotFound)]
    [InlineData(HttpStatusCode.BadRequest, JiraFailure.InvalidRequest)]
    [InlineData(HttpStatusCode.InternalServerError, JiraFailure.ServerError)]
    public async Task FailedResponse_IsReportedAsTheMatchingFailure(HttpStatusCode status, JiraFailure expected)
    {
        JiraClient client = CreateClient(StubHandler.Json("{}", status));

        JiraException exception = await Assert.ThrowsAsync<JiraException>(() => client.GetCurrentUserAsync(TestContext.Current.CancellationToken));

        Assert.Equal(expected, exception.Failure);
    }

    [Fact]
    public async Task UnreachableServer_IsReportedAsUnreachable()
    {
        JiraClient client = CreateClient(new StubHandler(_ => throw new HttpRequestException("offline")));

        JiraException exception = await Assert.ThrowsAsync<JiraException>(() => client.GetCurrentUserAsync(TestContext.Current.CancellationToken));

        Assert.Equal(JiraFailure.Unreachable, exception.Failure);
    }

    [Fact]
    public async Task Timeout_IsReportedAsUnreachable()
    {
        JiraClient client = CreateClient(new StubHandler(_ => throw new TaskCanceledException("timeout")));

        JiraException exception = await Assert.ThrowsAsync<JiraException>(() => client.GetCurrentUserAsync(TestContext.Current.CancellationToken));

        Assert.Equal(JiraFailure.Unreachable, exception.Failure);
    }

    [Fact]
    public async Task MalformedBody_IsReportedAsInvalidResponse()
    {
        JiraClient client = CreateClient(StubHandler.Json("not json"));

        JiraException exception = await Assert.ThrowsAsync<JiraException>(() => client.GetCurrentUserAsync(TestContext.Current.CancellationToken));

        Assert.Equal(JiraFailure.InvalidResponse, exception.Failure);
    }
}
