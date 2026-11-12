using System.Collections.Immutable;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout;

internal sealed record TextLine(string Text, TextStyle Style, double FontSize, SceneSize Size);

// A stack of text lines, each centred on the same vertical axis.
internal sealed class TextBlock
{
    public TextBlock(ImmutableArray<TextLine> lines)
    {
        Lines = lines;
        Width = lines.IsEmpty ? 0 : lines.Max(line => line.Size.Width);
        Height = lines.Sum(line => line.Size.Height);
    }

    public ImmutableArray<TextLine> Lines { get; }

    public double Width { get; }

    public double Height { get; }

    public double LastLineHeight => Lines.IsEmpty ? 0 : Lines[^1].Size.Height;

    public ImmutableArray<SceneText> At(string key, double centerX, double top)
    {
        ImmutableArray<SceneText>.Builder texts = ImmutableArray.CreateBuilder<SceneText>(Lines.Length);
        double y = top;
        foreach (TextLine line in Lines)
        {
            ScenePoint center = new(centerX, y + (line.Size.Height / 2));
            texts.Add(new SceneText(key, line.Text, center, line.Size, 0, line.FontSize, line.Style, HasBackground: false));
            y += line.Size.Height;
        }

        return texts.MoveToImmutable();
    }
}
