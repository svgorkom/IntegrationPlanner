namespace IntegrationPlanner.Jira.Tests;

public sealed class JiraSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    [Fact]
    public async Task Load_WithoutAFile_ReturnsTheDefaults()
    {
        JiraSettingsStore store = new(Path.Combine(_directory, "jira.json"));

        JiraSettings settings = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(JiraSettings.Default, settings);
    }

    [Fact]
    public async Task SavedSettings_LoadBackEqual()
    {
        JiraSettingsStore store = new(Path.Combine(_directory, "nested", "jira.json"));
        JiraSettings saved = new("https://jira.example.com", "AD", "main");
        await store.SaveAsync(saved, TestContext.Current.CancellationToken);

        JiraSettings loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(saved, loaded);
    }

    [Fact]
    public async Task Load_WithAnEmptyMainlineName_FallsBackToDevelop()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "jira.json");
        await File.WriteAllTextAsync(path, """{"baseUrl":"https://x.example.com","email":"a@b.c"}""", TestContext.Current.CancellationToken);

        JiraSettings loaded = await new JiraSettingsStore(path).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("develop", loaded.MainlineBranchName);
    }

    [Fact]
    public async Task Load_WithACorruptFile_Throws()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "jira.json");
        await File.WriteAllTextAsync(path, "{ nope", TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidDataException>(() => new JiraSettingsStore(path).LoadAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("https://jira.example.com", "token", true)]
    [InlineData("http://jira.example.com:8080/jira", "token", true)]
    [InlineData("not a url", "token", false)]
    [InlineData("ftp://jira.example.com", "token", false)]
    [InlineData("https://jira.example.com", " ", false)]
    public void TryCreateConnection_ValidatesTheInput(string url, string token, bool expected)
    {
        JiraSettings settings = new(url, string.Empty, "develop");

        Assert.Equal(expected, settings.TryCreateConnection(token, out _));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
