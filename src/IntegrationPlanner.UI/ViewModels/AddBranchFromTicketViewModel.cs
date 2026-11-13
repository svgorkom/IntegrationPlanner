using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntegrationPlanner.Jira;
using IntegrationPlanner.UI.Localization;
using Microsoft.Extensions.Logging;

namespace IntegrationPlanner.UI.ViewModels;

public sealed partial class AddBranchFromTicketViewModel : ObservableObject
{
    private readonly IJiraSettingsStore _settingsStore;
    private readonly IJiraTokenStore _tokenStore;
    private readonly IJiraClientFactory _clientFactory;
    private readonly ILogger<AddBranchFromTicketViewModel> _logger;
    private bool _isNameOverridden;
    private bool _isSettingName;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private JiraIssue? _selectedIssue;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    private string _branchName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand), nameof(AddCommand))]
    private bool _isBusy;

    public AddBranchFromTicketViewModel(
        IJiraSettingsStore settingsStore,
        IJiraTokenStore tokenStore,
        IJiraClientFactory clientFactory,
        ILogger<AddBranchFromTicketViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(tokenStore);
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _settingsStore = settingsStore;
        _tokenStore = tokenStore;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public event EventHandler<bool>? CloseRequested;

    public ObservableCollection<JiraIssue> Results { get; } = [];

    public string MainlineBranchName { get; private set; } = JiraSettings.DefaultMainlineBranchName;

    partial void OnSelectedIssueChanged(JiraIssue? value)
    {
        if (value is not null && !_isNameOverridden)
        {
            SetGeneratedName(value);
        }
    }

    partial void OnBranchNameChanged(string value)
    {
        if (!_isSettingName)
        {
            _isNameOverridden = true;
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return;
        }

        IsBusy = true;
        try
        {
            StatusMessage = await SearchCoreAsync(SearchText.Trim(), cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private void Add() => CloseRequested?.Invoke(this, true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    [RelayCommand]
    private void ResetName()
    {
        _isNameOverridden = false;
        if (SelectedIssue is not null)
        {
            SetGeneratedName(SelectedIssue);
        }
    }

    private bool IsNotBusy() => !IsBusy;

    private bool CanAdd() => !IsBusy && SelectedIssue is not null && !string.IsNullOrWhiteSpace(BranchName);

    private void SetGeneratedName(JiraIssue issue)
    {
        _isSettingName = true;
        try
        {
            BranchName = JiraBranchNamer.Suggest(issue);
        }
        finally
        {
            _isSettingName = false;
        }
    }

    private async Task<string> SearchCoreAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            JiraSettings settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(true);
            MainlineBranchName = settings.MainlineBranchName;
            string? token = _tokenStore.Load();
            if (token is null || !settings.TryCreateConnection(token, out JiraConnectionSettings? connection))
            {
                return UiText.JiraSettingsIncomplete;
            }

            IReadOnlyList<JiraIssue> issues = await _clientFactory.Create(connection!).SearchIssuesAsync(query, cancellationToken).ConfigureAwait(true);
            Results.Clear();
            foreach (JiraIssue issue in issues)
            {
                Results.Add(issue);
            }

            return issues.Count == 0 ? UiText.JiraNoResults : string.Empty;
        }
        catch (JiraException exception)
        {
            _logger.LogWarning(exception, "Searching Jira failed ({Failure}).", exception.Failure);
            return JiraFailureMessages.For(exception.Failure);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or Win32Exception)
        {
            _logger.LogWarning(exception, "Reading the Jira settings or token failed.");
            return UiText.ErrorJiraSettingsLoad;
        }
    }
}
