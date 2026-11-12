using System.Collections.Immutable;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout.Tests;

internal static class SceneQueries
{
    public static SceneLine Line(this DiagramScene scene, string key) => Assert.Single(scene.Lines, line => line.Key == key);

    public static SceneArrow Arrow(this DiagramScene scene, string key) => Assert.Single(scene.Arrows, arrow => arrow.Key == key);

    public static SceneText Text(this DiagramScene scene, string key) => Assert.Single(scene.Texts, text => text.Key == key);

    public static ImmutableArray<SceneText> Texts(this DiagramScene scene, string key) => [.. scene.Texts.Where(text => text.Key == key)];

    public static SceneRect BoundsOf(this IEnumerable<SceneText> texts) =>
        texts.Select(text => text.Bounds).Aggregate((total, bounds) => total.Union(bounds));

    // Lines of one label block may share edges but never overlap; different labels must not overlap at all.
    public static ImmutableArray<string> OverlappingLabels(this DiagramScene scene) =>
        [.. scene.Texts
            .SelectMany((first, index) => scene.Texts.Skip(index + 1).Select(second => (first, second)))
            .Where(pair => pair.first.Bounds.Overlaps(pair.second.Bounds))
            .Select(pair => $"'{pair.first.Text}' ({pair.first.Key}) overlaps '{pair.second.Text}' ({pair.second.Key})")];

    // Rightmost x the line reaches between two heights.
    public static double RightmostXBetween(this SceneLine line, double top, double bottom)
    {
        double dy = line.To.Y - line.From.Y;
        double fromT = Math.Clamp((top - line.From.Y) / dy, 0, 1);
        double toT = Math.Clamp((bottom - line.From.Y) / dy, 0, 1);
        return line.From.X + ((line.To.X - line.From.X) * Math.Max(fromT, toT));
    }

    public static ScenePoint Midpoint(this SceneLine line) => new((line.From.X + line.To.X) / 2, (line.From.Y + line.To.Y) / 2);
}
