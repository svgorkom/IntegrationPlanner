using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout;

public interface ITextMeasurer
{
    SceneSize Measure(string text, TextStyle style, double fontSize);
}
