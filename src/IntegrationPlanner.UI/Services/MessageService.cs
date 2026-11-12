using System.Windows;
using IntegrationPlanner.UI.Localization;

namespace IntegrationPlanner.UI.Services;

public sealed class MessageService : IMessageService
{
    public void ShowError(string message) => Show(message, MessageBoxImage.Error);

    public void ShowInformation(string message) => Show(message, MessageBoxImage.Information);

    private static void Show(string message, MessageBoxImage image)
    {
        ArgumentNullException.ThrowIfNull(message);
        Window? owner = Application.Current?.MainWindow;
        if (owner is { IsVisible: true })
        {
            MessageBox.Show(owner, message, UiText.MessageCaption, MessageBoxButton.OK, image);
            return;
        }

        MessageBox.Show(message, UiText.MessageCaption, MessageBoxButton.OK, image);
    }
}
