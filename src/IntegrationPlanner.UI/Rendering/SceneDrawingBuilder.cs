using System.Windows;
using System.Windows.Media;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.UI.Rendering;

public static class SceneDrawingBuilder
{
    public static DrawingGroup Build(DiagramScene scene, ScenePalette palette, double pixelsPerDip)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(palette);

        Brush background = Frozen(new SolidColorBrush(palette.BackgroundColor));
        Brush markerFill = Frozen(new SolidColorBrush(palette.MarkerFillColor));
        Pen solid = Frozen(new Pen(Frozen(new SolidColorBrush(palette.LineColor)), palette.LineThickness));
        Pen dashed = Frozen(new Pen(solid.Brush, palette.LineThickness) { DashStyle = Dashes(palette, palette.LineThickness) });
        Pen markerOutline = Frozen(new Pen(markerFill, palette.MarkerOutlineThickness) { DashStyle = Dashes(palette, palette.MarkerOutlineThickness) });

        DrawingGroup group = new();
        using (DrawingContext context = group.Open())
        {
            context.DrawRectangle(background, null, ToRect(scene.Bounds));
            foreach (SceneLine line in scene.Lines)
            {
                context.DrawLine(SceneStyles.IsDashed(line.Style) ? dashed : solid, ToPoint(line.From), ToPoint(line.To));
            }

            foreach (SceneArrow arrow in scene.Arrows)
            {
                bool isDone = arrow.Style == ArrowStyle.Done;
                context.DrawGeometry(isDone ? markerFill : background, isDone ? null : markerOutline, Polygon(arrow));
            }

            foreach (SceneText text in scene.Texts)
            {
                DrawText(context, text, palette, background, pixelsPerDip);
            }
        }

        group.Freeze();
        return group;
    }

    private static void DrawText(DrawingContext context, SceneText text, ScenePalette palette, Brush background, double pixelsPerDip)
    {
        Brush brush = Frozen(new SolidColorBrush(SceneStyles.TextColor(palette, text.Style)));
        FormattedText formatted = SceneStyles.Format(text.Text, text.Style, text.FontSize, palette, brush, pixelsPerDip);
        Point center = ToPoint(text.Center);
        context.PushTransform(new RotateTransform(text.Angle, center.X, center.Y));
        if (text.HasBackground)
        {
            context.DrawRectangle(
                background,
                null,
                new Rect(center.X - (text.Size.Width / 2), center.Y - (text.Size.Height / 2), text.Size.Width, text.Size.Height));
        }

        context.DrawText(formatted, new Point(center.X - (formatted.WidthIncludingTrailingWhitespace / 2), center.Y - (formatted.Height / 2)));
        context.Pop();
    }

    private static StreamGeometry Polygon(SceneArrow arrow)
    {
        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(ToPoint(arrow.Outline[0]), isFilled: true, isClosed: true);
            context.PolyLineTo([.. arrow.Outline.Skip(1).Select(ToPoint)], isStroked: true, isSmoothJoin: false);
        }

        geometry.Freeze();
        return geometry;
    }

    // WPF dash lengths are multiples of the pen thickness, whereas the palette uses DIPs like SVG does.
    private static DashStyle Dashes(ScenePalette palette, double thickness) =>
        Frozen(new DashStyle([palette.DashLength / thickness, palette.DashGap / thickness], 0));

    private static Point ToPoint(ScenePoint point) => new(point.X, point.Y);

    private static Rect ToRect(SceneRect rect) => new(rect.Left, rect.Top, rect.Width, rect.Height);

    private static T Frozen<T>(T freezable)
        where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
