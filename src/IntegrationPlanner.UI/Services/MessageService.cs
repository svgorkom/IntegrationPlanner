using System.Windows;
using IntegrationPlanner.UI.Localization;

namespace IntegrationPlanner.UI.Services;

public sealed class MessageService : IMessageService
{
    public void ShowError(string message) => Show(message, MessageBoxImage.Error);

    public void ShowInformation(string message) => Show(message, MessageBoxImage.Information);

    public SaveChangesAnswer AskSaveChanges() =>
        Show(UiText.QuestionSaveChanges, MessageBoxImage.Warning, MessageBoxButton.YesNoCancel) switch
        {
            MessageBoxResult.Yes => SaveChangesAnswer.Save,
            MessageBoxResult.No => SaveChangesAnswer.Discard,
            _ => SaveChangesAnswer.Cancel,
        };

    private static MessageBoxResult Show(string message, MessageBoxImage image, MessageBoxButton buttons = MessageBoxButton.OK)
    {
        ArgumentNullException.ThrowIfNull(message);
        Window? owner = Application.Current?.MainWindow;
        return owner is { IsVisible: true }
            ? MessageBox.Show(owner, message, UiText.MessageCaption, buttons, image)
            : MessageBox.Show(message, UiText.MessageCaption, buttons, image);
    }
}
