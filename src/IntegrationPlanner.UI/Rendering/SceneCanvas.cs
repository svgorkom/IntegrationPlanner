using System.Windows;
using System.Windows.Media;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.UI.Rendering;

// Hosts the rendered scene as a single DrawingVisual and sizes itself to the scene bounds.
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

    private readonly DrawingVisual _visual = new();

    public SceneCanvas()
    {
        AddVisualChild(_visual);
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

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);
        return _visual;
    }

    protected override Size MeasureOverride(Size availableSize) =>
        Scene is null ? default : new Size(Scene.Bounds.Width, Scene.Bounds.Height);

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        Redraw();
    }

    private static void OnVisualInputChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e) =>
        ((SceneCanvas)owner).Redraw();

    private void Redraw()
    {
        using DrawingContext context = _visual.RenderOpen();
        if (Scene is null || Palette is null)
        {
            return;
        }

        context.PushTransform(new TranslateTransform(-Scene.Bounds.Left, -Scene.Bounds.Top));
        context.DrawDrawing(SceneDrawingBuilder.Build(Scene, Palette, VisualTreeHelper.GetDpi(this).PixelsPerDip));
    }
}
