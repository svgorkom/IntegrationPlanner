using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace IntegrationPlanner.UI.Services;

// Mica backdrop and immersive dark title for a window (Windows 11); without support the window keeps its solid background.
public static class WindowBackdrop
{
    private const int MinimumMicaBuild = 22621;
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmSystemBackdropType = 38;
    private const int BackdropMainWindow = 2;

    public static bool Apply(Window window, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (Environment.OSVersion.Version.Build < MinimumMicaBuild)
        {
            return false;
        }

        IntPtr handle = new WindowInteropHelper(window).EnsureHandle();
        HwndSource? source = HwndSource.FromHwnd(handle);
        if (source?.CompositionTarget is null)
        {
            return false;
        }

        source.CompositionTarget.BackgroundColor = Colors.Transparent;
        int dark = isDark ? 1 : 0;
        int backdrop = BackdropMainWindow;
        _ = DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref dark, sizeof(int));
        return DwmSetWindowAttribute(handle, DwmSystemBackdropType, ref backdrop, sizeof(int)) == 0;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
