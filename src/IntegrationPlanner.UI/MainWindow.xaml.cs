using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IntegrationPlanner.UI.ViewModels;

namespace IntegrationPlanner.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _isCloseConfirmed;

    public MainWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        PreviewMouseDown += OnPreviewMouseDown;
        PreviewKeyDown += OnPreviewKeyDown;
        Closing += OnClosing;
    }

    // Property fields commit when they lose focus. The diagram canvas does not take focus, so a click
    // elsewhere, Enter and keyboard shortcuts commit the field being edited first.
    private static void CommitFocusedTextBox()
    {
        if (Keyboard.FocusedElement is TextBox textBox)
        {
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }
    }

    private static bool IsWithin(DependencyObject? element, DependencyObject ancestor)
    {
        for (DependencyObject? current = element; current is not null; current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox textBox && !IsWithin(e.OriginalSource as DependencyObject, textBox))
        {
            CommitFocusedTextBox();
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        bool isSingleLineEnter = e.Key == Key.Enter && Keyboard.FocusedElement is TextBox { AcceptsReturn: false };
        if (isSingleLineEnter || Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            CommitFocusedTextBox();
        }
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        CommitFocusedTextBox();
        if (_isCloseConfirmed || !_viewModel.HasUnsavedChanges)
        {
            return;
        }

        // The question and a possible save run after the close request, so close again once they allow it.
        e.Cancel = true;
        if (await _viewModel.ConfirmDiscardChangesAsync(CancellationToken.None))
        {
            _isCloseConfirmed = true;
            Close();
        }
    }
}
