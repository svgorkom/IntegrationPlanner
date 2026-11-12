using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout.Tests;

// Machine-independent text metrics, so golden output does not depend on installed fonts.
internal sealed class FixedWidthTextMeasurer : ITextMeasurer
{
    public const double CharacterWidthFactor = 0.5;
    public const double LineHeightFactor = 1.3;

    public SceneSize Measure(string text, TextStyle style, double fontSize) =>
        new(text.Length * fontSize * CharacterWidthFactor, fontSize * LineHeightFactor);
}
