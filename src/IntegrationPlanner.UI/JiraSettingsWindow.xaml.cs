using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using IntegrationPlanner.UI.Localization;
using IntegrationPlanner.UI.ViewModels;

namespace IntegrationPlanner.UI;

public partial class JiraSettingsWindow : Window
{
    private readonly JiraSettingsViewModel _viewModel;

    public JiraSettingsWindow(JiraSettingsViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.CloseRequested += (_, saved) => DialogResult = saved;
        Loaded += async (_, _) => await _viewModel.LoadAsync(CancellationToken.None);
    }

    private void OnTokenChanged(object sender, RoutedEventArgs e) => _viewModel.ApiToken = TokenBox.Password;

    private void OnOpenProfile(object sender, RoutedEventArgs e)
    {
        Uri? uri = _viewModel.GetProfileUri();
        if (uri is null)
        {
            _viewModel.StatusMessage = UiText.JiraSettingsIncomplete;
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
            _viewModel.StatusMessage = UiText.JiraOpenPageFailed;
        }
    }
}
