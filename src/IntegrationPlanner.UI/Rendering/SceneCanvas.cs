using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Editing;

namespace IntegrationPlanner.UI.Rendering;

// Hosts the rendered scene as a DrawingVisual and sizes itself to the scene bounds. A second visual shows the
// selection and the snapped preview while an item is dragged. Pressing on empty space is left to the
// surrounding scroll viewer, which pans (U1).
public sealed class SceneCanvas : FrameworkElement
{
    public static readonly DependencyProperty SceneProperty = DependencyProperty.Register(
        nameof(Scene),
        typeof(DiagramScene),
        typeof(SceneCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnVisualInputChanged));

    public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
        nameof(Palette),
        typeof(ScenePalette),
        typeof(SceneCanvas),
        new FrameworkPropertyMetadata(null, OnVisualInputChanged));

    public static readonly DependencyProperty SelectCommandProperty = DependencyProperty.Register(
        nameof(SelectCommand), typeof(ICommand), typeof(SceneCanvas), new PropertyMetadata(null));

    public static readonly DependencyProperty MoveCommandProperty = DependencyProperty.Register(
        nameof(MoveCommand), typeof(ICommand), typeof(SceneCanvas), new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedKeysProperty = DependencyProperty.Register(
        nameof(SelectedKeys), typeof(IReadOnlyCollection<string>), typeof(SceneCanvas), new PropertyMetadata(null, OnOverlayInputChanged));

    public static readonly DependencyProperty DragTargetsProperty = DependencyProperty.Register(
        nameof(DragTargets), typeof(IReadOnlyDictionary<string, SceneDragTarget>), typeof(SceneCanvas), new PropertyMetadata(null));

    public static readonly DependencyProperty HorizontalSnapProperty = DependencyProperty.Register(
        nameof(HorizontalSnap), typeof(double), typeof(SceneCanvas), new PropertyMetadata(1.0));

    public static readonly DependencyProperty VerticalSnapProperty = DependencyProperty.Register(
        nameof(VerticalSnap), typeof(double), typeof(SceneCanvas), new PropertyMetadata(1.0));

    public static readonly DependencyProperty HighlightBrushProperty = DependencyProperty.Register(
        nameof(HighlightBrush), typeof(Brush), typeof(SceneCanvas), new PropertyMetadata(null, OnOverlayInputChanged));

    public static readonly DependencyProperty ConnectCommandProperty = DependencyProperty.Register(
        nameof(ConnectCommand), typeof(ICommand), typeof(SceneCanvas), new PropertyMetadata(null));

    public static readonly DependencyProperty ConnectPreviewBrushProperty = DependencyProperty.Register(
        nameof(ConnectPreviewBrush), typeof(Brush), typeof(SceneCanvas), new PropertyMetadata(null, OnOverlayInputChanged));

    private const double HitTolerance = 5;
    private const double HighlightThickness = 6;
    private const double DragThreshold = 3;
    private const double ConnectPreviewThickness = 2;
    private const double RenameFallbackWidth = 120;
    private const double RenameFallbackHeight = 22;

    private readonly DrawingVisual _visual = new();
    private readonly DrawingVisual _overlay = new();
    private string? _dragKey;
    private string? _connectKey;
    private Point _dragStart;
    private Point _connectCurrent;
    private Vector _dragOffset;

    public SceneCanvas()
    {
        AddVisualChild(_visual);
        AddVisualChild(_overlay);
    }

    public DiagramScene? Scene
    {
        get => (DiagramScene?)GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    public ScenePalette? Palette
    {
        get => (ScenePalette?)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public ICommand? MoveCommand
    {
        get => (ICommand?)GetValue(MoveCommandProperty);
        set => SetValue(MoveCommandProperty, value);
    }

    public IReadOnlyCollection<string>? SelectedKeys
    {
        get => (IReadOnlyCollection<string>?)GetValue(SelectedKeysProperty);
        set => SetValue(SelectedKeysProperty, value);
    }

    public IReadOnlyDictionary<string, SceneDragTarget>? DragTargets
    {
        get => (IReadOnlyDictionary<string, SceneDragTarget>?)GetValue(DragTargetsProperty);
        set => SetValue(DragTargetsProperty, value);
    }

    public double HorizontalSnap
    {
        get => (double)GetValue(HorizontalSnapProperty);
        set => SetValue(HorizontalSnapProperty, value);
    }

    public double VerticalSnap
    {
        get => (double)GetValue(VerticalSnapProperty);
        set => SetValue(VerticalSnapProperty, value);
    }

    public Brush? HighlightBrush
    {
        get => (Brush?)GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    public ICommand? ConnectCommand
    {
        get => (ICommand?)GetValue(ConnectCommandProperty);
        set => SetValue(ConnectCommandProperty, value);
    }

    public Brush? ConnectPreviewBrush
    {
        get => (Brush?)GetValue(ConnectPreviewBrushProperty);
        set => SetValue(ConnectPreviewBrushProperty, value);
    }

    public event EventHandler<SceneRenameRequestedEventArgs>? RenameRequested;

    protected override int VisualChildrenCount => 2;

    protected override Visual GetVisualChild(int index) => index switch
    {
        0 => _visual,
        1 => _overlay,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    protected override Size MeasureOverride(Size availableSize) =>
        Scene is null ? default : new Size(Scene.Bounds.Width, Scene.Bounds.Height);

    // The whole canvas takes part in hit testing so that presses between drawn lines still reach it.
    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        ArgumentNullException.ThrowIfNull(hitTestParameters);
        return new Rect(RenderSize).Contains(hitTestParameters.HitPoint) ? new PointHitTestResult(this, hitTestParameters.HitPoint) : null;
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        Redraw();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnMouseLeftButtonDown(e);
        if (Scene is null)
        {
            return;
        }

        Point position = e.GetPosition(this);
        string? key = SceneHitTester.HitTest(Scene, ToScene(position), HitTolerance);
        Execute(SelectCommand, key);
        if (key is null || key == SceneKeys.Title)
        {
            return;
        }

        e.Handled = true;
        if (e.ClickCount == 2)
        {
            RequestRename(SceneSelection.RenameKeyOf(SceneSelection.FromKey(key)));
            return;
        }

        // Pressing on a lane line itself starts a connection; the branch label and the fork keep reordering lanes.
        if (IsLane(key) && ConnectCommand is not null)
        {
            if (CaptureMouse())
            {
                _connectKey = key;
                _dragStart = position;
                _connectCurrent = position;
            }

            return;
        }

        if (DragTargets?.ContainsKey(key) == true && CaptureMouse())
        {
            _dragKey = key;
            _dragStart = position;
            _dragOffset = default;
        }
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnMouseRightButtonDown(e);
        if (Scene is not null)
        {
            Execute(SelectCommand, SceneHitTester.HitTest(Scene, ToScene(e.GetPosition(this)), HitTolerance));
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnMouseMove(e);
        if (_connectKey is not null)
        {
            _connectCurrent = e.GetPosition(this);
            RedrawOverlay();
            return;
        }

        if (_dragKey is null || DragTargets is not { } targets || !targets.TryGetValue(_dragKey, out SceneDragTarget? target))
        {
            return;
        }

        Vector delta = e.GetPosition(this) - _dragStart;
        Vector offset = target.Axis == DragAxis.Horizontal
            ? new Vector(DragPlanner.SnapDistance(delta.X, HorizontalSnap), 0)
            : new Vector(0, DragPlanner.SnapDistance(delta.Y, VerticalSnap));
        if (offset != _dragOffset)
        {
            _dragOffset = offset;
            RedrawOverlay();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnMouseLeftButtonUp(e);
        if (_connectKey is not null)
        {
            CompleteConnect(e);
            return;
        }

        if (_dragKey is null)
        {
            return;
        }

        string key = _dragKey;
        Vector delta = e.GetPosition(this) - _dragStart;
        DragAxis? axis = DragTargets?.TryGetValue(key, out SceneDragTarget? target) == true ? target.Axis : null;
        ReleaseMouseCapture();
        e.Handled = true;
        if (axis is null || delta.Length < DragThreshold)
        {
            return;
        }

        Execute(MoveCommand, axis == DragAxis.Horizontal ? new SceneMove(key, delta.X, 0) : new SceneMove(key, 0, delta.Y));
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _dragKey = null;
        _connectKey = null;
        _dragOffset = default;
        RedrawOverlay();
    }

    public void RequestRename(string? key)
    {
        if (key is null || Scene is null || RenameBounds(Scene, key) is not SceneRect bounds)
        {
            return;
        }

        RenameRequested?.Invoke(this, new SceneRenameRequestedEventArgs(key, new Rect(bounds.Left - Scene.Bounds.Left, bounds.Top - Scene.Bounds.Top, bounds.Width, bounds.Height)));
    }

    private static SceneRect? RenameBounds(DiagramScene scene, string key)
    {
        SceneText? text = scene.Texts.FirstOrDefault(candidate => candidate.Key == key);
        if (text is not null)
        {
            return text.Bounds;
        }

        SceneArrow? arrow = scene.Arrows.FirstOrDefault(candidate => candidate.Key == key);
        if (arrow is not null)
        {
            return arrow.Bounds;
        }

        SceneLine? line = scene.Lines.FirstOrDefault(candidate => candidate.Key == key.Replace("-label", string.Empty, StringComparison.Ordinal));
        return line is null
            ? null
            : new SceneRect(((line.From.X + line.To.X) / 2) - (RenameFallbackWidth / 2), ((line.From.Y + line.To.Y) / 2) - (RenameFallbackHeight / 2), RenameFallbackWidth, RenameFallbackHeight);
    }

    private static bool IsLane(string key) => SceneKeys.TryParse(key, out SceneElementRef? element) && element.Kind == SceneElementKind.Lane;

    private static void OnVisualInputChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e) =>
        ((SceneCanvas)owner).Redraw();

    private static void OnOverlayInputChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e) =>
        ((SceneCanvas)owner).RedrawOverlay();

    private static void Execute(ICommand? command, object? parameter)
    {
        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
        }
    }

    private ScenePoint ToScene(Point position) => new(position.X + Scene!.Bounds.Left, position.Y + Scene.Bounds.Top);

    private string? TargetLaneAt(Point position)
    {
        string? key = SceneHitTester.HitLane(Scene!, ToScene(position), VerticalSnap / 2);
        return key is null || key == _connectKey ? null : key;
    }

    private void CompleteConnect(MouseButtonEventArgs e)
    {
        string fromKey = _connectKey!;
        Point position = e.GetPosition(this);
        double distance = (position - _dragStart).Length;
        string? toKey = Scene is null ? null : TargetLaneAt(position);
        ReleaseMouseCapture();
        e.Handled = true;
        if (toKey is not null && distance >= DragThreshold)
        {
            Execute(ConnectCommand, new SceneConnect(fromKey, toKey, ToScene(position).X, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)));
        }
    }

    private void Redraw()
    {
        using (DrawingContext context = _visual.RenderOpen())
        {
            if (Scene is not null && Palette is not null)
            {
                context.PushTransform(new TranslateTransform(-Scene.Bounds.Left, -Scene.Bounds.Top));
                context.DrawDrawing(SceneDrawingBuilder.Build(Scene, Palette, VisualTreeHelper.GetDpi(this).PixelsPerDip));
            }
        }

        RedrawOverlay();
    }

    private void RedrawOverlay()
    {
        using DrawingContext context = _overlay.RenderOpen();
        if (Scene is null || HighlightBrush is null)
        {
            return;
        }

        context.PushTransform(new TranslateTransform(-Scene.Bounds.Left, -Scene.Bounds.Top));
        Pen pen = new(HighlightBrush, HighlightThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        DrawHighlight(context, pen, SelectedKeys ?? [], default);
        if (_dragKey is not null && _dragOffset != default && DragTargets?.TryGetValue(_dragKey, out SceneDragTarget? target) == true)
        {
            DrawHighlight(context, pen, target.Keys, _dragOffset);
        }

        DrawConnectPreview(context);
    }

    private void DrawConnectPreview(DrawingContext context)
    {
        if (_connectKey is null || ConnectPreviewBrush is null || (_connectCurrent - _dragStart).Length < DragThreshold)
        {
            return;
        }

        string? targetKey = TargetLaneAt(_connectCurrent);
        if (targetKey is not null)
        {
            DrawHighlight(context, new Pen(ConnectPreviewBrush, HighlightThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, [targetKey], default);
        }

        Pen preview = new(ConnectPreviewBrush, ConnectPreviewThickness) { DashStyle = DashStyles.Dash, EndLineCap = PenLineCap.Round };
        Point origin = new(Scene!.Bounds.Left, Scene.Bounds.Top);
        context.DrawLine(preview, _dragStart + (Vector)origin, _connectCurrent + (Vector)origin);
    }

    private void DrawHighlight(DrawingContext context, Pen pen, IReadOnlyCollection<string> keys, Vector offset)
    {
        if (keys.Count == 0)
        {
            return;
        }

        HashSet<string> wanted = [.. keys];
        context.PushTransform(new TranslateTransform(offset.X, offset.Y));
        foreach (SceneLine line in Scene!.Lines.Where(line => wanted.Contains(line.Key)))
        {
            context.DrawLine(pen, new Point(line.From.X, line.From.Y), new Point(line.To.X, line.To.Y));
        }

        foreach (SceneText text in Scene.Texts.Where(text => wanted.Contains(text.Key)))
        {
            RectangleGeometry box = new(
                new Rect(text.Center.X - (text.Size.Width / 2), text.Center.Y - (text.Size.Height / 2), text.Size.Width, text.Size.Height),
                2,
                2,
                new RotateTransform(text.Angle, text.Center.X, text.Center.Y));
            context.DrawGeometry(null, pen, box);
        }

        foreach (SceneArrow arrow in Scene.Arrows.Where(arrow => wanted.Contains(arrow.Key) && arrow.Outline.Length > 0))
        {
            StreamGeometry outline = new();
            using (StreamGeometryContext geometry = outline.Open())
            {
                geometry.BeginFigure(new Point(arrow.Outline[0].X, arrow.Outline[0].Y), isFilled: false, isClosed: true);
                geometry.PolyLineTo([.. arrow.Outline.Skip(1).Select(point => new Point(point.X, point.Y))], isStroked: true, isSmoothJoin: true);
            }

            outline.Freeze();
            context.DrawGeometry(null, pen, outline);
        }

        context.Pop();
    }
}
