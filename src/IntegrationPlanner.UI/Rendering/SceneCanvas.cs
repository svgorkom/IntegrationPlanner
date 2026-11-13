using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Editing;

namespace IntegrationPlanner.UI.Rendering;

// Hosts the rendered scene as a DrawingVisual and sizes itself to the scene bounds. A second visual shows hover,
// selection, handles and the snapped preview while an item is dragged. Pressing on empty space is left to the
// surrounding scroll viewer, which pans (U1). Pointer tolerances and handle sizes are in screen pixels, so they stay
// usable at every zoom level.
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

    public static readonly DependencyProperty MarkerGapProperty = DependencyProperty.Register(
        nameof(MarkerGap), typeof(double), typeof(SceneCanvas), new PropertyMetadata(0.0));

    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
        nameof(Zoom), typeof(double), typeof(SceneCanvas), new PropertyMetadata(1.0, OnOverlayInputChanged));

    public static readonly DependencyProperty HighlightBrushProperty = DependencyProperty.Register(
        nameof(HighlightBrush), typeof(Brush), typeof(SceneCanvas), new PropertyMetadata(null, OnOverlayInputChanged));

    public static readonly DependencyProperty ConnectCommandProperty = DependencyProperty.Register(
        nameof(ConnectCommand), typeof(ICommand), typeof(SceneCanvas), new PropertyMetadata(null));

    public static readonly DependencyProperty ConnectPreviewBrushProperty = DependencyProperty.Register(
        nameof(ConnectPreviewBrush), typeof(Brush), typeof(SceneCanvas), new PropertyMetadata(null, OnOverlayInputChanged));

    public static readonly DependencyProperty ReshapeCommandProperty = DependencyProperty.Register(
        nameof(ReshapeCommand), typeof(ICommand), typeof(SceneCanvas), new PropertyMetadata(null));

    public static readonly DependencyProperty RetargetCommandProperty = DependencyProperty.Register(
        nameof(RetargetCommand), typeof(ICommand), typeof(SceneCanvas), new PropertyMetadata(null));

    public static readonly DependencyProperty ContextPointProperty = DependencyProperty.Register(
        nameof(ContextPoint),
        typeof(ScenePoint?),
        typeof(SceneCanvas),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private const double HitTolerance = 7;
    private const double HandleRadius = 7;
    private const double HandleHitRadius = 11;
    private const double HandleRingThickness = 2;
    private const double ReadoutOffset = 14;
    private const double ReadoutPadding = 3;
    private const double ReadoutFontSize = 12;
    private const double HighlightThickness = 6;
    private const double HoverOpacity = 0.35;
    private const double DragThreshold = 3;
    private const double ConnectPreviewThickness = 2;
    private const double RenameFallbackWidth = 120;
    private const double RenameFallbackHeight = 22;
    private const double MinimumZoom = 0.05;

    private readonly DrawingVisual _visual = new();
    private readonly DrawingVisual _overlay = new();
    private string? _dragKey;
    private string? _connectKey;
    private string? _reshapeKey;
    private string? _hoverKey;
    private CanvasHandle? _retarget;
    private MarkerDrop? _markerDrop;
    private ScenePoint? _dragAnchor;
    private bool _hasReshaped;
    private Point _dragStart;
    private Point _pointer;
    private Vector _dragOffset;

    public SceneCanvas()
    {
        AddVisualChild(_visual);
        AddVisualChild(_overlay);
    }

    private enum HandleKind
    {
        MarkerTail,
        ConnectorFrom,
        ConnectorTo,
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

    public double MarkerGap
    {
        get => (double)GetValue(MarkerGapProperty);
        set => SetValue(MarkerGapProperty, value);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
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

    public ICommand? ReshapeCommand
    {
        get => (ICommand?)GetValue(ReshapeCommandProperty);
        set => SetValue(ReshapeCommandProperty, value);
    }

    public ICommand? RetargetCommand
    {
        get => (ICommand?)GetValue(RetargetCommandProperty);
        set => SetValue(RetargetCommandProperty, value);
    }

    public ScenePoint? ContextPoint
    {
        get => (ScenePoint?)GetValue(ContextPointProperty);
        set => SetValue(ContextPointProperty, value);
    }

    public event EventHandler<SceneRenameRequestedEventArgs>? RenameRequested;

    protected override int VisualChildrenCount => 2;

    protected override Visual GetVisualChild(int index) => index switch
    {
        0 => _visual,
        1 => _overlay,
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    // Abandons the drag in progress without changing the plan; false when nothing was being dragged.
    public bool CancelInteraction()
    {
        if (!IsMouseCaptured)
        {
            return false;
        }

        ReleaseMouseCapture();
        return true;
    }

    public void RequestRename(string? key)
    {
        if (key is null || Scene is null || RenameBounds(Scene, key) is not SceneRect bounds)
        {
            return;
        }

        RenameRequested?.Invoke(this, new SceneRenameRequestedEventArgs(key, new Rect(bounds.Left - Scene.Bounds.Left, bounds.Top - Scene.Bounds.Top, bounds.Width, bounds.Height)));
    }

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
        if (e.ClickCount == 1 && HandleAt(position) is CanvasHandle handle && CaptureMouse())
        {
            StartHandleDrag(handle, position);
            e.Handled = true;
            return;
        }

        string? key = HitTest(position);
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
        if (IsKind(key, SceneElementKind.Lane) && ConnectCommand is not null)
        {
            if (CaptureMouse())
            {
                _connectKey = key;
                _dragStart = position;
                _pointer = position;
            }

            return;
        }

        if (DragTargets?.TryGetValue(key, out SceneDragTarget? target) == true && CaptureMouse())
        {
            _dragKey = key;
            _dragStart = position;
            _dragOffset = default;
            _dragAnchor = target.Axis == DragAxis.Both ? MarkerReshaper.AnchorOf(Scene, key, MarkerGap) : null;
            Cursor = CursorFor(target.Axis);
        }
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnMouseRightButtonDown(e);
        if (Scene is null)
        {
            return;
        }

        Point position = e.GetPosition(this);
        ContextPoint = ToScene(position);
        Execute(SelectCommand, HitTest(position));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnMouseMove(e);
        if (Scene is null)
        {
            return;
        }

        Point position = e.GetPosition(this);
        if (_reshapeKey is not null)
        {
            _hasReshaped = true;
            ScenePoint pointer = ToScene(position);
            Execute(ReshapeCommand, new SceneReshape(_reshapeKey, pointer.X, pointer.Y, ReshapePhase.Preview));
            return;
        }

        if (_connectKey is not null || _retarget is not null)
        {
            _pointer = position;
            RedrawOverlay();
            return;
        }

        if (_dragKey is not null)
        {
            UpdateDragPreview(position);
            return;
        }

        UpdateHover(position);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (IsMouseCaptured || _hoverKey is null)
        {
            return;
        }

        _hoverKey = null;
        Cursor = null;
        RedrawOverlay();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnMouseLeftButtonUp(e);
        if (_reshapeKey is not null)
        {
            CompleteReshape(e);
        }
        else if (_connectKey is not null)
        {
            CompleteConnect(e);
        }
        else if (_retarget is not null)
        {
            CompleteRetarget(e);
        }
        else if (_dragKey is not null)
        {
            CompleteMove(e);
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        string? reshapeKey = _reshapeKey;
        _reshapeKey = null;
        _dragKey = null;
        _connectKey = null;
        _retarget = null;
        _markerDrop = null;
        _dragAnchor = null;
        _dragOffset = default;
        Cursor = null;
        if (reshapeKey is not null)
        {
            Execute(ReshapeCommand, new SceneReshape(reshapeKey, 0, 0, ReshapePhase.Cancel));
        }

        RedrawOverlay();
    }

    private static Cursor CursorFor(DragAxis axis) => axis switch
    {
        DragAxis.Horizontal => Cursors.SizeWE,
        DragAxis.Vertical => Cursors.SizeNS,
        _ => Cursors.SizeAll,
    };

    private static bool IsKind(string key, SceneElementKind kind) => SceneKeys.TryParse(key, out SceneElementRef? element) && element.Kind == kind;

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

    private static double Distance(ScenePoint a, ScenePoint b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    // Converts a size in screen pixels to scene units at the current zoom.
    private double Px(double pixels) => pixels / Math.Max(Zoom, MinimumZoom);

    private string? HitTest(Point position) => SceneHitTester.HitTest(Scene!, ToScene(position), Px(HitTolerance));

    private ScenePoint ToScene(Point position) => new(position.X + Scene!.Bounds.Left, position.Y + Scene.Bounds.Top);

    private Point FromScene(ScenePoint point) => new(point.X - Scene!.Bounds.Left, point.Y - Scene.Bounds.Top);

    private void StartHandleDrag(CanvasHandle handle, Point position)
    {
        Cursor = Cursors.Hand;
        if (handle.Kind == HandleKind.MarkerTail)
        {
            _reshapeKey = handle.Key;
            _hasReshaped = false;
            return;
        }

        _retarget = handle;
        _dragStart = position;
        _pointer = position;
    }

    private void UpdateHover(Point position)
    {
        CanvasHandle? handle = HandleAt(position);
        string? key = handle is null ? HitTest(position) : null;
        Cursor = handle is not null ? Cursors.Hand
            : key is null ? null
            : IsKind(key, SceneElementKind.Lane) && ConnectCommand is not null ? Cursors.Cross
            : DragTargets?.TryGetValue(key, out SceneDragTarget? target) == true ? CursorFor(target.Axis)
            : Cursors.Hand;
        ToolTip = key is null ? null : Scene!.Texts.FirstOrDefault(text => text.Key == key && text.ToolTip is not null)?.ToolTip;
        if (key != _hoverKey)
        {
            _hoverKey = key;
            RedrawOverlay();
        }
    }

    private void UpdateDragPreview(Point position)
    {
        if (DragTargets is not { } targets || !targets.TryGetValue(_dragKey!, out SceneDragTarget? target))
        {
            return;
        }

        Vector delta = position - _dragStart;
        Vector offset;
        if (target.Axis == DragAxis.Both)
        {
            _markerDrop = _dragAnchor is ScenePoint anchor && delta.Length >= Px(DragThreshold)
                ? MarkerDropResolver.Resolve(Scene!, new ScenePoint(anchor.X + delta.X, anchor.Y + delta.Y), HorizontalSnap, VerticalSnap)
                : null;
            offset = _markerDrop is not null && _dragAnchor is ScenePoint origin
                ? new Vector(_markerDrop.Anchor.X - origin.X, _markerDrop.Anchor.Y - origin.Y)
                : default;
        }
        else
        {
            offset = target.Axis == DragAxis.Horizontal
                ? new Vector(DragPlanner.SnapDistance(delta.X, HorizontalSnap), 0)
                : new Vector(0, DragPlanner.SnapDistance(delta.Y, VerticalSnap));
        }

        if (offset != _dragOffset)
        {
            _dragOffset = offset;
            RedrawOverlay();
        }
    }

    // Grab points for the selection: a marker's tail reshapes it, either end of a pull or merge reconnects it.
    private List<CanvasHandle> Handles()
    {
        List<CanvasHandle> handles = [];
        if (Scene is null || SelectedKeys is not { Count: > 0 } selected)
        {
            return handles;
        }

        foreach (SceneArrow arrow in Scene.Arrows.Where(arrow => arrow.Outline.Length >= 5 && selected.Contains(arrow.Key)))
        {
            handles.Add(new CanvasHandle(HandleKind.MarkerTail, arrow.Key, MarkerReshaper.TailCenter(arrow), null));
        }

        if (RetargetCommand is null)
        {
            return handles;
        }

        foreach (SceneLine line in Scene.Lines.Where(line => selected.Contains(line.Key) && IsKind(line.Key, SceneElementKind.Connector)))
        {
            handles.Add(new CanvasHandle(HandleKind.ConnectorFrom, line.Key, line.From, line.To));
            handles.Add(new CanvasHandle(HandleKind.ConnectorTo, line.Key, line.To, line.From));
        }

        return handles;
    }

    private CanvasHandle? HandleAt(Point position)
    {
        ScenePoint pointer = ToScene(position);
        double radius = Px(HandleHitRadius);
        return Handles()
            .Where(handle => Distance(handle.Center, pointer) <= radius)
            .OrderBy(handle => Distance(handle.Center, pointer))
            .FirstOrDefault();
    }

    private string? TargetLaneAt(Point position, string? excludedKey)
    {
        string? key = SceneHitTester.HitLane(Scene!, ToScene(position), VerticalSnap / 2);
        return key is null || key == excludedKey ? null : key;
    }

    // The lane at the end of the connector that stays put, so the moving end cannot be dropped onto it.
    private string? FixedLaneKey(CanvasHandle handle) =>
        handle.Other is ScenePoint other ? SceneHitTester.HitLane(Scene!, other, Px(1)) : null;

    private void CompleteReshape(MouseButtonEventArgs e)
    {
        string key = _reshapeKey!;
        _reshapeKey = null;
        ScenePoint pointer = ToScene(e.GetPosition(this));
        ReleaseMouseCapture();
        e.Handled = true;
        Execute(ReshapeCommand, new SceneReshape(key, pointer.X, pointer.Y, _hasReshaped ? ReshapePhase.Commit : ReshapePhase.Cancel));
    }

    private void CompleteConnect(MouseButtonEventArgs e)
    {
        string fromKey = _connectKey!;
        Point position = e.GetPosition(this);
        double distance = (position - _dragStart).Length;
        string? toKey = TargetLaneAt(position, fromKey);
        ReleaseMouseCapture();
        e.Handled = true;
        if (toKey is not null && distance >= Px(DragThreshold))
        {
            Execute(ConnectCommand, new SceneConnect(fromKey, toKey, ToScene(position).X, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)));
        }
    }

    private void CompleteRetarget(MouseButtonEventArgs e)
    {
        CanvasHandle handle = _retarget!;
        Point position = e.GetPosition(this);
        double distance = (position - _dragStart).Length;
        string? laneKey = TargetLaneAt(position, FixedLaneKey(handle));
        ReleaseMouseCapture();
        e.Handled = true;
        if (laneKey is not null && distance >= Px(DragThreshold))
        {
            ConnectorEnd end = handle.Kind == HandleKind.ConnectorFrom ? ConnectorEnd.From : ConnectorEnd.To;
            Execute(RetargetCommand, new SceneRetarget(handle.Key, end, laneKey, ToScene(position).X));
        }
    }

    private void CompleteMove(MouseButtonEventArgs e)
    {
        string key = _dragKey!;
        Vector delta = e.GetPosition(this) - _dragStart;
        DragAxis? axis = DragTargets?.TryGetValue(key, out SceneDragTarget? target) == true ? target.Axis : null;
        ReleaseMouseCapture();
        e.Handled = true;
        if (axis is null || delta.Length < Px(DragThreshold))
        {
            return;
        }

        SceneMove move = axis switch
        {
            DragAxis.Horizontal => new SceneMove(key, delta.X, 0),
            DragAxis.Vertical => new SceneMove(key, 0, delta.Y),
            _ => new SceneMove(key, delta.X, delta.Y),
        };
        Execute(MoveCommand, move);
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

    private Pen HighlightPen(Brush brush) =>
        new(brush, Px(HighlightThickness)) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

    private void RedrawOverlay()
    {
        using DrawingContext context = _overlay.RenderOpen();
        if (Scene is null || HighlightBrush is null)
        {
            return;
        }

        context.PushTransform(new TranslateTransform(-Scene.Bounds.Left, -Scene.Bounds.Top));
        DrawHover(context);
        Pen pen = HighlightPen(HighlightBrush);
        DrawHighlight(context, pen, SelectedKeys ?? [], default);
        if (_dragKey is not null && _dragOffset != default && DragTargets?.TryGetValue(_dragKey, out SceneDragTarget? target) == true)
        {
            if (_markerDrop is not null && ConnectPreviewBrush is not null)
            {
                DrawHighlight(context, HighlightPen(ConnectPreviewBrush), [_markerDrop.TargetKey], default);
            }

            DrawHighlight(context, pen, target.Keys, _dragOffset);
        }

        DrawConnectPreview(context);
        DrawRetargetPreview(context);
        DrawHandles(context);
    }

    private void DrawHover(DrawingContext context)
    {
        if (_hoverKey is null || HighlightBrush is null || SelectedKeys?.Contains(_hoverKey) == true)
        {
            return;
        }

        Brush brush = HighlightBrush.CloneCurrentValue();
        brush.Opacity = HoverOpacity;
        brush.Freeze();
        DrawHighlight(context, HighlightPen(brush), SceneSelection.KeysOf(SceneSelection.FromKey(_hoverKey)), default);
    }

    private void DrawHandles(DrawingContext context)
    {
        if (Palette is null)
        {
            return;
        }

        Pen ring = new(new SolidColorBrush(Palette.BackgroundColor), Px(HandleRingThickness));
        double radius = Px(HandleRadius);
        foreach (CanvasHandle handle in Handles())
        {
            context.DrawEllipse(HighlightBrush, ring, new Point(handle.Center.X, handle.Center.Y), radius, radius);
        }

        if (_reshapeKey is not null && Scene!.Arrows.FirstOrDefault(arrow => arrow.Key == _reshapeKey && arrow.Outline.Length >= 5) is SceneArrow reshaped)
        {
            DrawReadout(context, reshaped);
        }
    }

    private void DrawReadout(DrawingContext context, SceneArrow arrow)
    {
        ScenePoint tailPoint = MarkerReshaper.TailCenter(arrow);
        double dx = arrow.Outline[0].X - tailPoint.X;
        double dy = arrow.Outline[0].Y - tailPoint.Y;
        double angle = MarkerDirection.Normalize(Math.Atan2(dy, dx) * 180 / Math.PI);
        string readout = string.Create(CultureInfo.CurrentCulture, $"{angle:0}°  {Math.Sqrt((dx * dx) + (dy * dy)):0}");
        FormattedText text = new(
            readout,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(Palette!.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            Px(ReadoutFontSize),
            new SolidColorBrush(Palette.TextColor),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        double padding = Px(ReadoutPadding);
        Point origin = new(tailPoint.X + Px(ReadoutOffset), tailPoint.Y - Px(ReadoutOffset) - text.Height);
        context.DrawRoundedRectangle(
            new SolidColorBrush(Palette.BackgroundColor),
            new Pen(HighlightBrush, Px(1)),
            new Rect(origin.X - padding, origin.Y - padding, text.Width + (2 * padding), text.Height + (2 * padding)),
            Px(2),
            Px(2));
        context.DrawText(text, origin);
    }

    private void DrawConnectPreview(DrawingContext context)
    {
        if (_connectKey is null)
        {
            return;
        }

        DrawLanePreview(context, _dragStart, TargetLaneAt(_pointer, _connectKey));
    }

    private void DrawRetargetPreview(DrawingContext context)
    {
        if (_retarget?.Other is not ScenePoint fixedEnd)
        {
            return;
        }

        DrawLanePreview(context, FromScene(fixedEnd), TargetLaneAt(_pointer, FixedLaneKey(_retarget)));
    }

    private void DrawLanePreview(DrawingContext context, Point origin, string? targetKey)
    {
        if (ConnectPreviewBrush is null || (_pointer - _dragStart).Length < Px(DragThreshold))
        {
            return;
        }

        if (targetKey is not null)
        {
            DrawHighlight(context, HighlightPen(ConnectPreviewBrush), [targetKey], default);
        }

        Pen preview = new(ConnectPreviewBrush, Px(ConnectPreviewThickness)) { DashStyle = DashStyles.Dash, EndLineCap = PenLineCap.Round };
        Vector shift = new(Scene!.Bounds.Left, Scene.Bounds.Top);
        context.DrawLine(preview, origin + shift, _pointer + shift);
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

    // Other is the opposite end of a pull or merge, which stays put while this end is dragged.
    private sealed record CanvasHandle(HandleKind Kind, string Key, ScenePoint Center, ScenePoint? Other);
}
