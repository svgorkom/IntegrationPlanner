using System.Globalization;
using System.Text;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout.Tests;

internal static class SceneSnapshot
{
    public static string Write(DiagramScene scene)
    {
        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"bounds {Rect(scene.Bounds)}");
        foreach (SceneLine line in scene.Lines)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"line {line.Key} {line.Style} {Point(line.From)} {Point(line.To)}");
        }

        foreach (SceneText text in scene.Texts)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"text {text.Key} {text.Style} \"{text.Text}\" center={Point(text.Center)} size={Number(text.Size.Width)}x{Number(text.Size.Height)} angle={Number(text.Angle)} font={Number(text.FontSize)} background={text.HasBackground}");
        }

        foreach (SceneArrow arrow in scene.Arrows)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"arrow {arrow.Key} {arrow.Style} {string.Join(' ', arrow.Outline.Select(Point))}");
        }

        return builder.ToString();
    }

    private static string Rect(SceneRect rect) => $"{Number(rect.Left)},{Number(rect.Top)} {Number(rect.Width)}x{Number(rect.Height)}";

    private static string Point(ScenePoint point) => $"({Number(point.X)},{Number(point.Y)})";

    // Rounding absorbs floating-point noise from trigonometry; -0 is normalised so it cannot flip the snapshot.
    private static string Number(double value) => (Math.Round(value, 2) + 0.0).ToString("0.00", CultureInfo.InvariantCulture);
}
