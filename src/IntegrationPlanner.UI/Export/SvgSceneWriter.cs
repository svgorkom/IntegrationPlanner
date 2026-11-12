using System.Globalization;
using System.Windows.Media;
using System.Xml.Linq;
using IntegrationPlanner.Layout.Scene;
using IntegrationPlanner.UI.Rendering;

namespace IntegrationPlanner.UI.Export;

public sealed class SvgSceneWriter
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private readonly ScenePalette _palette;

    public SvgSceneWriter(ScenePalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        _palette = palette;
    }

    public string Write(DiagramScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        SceneRect bounds = scene.Bounds;
        XElement root = new(
            Svg + "svg",
            new XAttribute("width", Number(bounds.Width)),
            new XAttribute("height", Number(bounds.Height)),
            new XAttribute("viewBox", $"{Number(bounds.Left)} {Number(bounds.Top)} {Number(bounds.Width)} {Number(bounds.Height)}"),
            Rectangle(bounds, _palette.BackgroundColor));
        root.Add(scene.Lines.Select(Line));
        root.Add(scene.Arrows.Select(Arrow));
        root.Add(scene.Texts.Select(Text));
        XDocument document = new(new XDeclaration("1.0", "utf-8", null), root);
        return $"{document.Declaration}{Environment.NewLine}{root}";
    }

    private XElement Line(SceneLine line)
    {
        XElement element = new(
            Svg + "line",
            new XAttribute("x1", Number(line.From.X)),
            new XAttribute("y1", Number(line.From.Y)),
            new XAttribute("x2", Number(line.To.X)),
            new XAttribute("y2", Number(line.To.Y)),
            new XAttribute("stroke", Hex(_palette.LineColor)),
            new XAttribute("stroke-width", Number(_palette.LineThickness)));
        if (SceneStyles.IsDashed(line.Style))
        {
            element.Add(new XAttribute("stroke-dasharray", DashArray()));
        }

        return element;
    }

    private XElement Arrow(SceneArrow arrow)
    {
        XElement element = new(
            Svg + "polygon",
            new XAttribute("points", string.Join(' ', arrow.Outline.Select(point => $"{Number(point.X)},{Number(point.Y)}"))));
        if (arrow.Style == ArrowStyle.Done)
        {
            element.Add(new XAttribute("fill", Hex(_palette.MarkerFillColor)));
            return element;
        }

        element.Add(
            new XAttribute("fill", Hex(_palette.BackgroundColor)),
            new XAttribute("stroke", Hex(_palette.MarkerFillColor)),
            new XAttribute("stroke-width", Number(_palette.MarkerOutlineThickness)),
            new XAttribute("stroke-dasharray", DashArray()));
        return element;
    }

    private XElement Text(SceneText text)
    {
        XElement group = new(Svg + "g");
        if (text.Angle != 0)
        {
            group.Add(new XAttribute("transform", $"rotate({Number(text.Angle)} {Number(text.Center.X)} {Number(text.Center.Y)})"));
        }

        if (text.HasBackground)
        {
            SceneRect background = new(
                text.Center.X - (text.Size.Width / 2),
                text.Center.Y - (text.Size.Height / 2),
                text.Size.Width,
                text.Size.Height);
            group.Add(Rectangle(background, _palette.BackgroundColor));
        }

        XElement element = new(
            Svg + "text",
            new XAttribute("x", Number(text.Center.X)),
            new XAttribute("y", Number(text.Center.Y)),
            new XAttribute("text-anchor", "middle"),
            new XAttribute("dominant-baseline", "central"),
            new XAttribute("font-family", _palette.FontFamily.Source),
            new XAttribute("font-size", Number(text.FontSize)),
            new XAttribute("fill", Hex(SceneStyles.TextColor(_palette, text.Style))),
            text.Text);
        if (SceneStyles.IsBold(text.Style))
        {
            element.Add(new XAttribute("font-weight", "bold"));
        }

        if (SceneStyles.IsStruckThrough(text.Style))
        {
            element.Add(new XAttribute("text-decoration", "line-through"));
        }

        group.Add(element);
        return group;
    }

    private string DashArray() => $"{Number(_palette.DashLength)} {Number(_palette.DashGap)}";

    private static XElement Rectangle(SceneRect rect, Color fill) =>
        new(
            Svg + "rect",
            new XAttribute("x", Number(rect.Left)),
            new XAttribute("y", Number(rect.Top)),
            new XAttribute("width", Number(rect.Width)),
            new XAttribute("height", Number(rect.Height)),
            new XAttribute("fill", Hex(fill)));

    private static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string Number(double value) => Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
}
