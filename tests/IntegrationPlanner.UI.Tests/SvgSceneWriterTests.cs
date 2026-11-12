using System.Collections.Immutable;
using System.Globalization;
using System.Windows.Media;
using System.Xml.Linq;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Export;
using IntegrationPlanner.UI.Rendering;

namespace IntegrationPlanner.UI.Tests;

public sealed class SvgSceneWriterTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private readonly SvgSceneWriter _writer = new(new ScenePalette { LineColor = Color.FromRgb(0x1F, 0x56, 0x70), DashLength = 6, DashGap = 4 });

    [Fact]
    public void PlannedConnector_IsDashed()
    {
        DiagramScene scene = SceneWith(lines: [new SceneLine("planned", new ScenePoint(0, 0), new ScenePoint(10, 10), LineStyle.PlannedConnector)]);

        XElement line = Element(scene, "line");

        Assert.Equal("6 4", (string?)line.Attribute("stroke-dasharray"));
    }

    [Fact]
    public void DoneConnector_IsSolid()
    {
        DiagramScene scene = SceneWith(lines: [new SceneLine("done", new ScenePoint(0, 0), new ScenePoint(10, 10), LineStyle.Connector)]);

        XElement line = Element(scene, "line");

        Assert.Null(line.Attribute("stroke-dasharray"));
    }

    [Fact]
    public void Line_UsesThePaletteLineColour()
    {
        DiagramScene scene = SceneWith(lines: [new SceneLine("lane", new ScenePoint(0, 0), new ScenePoint(10, 0), LineStyle.Lane)]);

        XElement line = Element(scene, "line");

        Assert.Equal("#1F5670", (string?)line.Attribute("stroke"));
    }

    [Fact]
    public void CancelledBranchName_IsStruckThrough()
    {
        DiagramScene scene = SceneWith(texts: [TextOf(TextStyle.CancelledBranchName)]);

        XElement text = Element(scene, "text");

        Assert.Equal("line-through", (string?)text.Attribute("text-decoration"));
    }

    [Fact]
    public void Title_IsBold()
    {
        DiagramScene scene = SceneWith(texts: [TextOf(TextStyle.Title)]);

        XElement text = Element(scene, "text");

        Assert.Equal("bold", (string?)text.Attribute("font-weight"));
    }

    [Fact]
    public void RotatedText_IsRotatedAroundItsCentre()
    {
        DiagramScene scene = SceneWith(texts: [TextOf(TextStyle.ConnectorLabel) with { Angle = 72.5 }]);

        XElement group = Element(scene, "g");

        Assert.Equal("rotate(72.5 10.25 20.5)", (string?)group.Attribute("transform"));
    }

    [Fact]
    public void Numbers_AreWrittenInvariantlyWhateverTheCurrentCulture()
    {
        DiagramScene scene = SceneWith(texts: [TextOf(TextStyle.BranchName)]);

        string svg = WriteUnder(CultureInfo.GetCultureInfo("nl-NL"), scene);

        Assert.Equal("10.25", (string?)XDocument.Parse(svg).Descendants(Svg + "text").Single().Attribute("x"));
    }

    [Fact]
    public void ViewBox_MatchesTheSceneBounds()
    {
        DiagramScene scene = SceneWith();

        XElement root = XDocument.Parse(_writer.Write(scene)).Root!;

        Assert.Equal("-5.5 -10 100 50", (string?)root.Attribute("viewBox"));
    }

    private static DiagramScene SceneWith(ImmutableArray<SceneLine> lines = default, ImmutableArray<SceneText> texts = default) =>
        new(new SceneRect(-5.5, -10, 100, 50), lines.IsDefault ? [] : lines, texts.IsDefault ? [] : texts, []);

    private static SceneText TextOf(TextStyle style) =>
        new("text", "AD-1", new ScenePoint(10.25, 20.5), new SceneSize(40, 12), 0, 12, style, false);

    private XElement Element(DiagramScene scene, string name) =>
        XDocument.Parse(_writer.Write(scene)).Descendants(Svg + name).Single();

    private string WriteUnder(CultureInfo culture, DiagramScene scene)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
        try
        {
            return _writer.Write(scene);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
