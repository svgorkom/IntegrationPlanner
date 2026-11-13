namespace IntegrationPlanner.Core.Model;

// Marker angles in degrees, clockwise with y pointing down: the direction the arrow points in.
public static class MarkerDirection
{
    public const double FromLeft = 0;
    public const double FromTop = 90;
    public const double FromRight = 180;
    public const double FromBottom = 270;

    public static double Normalize(double angle)
    {
        double normalized = angle % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}
