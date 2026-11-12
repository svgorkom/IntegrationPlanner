using System.Windows.Media;
using IntegrationPlanner.Layout;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.UI.Rendering;

public sealed class WpfTextMeasurer : ITextMeasurer
{
    // Layout works in device-independent units, so text is measured at one pixel per DIP.
    private const double LayoutPixelsPerDip = 1.0;

    private readonly ScenePalette _palette;

    public WpfTextMeasurer(ScenePalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        _palette = palette;
    }

    public SceneSize Measure(string text, TextStyle style, double fontSize)
    {
        ArgumentNullException.ThrowIfNull(text);
        FormattedText formatted = SceneStyles.Format(text, style, fontSize, _palette, Brushes.Black, LayoutPixelsPerDip);
        return new SceneSize(formatted.WidthIncludingTrailingWhitespace, formatted.Height);
    }
}
