using System.Windows.Media;

namespace IntegrationPlanner.UI.Rendering;

// Diagram colours and strokes; the instance used by the app is declared in Themes/Theme.xaml.
public sealed class ScenePalette
{
    public Color LineColor { get; set; } = Colors.Black;

    public Color TextColor { get; set; } = Colors.Black;

    public Color CancelledColor { get; set; } = Colors.Red;

    public Color MarkerFillColor { get; set; } = Colors.Black;

    public Color MarkerTextColor { get; set; } = Colors.White;

    public Color BackgroundColor { get; set; } = Colors.White;

    public FontFamily FontFamily { get; set; } = new("Segoe UI");

    public double LineThickness { get; set; } = 1;

    public double MarkerOutlineThickness { get; set; } = 1;

    public double DashLength { get; set; } = 6;

    public double DashGap { get; set; } = 4;
}
