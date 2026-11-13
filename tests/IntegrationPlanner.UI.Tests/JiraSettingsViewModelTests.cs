using System.IO;
using IntegrationPlanner.Jira;
using IntegrationPlanner.UI.Localization;
using IntegrationPlanner.UI.Tests.Fakes;
using IntegrationPlanner.UI.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationPlanner.UI.Tests;

public sealed class JiraSettingsViewModelTests
{
    private readonly FakeJiraSettingsStore _settingsStore = new();
    private readonly FakeJiraTokenStore _tokenStore = new();
    private readonly FakeJiraClientFactory _clientFactory = new();
    private readonly FakeMessageService _messages = new();

    [Fact]
    public async Task LoadAsync_PopulatesFieldsFromStore()
    {
        // Arrange
        _settingsStore.Settings = new JiraSettings("https://jira.example.com", "AD", "develop");
        JiraSettingsViewModel viewModel = Create();

        // Act
        await viewModel.LoadAsync(CancellationToken.None);

        // Assert
        Assert.Equal("https://jira.example.com", viewModel.BaseUrl);
        Assert.Equal("AD", viewModel.ProjectKey);
    }

    [Fact]
    public async Task LoadAsync_CorruptFile_ShowsError()
    {
        // Arrange
        _settingsStore.LoadFailure = new InvalidDataException("bad");
        JiraSettingsViewModel viewModel = Create();

        // Act
        await viewModel.LoadAsync(CancellationToken.None);

        // Assert
        Assert.Equal([UiText.ErrorJiraSettingsLoad], _messages.Errors);
    }

    [Fact]
    public async Task Save_StoresTokenAndSettingsAndCloses()
    {
        // Arrange
        JiraSettingsViewModel viewModel = Create();
        viewModel.BaseUrl = "https://jira.example.com";
        viewModel.ApiToken = "secret";
        bool? closed = null;
        viewModel.CloseRequested += (_, saved) => closed = saved;

        // Act
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("secret", _tokenStore.Token);
        Assert.Equal("https://jira.example.com", _settingsStore.Saved?.BaseUrl);
        Assert.True(closed);
    }

    [Fact]
    public async Task Save_EmptyToken_KeepsStoredToken()
    {
        // Arrange
        _tokenStore.Token = "existing";
        JiraSettingsViewModel viewModel = Create();

        // Act
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("existing", _tokenStore.Token);
    }

    [Fact]
    public async Task Save_BlankMainline_DoesNotSave()
    {
        // Arrange
        JiraSettingsViewModel viewModel = Create();
        viewModel.MainlineBranchName = " ";

        // Act
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Null(_settingsStore.Saved);
        Assert.Equal(UiText.JiraMainlineRequired, viewModel.StatusMessage);
    }

    [Fact]
    public async Task TestConnection_Incomplete_ReportsIncomplete()
    {
        // Arrange
        JiraSettingsViewModel viewModel = Create();

        // Act
        await viewModel.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(UiText.JiraSettingsIncomplete, viewModel.StatusMessage);
    }

    [Fact]
    public async Task TestConnection_Success_ReportsUser()
    {
        // Arrange
        _clientFactory.User = "Ada";
        JiraSettingsViewModel viewModel = CreateComplete();

        // Act
        await viewModel.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(UiText.Format(UiText.JiraConnectedFormat, "Ada"), viewModel.StatusMessage);
    }

    [Fact]
    public async Task TestConnection_Unauthorized_ReportsUnauthorized()
    {
        // Arrange
        _clientFactory.Failure = new JiraException(JiraFailure.Unauthorized, "401");
        JiraSettingsViewModel viewModel = CreateComplete();

        // Act
        await viewModel.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(UiText.JiraFailureUnauthorized, viewModel.StatusMessage);
    }

    private JiraSettingsViewModel CreateComplete()
    {
        JiraSettingsViewModel viewModel = Create();
        viewModel.BaseUrl = "https://jira.example.com";
        viewModel.ApiToken = "secret";
        return viewModel;
    }

    private JiraSettingsViewModel Create() =>
        new(_settingsStore, _tokenStore, _clientFactory, _messages, NullLogger<JiraSettingsViewModel>.Instance);
}
