using IntegrationPlanner.Jira;
using IntegrationPlanner.UI.Localization;
using IntegrationPlanner.UI.Tests.Fakes;
using IntegrationPlanner.UI.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationPlanner.UI.Tests;

public sealed class AddBranchFromTicketViewModelTests
{
    private static readonly JiraIssue Login = new("AD-7", "Fix login page", "Open", new Uri("https://jira.example.com/browse/AD-7"));

    private readonly FakeJiraSettingsStore _settingsStore = new() { Settings = new JiraSettings("https://jira.example.com", string.Empty, "develop") };
    private readonly FakeJiraTokenStore _tokenStore = new() { Token = "pat" };
    private readonly FakeJiraClientFactory _clientFactory = new();

    [Fact]
    public async Task Search_ListsTheFoundTickets()
    {
        // Arrange
        _clientFactory.Issues.Add(Login);
        AddBranchFromTicketViewModel viewModel = Create();
        viewModel.SearchText = " login ";

        // Act
        await viewModel.SearchCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal([Login], viewModel.Results);
        Assert.Equal("login", _clientFactory.LastQuery);
    }

    [Fact]
    public async Task Search_WithoutResults_ReportsNoTickets()
    {
        // Arrange
        AddBranchFromTicketViewModel viewModel = Create();
        viewModel.SearchText = "zzz";

        // Act
        await viewModel.SearchCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(UiText.JiraNoResults, viewModel.StatusMessage);
    }

    [Fact]
    public async Task Search_WithoutToken_ReportsIncompleteSettings()
    {
        // Arrange
        _tokenStore.Token = null;
        AddBranchFromTicketViewModel viewModel = Create();
        viewModel.SearchText = "login";

        // Act
        await viewModel.SearchCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(UiText.JiraSettingsIncomplete, viewModel.StatusMessage);
    }

    [Fact]
    public async Task Search_WhenJiraRejects_ReportsUnauthorized()
    {
        // Arrange
        _clientFactory.Failure = new JiraException(JiraFailure.Unauthorized, "401");
        AddBranchFromTicketViewModel viewModel = Create();
        viewModel.SearchText = "login";

        // Act
        await viewModel.SearchCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(UiText.JiraFailureUnauthorized, viewModel.StatusMessage);
    }

    [Fact]
    public void SelectingATicket_GeneratesTheBranchName()
    {
        // Arrange
        AddBranchFromTicketViewModel viewModel = Create();

        // Act
        viewModel.SelectedIssue = Login;

        // Assert
        Assert.Equal("feature/AD-7-fix-login-page", viewModel.BranchName);
        Assert.True(viewModel.AddCommand.CanExecute(null));
    }

    [Fact]
    public void EditedName_SurvivesChangingTheSelection()
    {
        // Arrange
        AddBranchFromTicketViewModel viewModel = Create();
        viewModel.SelectedIssue = Login;
        viewModel.BranchName = "bugfix/my-own-name";

        // Act
        viewModel.SelectedIssue = Login with { Key = "AD-8" };

        // Assert
        Assert.Equal("bugfix/my-own-name", viewModel.BranchName);
    }

    [Fact]
    public void ResetName_RestoresTheGeneratedName()
    {
        // Arrange
        AddBranchFromTicketViewModel viewModel = Create();
        viewModel.SelectedIssue = Login;
        viewModel.BranchName = "bugfix/my-own-name";

        // Act
        viewModel.ResetNameCommand.Execute(null);

        // Assert
        Assert.Equal("feature/AD-7-fix-login-page", viewModel.BranchName);
    }

    [Fact]
    public void Add_WithoutATicketOrName_IsDisabled()
    {
        // Arrange
        AddBranchFromTicketViewModel viewModel = Create();

        // Act
        bool withoutTicket = viewModel.AddCommand.CanExecute(null);
        viewModel.SelectedIssue = Login;
        viewModel.BranchName = " ";

        // Assert
        Assert.False(withoutTicket);
        Assert.False(viewModel.AddCommand.CanExecute(null));
    }

    [Fact]
    public void Add_RequestsCloseAsAccepted()
    {
        // Arrange
        AddBranchFromTicketViewModel viewModel = Create();
        viewModel.SelectedIssue = Login;
        bool? accepted = null;
        viewModel.CloseRequested += (_, value) => accepted = value;

        // Act
        viewModel.AddCommand.Execute(null);

        // Assert
        Assert.True(accepted);
    }

    private AddBranchFromTicketViewModel Create() =>
        new(_settingsStore, _tokenStore, _clientFactory, NullLogger<AddBranchFromTicketViewModel>.Instance);
}
