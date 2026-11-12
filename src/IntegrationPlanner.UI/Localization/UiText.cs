using System.Globalization;
using System.Resources;

namespace IntegrationPlanner.UI.Localization;

// Typed access to Strings.resx for code; each property name is its resource key.
public static class UiText
{
    private static readonly ResourceManager Strings = new("IntegrationPlanner.UI.Resources.Strings", typeof(UiText).Assembly);

    public static string MainWindowTitle => Get(nameof(MainWindowTitle));

    public static string MainWindowTitleWithFile => Get(nameof(MainWindowTitleWithFile));

    public static string MessageCaption => Get(nameof(MessageCaption));

    public static string DialogOpenPlanTitle => Get(nameof(DialogOpenPlanTitle));

    public static string DialogPlanFilter => Get(nameof(DialogPlanFilter));

    public static string DialogExportTitle => Get(nameof(DialogExportTitle));

    public static string DialogPngFilter => Get(nameof(DialogPngFilter));

    public static string DialogSvgFilter => Get(nameof(DialogSvgFilter));

    public static string ErrorOpenRead => Get(nameof(ErrorOpenRead));

    public static string ErrorOpenCorrupt => Get(nameof(ErrorOpenCorrupt));

    public static string ErrorOpenUnsupportedVersion => Get(nameof(ErrorOpenUnsupportedVersion));

    public static string InfoDroppedReferences => Get(nameof(InfoDroppedReferences));

    public static string ErrorExportFailed => Get(nameof(ErrorExportFailed));

    public static string ErrorCopyFailed => Get(nameof(ErrorCopyFailed));

    public static string ErrorUnexpected => Get(nameof(ErrorUnexpected));

    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Strings.GetString(key, CultureInfo.CurrentUICulture)
            ?? throw new KeyNotFoundException($"String resource '{key}' is missing from {nameof(Strings)}.resx.");
    }

    public static string Format(string format, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, format, arguments);
}
