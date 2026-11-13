namespace IntegrationPlanner.Layout.Scene;

// One line of text, centred on Center and rotated by Angle degrees (clockwise, y pointing down) around it.
public sealed record SceneText(string Key, string Text, ScenePoint Center, SceneSize Size, double Angle, double FontSize, TextStyle Style, bool HasBackground, string? ToolTip = null)
{
    public SceneRect Bounds => SceneGeometry.RotatedBounds(Center, Size, Angle);
}
