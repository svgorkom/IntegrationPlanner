using System.Collections.Immutable;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout;

internal static class MarkerArrowFactory
{
    public static (SceneArrow Arrow, SceneText Label) Create(Marker marker, ScenePoint anchor, TextBlockFactory text)
    {
        LayoutOptions options = text.Options;
        bool isDone = marker.State == ProgressState.Done;
        TextLine label = text.Line(marker.Label, isDone ? TextStyle.MarkerLabel : TextStyle.PlannedMarkerLabel, options.MarkerLabelFontSize);

        double headLength = options.MarkerHeadLength;
        double shaftLength = Math.Max(marker.Length - headLength, label.Size.Width + (4 * options.LabelPadding));
        double shaftHalf = options.MarkerShaftWidth / 2;
        double headHalf = options.MarkerHeadWidth / 2;
        double tail = -(headLength + shaftLength);

        // Built pointing along +x with the tip at the origin, then rotated towards the anchor.
        ScenePoint[] local =
        [
            new(0, 0),
            new(-headLength, -headHalf),
            new(-headLength, -shaftHalf),
            new(tail, -shaftHalf),
            new(tail, shaftHalf),
            new(-headLength, shaftHalf),
            new(-headLength, headHalf),
        ];

        double angle = marker.Angle;
        ScenePoint tip = SceneGeometry.Rotate(new ScenePoint(anchor.X - options.MarkerGap, anchor.Y), anchor, angle);
        ImmutableArray<ScenePoint> outline = [.. local.Select(point => Place(point, tip, angle))];
        ScenePoint labelCenter = Place(new ScenePoint(-headLength - (shaftLength / 2), 0), tip, angle);

        string key = SceneKeys.Marker(marker.Id);
        SceneArrow arrow = new(key, outline, isDone ? ArrowStyle.Done : ArrowStyle.Planned);
        SceneText sceneLabel = new(key, label.Text, labelCenter, label.Size, ReadableAngle(angle), label.FontSize, label.Style, HasBackground: false);
        return (arrow, sceneLabel);
    }

    private static ScenePoint Place(ScenePoint local, ScenePoint tip, double angle) =>
        SceneGeometry.Rotate(new ScenePoint(tip.X + local.X, tip.Y + local.Y), tip, angle);

    // The label runs along the shaft but is never upside down; vertical text reads from bottom to top.
    private static double ReadableAngle(double angle)
    {
        double normalized = MarkerDirection.Normalize(angle);
        if (normalized < 90)
        {
            return normalized;
        }

        return normalized < 270 ? normalized - 180 : normalized - 360;
    }
}
