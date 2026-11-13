using IntegrationPlanner.Jira;
using IntegrationPlanner.UI.Localization;

namespace IntegrationPlanner.UI.ViewModels;

internal static class JiraFailureMessages
{
    public static string For(JiraFailure failure) => failure switch
    {
        JiraFailure.Unreachable => UiText.JiraFailureUnreachable,
        JiraFailure.Unauthorized => UiText.JiraFailureUnauthorized,
        JiraFailure.NotFound => UiText.JiraFailureNotFound,
        _ => UiText.JiraFailureOther,
    };
}
