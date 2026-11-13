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

    public static string MainWindowTitleWithFileUnsaved => Get(nameof(MainWindowTitleWithFileUnsaved));

    public static string UntitledFileName => Get(nameof(UntitledFileName));

    public static string DialogSavePlanTitle => Get(nameof(DialogSavePlanTitle));

    public static string QuestionSaveChanges => Get(nameof(QuestionSaveChanges));

    public static string ErrorSaveFailed => Get(nameof(ErrorSaveFailed));

    public static string DefaultPlanTitle => Get(nameof(DefaultPlanTitle));

    public static string DefaultScenarioName => Get(nameof(DefaultScenarioName));

    public static string DuplicateScenarioName => Get(nameof(DuplicateScenarioName));

    public static string DefaultBranchName => Get(nameof(DefaultBranchName));

    public static string DefaultMarkerLabel => Get(nameof(DefaultMarkerLabel));

    public static string ChoiceNone => Get(nameof(ChoiceNone));

    public static string ChoiceConnectorKindPull => Get(nameof(ChoiceConnectorKindPull));

    public static string ChoiceConnectorKindMerge => Get(nameof(ChoiceConnectorKindMerge));

    public static string ChoiceProgressStatePlanned => Get(nameof(ChoiceProgressStatePlanned));

    public static string ChoiceProgressStateDone => Get(nameof(ChoiceProgressStateDone));

    public static string ChoiceMarkerKindReleaseBuild => Get(nameof(ChoiceMarkerKindReleaseBuild));

    public static string ChoiceMarkerKindIntegrationTest => Get(nameof(ChoiceMarkerKindIntegrationTest));

    public static string ChoiceMarkerKindTesting => Get(nameof(ChoiceMarkerKindTesting));

    public static string ChoiceMarkerKindCustom => Get(nameof(ChoiceMarkerKindCustom));

    public static string ChoiceMarkerAnchorBranch => Get(nameof(ChoiceMarkerAnchorBranch));

    public static string ChoiceMarkerAnchorConnector => Get(nameof(ChoiceMarkerAnchorConnector));

    public static string ChoiceConnectorFormat => Get(nameof(ChoiceConnectorFormat));

    public static string JiraConnectedFormat => Get(nameof(JiraConnectedFormat));

    public static string JiraSettingsIncomplete => Get(nameof(JiraSettingsIncomplete));

    public static string JiraMainlineRequired => Get(nameof(JiraMainlineRequired));

    public static string JiraFailureUnreachable => Get(nameof(JiraFailureUnreachable));

    public static string JiraFailureUnauthorized => Get(nameof(JiraFailureUnauthorized));

    public static string JiraFailureNotFound => Get(nameof(JiraFailureNotFound));

    public static string JiraFailureOther => Get(nameof(JiraFailureOther));

    public static string JiraOpenPageFailed => Get(nameof(JiraOpenPageFailed));

    public static string JiraNoResults => Get(nameof(JiraNoResults));

    public static string JiraDuplicateTicketFormat => Get(nameof(JiraDuplicateTicketFormat));

    public static string JiraDuplicateNameFormat => Get(nameof(JiraDuplicateNameFormat));

    public static string ErrorJiraSettingsLoad => Get(nameof(ErrorJiraSettingsLoad));

    public static string ErrorJiraSettingsSave => Get(nameof(ErrorJiraSettingsSave));

    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Strings.GetString(key, CultureInfo.CurrentUICulture)
            ?? throw new KeyNotFoundException($"String resource '{key}' is missing from {nameof(Strings)}.resx.");
    }

    public static string Format(string format, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, format, arguments);
}
