using System.Globalization;
using System.Resources;
using System.Windows.Markup;

namespace IntegrationPlanner.UI.Localization;

[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    private static readonly ResourceManager Strings = new("IntegrationPlanner.UI.Resources.Strings", typeof(LocExtension).Assembly);

    public LocExtension()
    {
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Key, nameof(Key));
        return Strings.GetString(Key, CultureInfo.CurrentUICulture)
            ?? throw new KeyNotFoundException($"String resource '{Key}' is missing from {nameof(Strings)}.resx.");
    }
}
