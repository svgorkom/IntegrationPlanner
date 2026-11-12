using System.Globalization;
using System.Windows;
using System.Windows.Media;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.UI.Rendering;

// Maps layout style tokens to visual attributes, shared by screen rendering, measuring and SVG export.
public static class SceneStyles
{
    public static bool IsDashed(LineStyle style) => style == LineStyle.PlannedConnector;

    public static bool IsBold(TextStyle style) => style == TextStyle.Title;

    public static bool IsStruckThrough(TextStyle style) =>
        style is TextStyle.CancelledBranchName or TextStyle.CancelledBranchSummary;

    public static Color TextColor(ScenePalette palette, TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(palette);
        return style switch
        {
            TextStyle.CancelledBranchName or TextStyle.CancelledBranchSummary => palette.CancelledColor,
            TextStyle.MarkerLabel => palette.MarkerTextColor,
            TextStyle.PlannedMarkerLabel => palette.MarkerFillColor,
            _ => palette.TextColor,
        };
    }

    public static FormattedText Format(string text, TextStyle style, double fontSize, ScenePalette palette, Brush brush, double pixelsPerDip)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(brush);
        Typeface typeface = new(palette.FontFamily, FontStyles.Normal, IsBold(style) ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
        FormattedText formatted = new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize, brush, pixelsPerDip);
        if (IsStruckThrough(style))
        {
            formatted.SetTextDecorations(TextDecorations.Strikethrough);
        }

        return formatted;
    }
}
