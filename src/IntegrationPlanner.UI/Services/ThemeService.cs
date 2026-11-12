using System.Windows;
using Microsoft.Win32;

namespace IntegrationPlanner.UI.Services;

// Switches between the light and dark variants: the Fluent control theme plus the colour dictionary in Themes/.
public sealed class ThemeService
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";
    private const string ColorsLightSource = "Themes/Colors.Light.xaml";
    private const string ColorsDarkSource = "Themes/Colors.Dark.xaml";

    public event EventHandler? Changed;

    public bool IsDark { get; private set; }

    public void ApplySystemTheme() => Apply(IsSystemThemeDark());

    public void Toggle() => Apply(!IsDark);

    private static bool IsSystemThemeDark()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue(AppsUseLightThemeValue) is int appsUseLight && appsUseLight == 0;
    }

    private static void ReplaceColors(Application application, string source)
    {
        System.Collections.ObjectModel.Collection<ResourceDictionary> dictionaries = application.Resources.MergedDictionaries;
        int index = dictionaries.ToList().FindIndex(dictionary => dictionary.Source?.OriginalString.Contains("Colors.", StringComparison.Ordinal) == true);
        ResourceDictionary colors = new() { Source = new Uri(source, UriKind.Relative) };
        if (index < 0)
        {
            dictionaries.Insert(0, colors);
            return;
        }

        dictionaries[index] = colors;
    }

    private void Apply(bool isDark)
    {
        Application application = Application.Current;
        IsDark = isDark;
        ReplaceColors(application, isDark ? ColorsDarkSource : ColorsLightSource);
        application.ThemeMode = isDark ? ThemeMode.Dark : ThemeMode.Light;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
