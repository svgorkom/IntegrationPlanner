using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntegrationPlanner.Jira;
using IntegrationPlanner.UI.Localization;
using IntegrationPlanner.UI.Services;
using Microsoft.Extensions.Logging;

namespace IntegrationPlanner.UI.ViewModels;

public sealed partial class JiraSettingsViewModel : ObservableObject
{
    private readonly IJiraSettingsStore _settingsStore;
    private readonly IJiraTokenStore _tokenStore;
    private readonly IJiraClientFactory _clientFactory;
    private readonly IMessageService _messages;
    private readonly ILogger<JiraSettingsViewModel> _logger;

    [ObservableProperty]
    private string _baseUrl = string.Empty;

    [ObservableProperty]
    private string _apiToken = string.Empty;

    [ObservableProperty]
    private string _projectKey = string.Empty;

    [ObservableProperty]
    private string _mainlineBranchName = JiraSettings.DefaultMainlineBranchName;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand), nameof(SaveCommand))]
    private bool _isBusy;

    public JiraSettingsViewModel(
        IJiraSettingsStore settingsStore,
        IJiraTokenStore tokenStore,
        IJiraClientFactory clientFactory,
        IMessageService messages,
        ILogger<JiraSettingsViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(tokenStore);
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(logger);
        _settingsStore = settingsStore;
        _tokenStore = tokenStore;
        _clientFactory = clientFactory;
        _messages = messages;
        _logger = logger;
    }

    public event EventHandler<bool>? CloseRequested;

    public Uri? GetProfileUri() =>
        Uri.TryCreate(BaseUrl.Trim().TrimEnd('/') + "/secure/ViewProfile.jspa", UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri
            : null;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            JiraSettings settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(true);
            BaseUrl = settings.BaseUrl;
            ProjectKey = settings.ProjectKey;
            MainlineBranchName = settings.MainlineBranchName;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _logger.LogWarning(exception, "Reading the Jira settings failed.");
            _messages.ShowError(UiText.ErrorJiraSettingsLoad);
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task TestConnectionAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            StatusMessage = await TestAsync(cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsNotBusy))]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(MainlineBranchName))
        {
            StatusMessage = UiText.JiraMainlineRequired;
            return;
        }

        IsBusy = true;
        try
        {
            if (!string.IsNullOrWhiteSpace(ApiToken))
            {
                _tokenStore.Save(ApiToken);
            }

            await _settingsStore.SaveAsync(CreateSettings(), cancellationToken).ConfigureAwait(true);
            CloseRequested?.Invoke(this, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or Win32Exception)
        {
            _logger.LogError(exception, "Saving the Jira settings failed.");
            _messages.ShowError(UiText.ErrorJiraSettingsSave);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    private bool IsNotBusy() => !IsBusy;

    private JiraSettings CreateSettings() => new(BaseUrl.Trim(), ProjectKey.Trim(), MainlineBranchName.Trim());

    private async Task<string> TestAsync(CancellationToken cancellationToken)
    {
        string? token = string.IsNullOrWhiteSpace(ApiToken) ? ReadStoredToken() : ApiToken;
        if (token is null || !CreateSettings().TryCreateConnection(token, out JiraConnectionSettings? connection))
        {
            return UiText.JiraSettingsIncomplete;
        }

        try
        {
            string user = await _clientFactory.Create(connection!).GetCurrentUserAsync(cancellationToken).ConfigureAwait(true);
            return UiText.Format(UiText.JiraConnectedFormat, user);
        }
        catch (JiraException exception)
        {
            _logger.LogWarning(exception, "Testing the Jira connection failed ({Failure}).", exception.Failure);
            return JiraFailureMessages.For(exception.Failure);
        }
    }

    private string? ReadStoredToken()
    {
        try
        {
            return _tokenStore.Load();
        }
        catch (Win32Exception exception)
        {
            _logger.LogWarning(exception, "Reading the stored Jira token failed.");
            return null;
        }
    }
}
