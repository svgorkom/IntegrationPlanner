using System.Collections.Immutable;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout;

internal sealed class TextBlockFactory
{
    private static readonly char[] WordSeparators = [' ', '\t', '\r', '\n'];

    private readonly ITextMeasurer _measurer;

    public TextBlockFactory(ITextMeasurer measurer, LayoutOptions options)
    {
        _measurer = measurer;
        Options = options;
    }

    public LayoutOptions Options { get; }

    public TextLine Line(string text, TextStyle style, double fontSize) =>
        new(text, style, fontSize, _measurer.Measure(text, style, fontSize));

    public ImmutableArray<TextLine> Wrapped(string text, TextStyle style, double fontSize, double maxWidth)
    {
        string[] words = text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
        ImmutableArray<TextLine>.Builder lines = ImmutableArray.CreateBuilder<TextLine>();
        string current = string.Empty;
        foreach (string word in words)
        {
            string candidate = current.Length == 0 ? word : $"{current} {word}";
            if (current.Length > 0 && _measurer.Measure(candidate, style, fontSize).Width > maxWidth)
            {
                lines.Add(Line(current, style, fontSize));
                current = word;
                continue;
            }

            current = candidate;
        }

        if (current.Length > 0)
        {
            lines.Add(Line(current, style, fontSize));
        }

        return lines.ToImmutable();
    }
}
