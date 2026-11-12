using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using IntegrationPlanner.UI.Editing;
using IntegrationPlanner.UI.Services;
using IntegrationPlanner.UI.ViewModels;

namespace IntegrationPlanner.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ThemeService _themeService;
    private bool _isCloseConfirmed;
    private string? _renameKey;

    public MainWindow(MainViewModel viewModel, ThemeService themeService)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(themeService);
        InitializeComponent();
        _viewModel = viewModel;
        _themeService = themeService;
        DataContext = viewModel;
        PreviewMouseDown += OnPreviewMouseDown;
        PreviewKeyDown += OnPreviewKeyDown;
        Closing += OnClosing;
        SourceInitialized += (_, _) => ApplyBackdrop();
        StateChanged += (_, _) => MaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        _themeService.Changed += (_, _) => ApplyBackdrop();
    }

    private void ApplyBackdrop()
    {
        if (WindowBackdrop.Apply(this, _themeService.IsDark))
        {
            Background = Brushes.Transparent;
        }
        else
        {
            SetResourceReference(BackgroundProperty, "WindowBackgroundBrush");
        }
    }

    private void OnMoreClick(object sender, RoutedEventArgs e)
    {
        MoreButton.ContextMenu!.PlacementTarget = MoreButton;
        MoreButton.ContextMenu.IsOpen = true;
    }

    private void OnToggleTheme(object sender, RoutedEventArgs e) => _themeService.Toggle();

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
        }
        else
        {
            SystemCommands.MaximizeWindow(this);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);

    private void OnRenameSelected(object sender, RoutedEventArgs e)
    {
        string? selected = _viewModel.SelectedKeys.FirstOrDefault();
        Diagram.RequestRename(SceneSelection.RenameKeyOf(SceneSelection.FromKey(selected)));
    }

    private void OnRenameRequested(object? sender, SceneRenameRequestedEventArgs e)
    {
        string? text = _viewModel.RenameText(e.Key);
        if (text is null)
        {
            return;
        }

        Rect bounds = Diagram.TransformToVisual(CanvasHost).TransformBounds(e.Bounds);
        _renameKey = e.Key;
        InlineEditor.Text = text;
        InlineEditor.Margin = new Thickness(bounds.Left, bounds.Top, 0, 0);
        InlineEditor.MinWidth = Math.Max(bounds.Width, 120);
        InlineEditor.Visibility = Visibility.Visible;
        InlineEditor.Focus();
        InlineEditor.SelectAll();
    }

    private void OnInlineEditorKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            CloseInlineEditor(commit: true);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CloseInlineEditor(commit: false);
        }
    }

    private void OnInlineEditorLostFocus(object sender, KeyboardFocusChangedEventArgs e) => CloseInlineEditor(commit: true);

    private void CloseInlineEditor(bool commit)
    {
        string? key = _renameKey;
        if (key is null)
        {
            return;
        }

        _renameKey = null;
        InlineEditor.Visibility = Visibility.Collapsed;
        if (commit)
        {
            _viewModel.RenameElementCommand.Execute(new SceneRename(key, InlineEditor.Text));
        }
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
            // Close() throws while the current close request is still being processed, so let it finish first.
            await Dispatcher.Yield(DispatcherPriority.Background);
            Close();
        }
    }
}
