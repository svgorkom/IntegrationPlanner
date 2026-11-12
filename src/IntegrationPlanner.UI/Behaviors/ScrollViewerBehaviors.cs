using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace IntegrationPlanner.UI.Behaviors;

// View-only input handling for the diagram: Ctrl+wheel zooms through view-model commands, left-drag pans.
public static class ScrollViewerBehaviors
{
    public static readonly DependencyProperty ZoomInCommandProperty = DependencyProperty.RegisterAttached(
        "ZoomInCommand", typeof(ICommand), typeof(ScrollViewerBehaviors), new PropertyMetadata(null, OnZoomCommandChanged));

    public static readonly DependencyProperty ZoomOutCommandProperty = DependencyProperty.RegisterAttached(
        "ZoomOutCommand", typeof(ICommand), typeof(ScrollViewerBehaviors), new PropertyMetadata(null, OnZoomCommandChanged));

    public static readonly DependencyProperty IsDragPanEnabledProperty = DependencyProperty.RegisterAttached(
        "IsDragPanEnabled", typeof(bool), typeof(ScrollViewerBehaviors), new PropertyMetadata(false, OnIsDragPanEnabledChanged));

    private static readonly DependencyProperty DragOriginProperty = DependencyProperty.RegisterAttached(
        "DragOrigin", typeof(Point?), typeof(ScrollViewerBehaviors), new PropertyMetadata(null));

    public static ICommand? GetZoomInCommand(DependencyObject element) => (ICommand?)element.GetValue(ZoomInCommandProperty);

    public static void SetZoomInCommand(DependencyObject element, ICommand? value) => element.SetValue(ZoomInCommandProperty, value);

    public static ICommand? GetZoomOutCommand(DependencyObject element) => (ICommand?)element.GetValue(ZoomOutCommandProperty);

    public static void SetZoomOutCommand(DependencyObject element, ICommand? value) => element.SetValue(ZoomOutCommandProperty, value);

    public static bool GetIsDragPanEnabled(DependencyObject element) => (bool)element.GetValue(IsDragPanEnabledProperty);

    public static void SetIsDragPanEnabled(DependencyObject element, bool value) => element.SetValue(IsDragPanEnabledProperty, value);

    private static void OnZoomCommandChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not ScrollViewer viewer)
        {
            return;
        }

        viewer.PreviewMouseWheel -= OnPreviewMouseWheel;
        viewer.PreviewMouseWheel += OnPreviewMouseWheel;
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control || sender is not ScrollViewer viewer)
        {
            return;
        }

        ICommand? command = e.Delta > 0 ? GetZoomInCommand(viewer) : GetZoomOutCommand(viewer);
        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
        }

        e.Handled = true;
    }

    private static void OnIsDragPanEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not ScrollViewer viewer)
        {
            return;
        }

        viewer.PreviewMouseLeftButtonDown -= OnDragStart;
        viewer.PreviewMouseMove -= OnDragMove;
        viewer.PreviewMouseLeftButtonUp -= OnDragEnd;
        viewer.LostMouseCapture -= OnLostCapture;
        if (!(bool)e.NewValue)
        {
            return;
        }

        viewer.PreviewMouseLeftButtonDown += OnDragStart;
        viewer.PreviewMouseMove += OnDragMove;
        viewer.PreviewMouseLeftButtonUp += OnDragEnd;
        viewer.LostMouseCapture += OnLostCapture;
    }

    private static void OnDragStart(object sender, MouseButtonEventArgs e)
    {
        ScrollViewer viewer = (ScrollViewer)sender;

        // Clicks on the scroll bars must keep working as scroll bar clicks.
        if (e.OriginalSource is DependencyObject source && FindAncestor<ScrollBar>(source) is not null)
        {
            return;
        }

        Point position = e.GetPosition(viewer);
        viewer.SetValue(DragOriginProperty, new Point(position.X + viewer.HorizontalOffset, position.Y + viewer.VerticalOffset));
        viewer.CaptureMouse();
        e.Handled = true;
    }

    private static void OnDragMove(object sender, MouseEventArgs e)
    {
        ScrollViewer viewer = (ScrollViewer)sender;
        if (viewer.GetValue(DragOriginProperty) is not Point origin)
        {
            return;
        }

        Point position = e.GetPosition(viewer);
        viewer.ScrollToHorizontalOffset(origin.X - position.X);
        viewer.ScrollToVerticalOffset(origin.Y - position.Y);
    }

    private static void OnDragEnd(object sender, MouseButtonEventArgs e)
    {
        ScrollViewer viewer = (ScrollViewer)sender;
        if (viewer.GetValue(DragOriginProperty) is null)
        {
            return;
        }

        viewer.ReleaseMouseCapture();
        e.Handled = true;
    }

    private static void OnLostCapture(object sender, MouseEventArgs e) => ((ScrollViewer)sender).ClearValue(DragOriginProperty);

    private static T? FindAncestor<T>(DependencyObject start)
        where T : DependencyObject
    {
        for (DependencyObject? current = start; current is not null; current = current is Visual ? VisualTreeHelper.GetParent(current) : null)
        {
            if (current is T match)
            {
                return match;
            }
        }

        return null;
    }
}
