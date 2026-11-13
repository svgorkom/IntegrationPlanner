using IntegrationPlanner.Layout;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.UI.Tests.Fakes;

// Font-independent text metrics so tests can lay out a real scene.
internal sealed class FixedWidthTextMeasurer : ITextMeasurer
{
    public SceneSize Measure(string text, TextStyle style, double fontSize) => new(text.Length * fontSize * 0.5, fontSize * 1.3);
}
