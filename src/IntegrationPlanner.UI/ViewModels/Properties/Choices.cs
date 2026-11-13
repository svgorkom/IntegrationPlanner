using IntegrationPlanner.Core.Model;
using IntegrationPlanner.UI.Localization;

namespace IntegrationPlanner.UI.ViewModels.Properties;

public sealed record Choice<T>(T Value, string Text);

// Combo box item for a model item; Id is the item's ID value, or empty for "none".
public sealed record IdChoice(string Id, string Text);

public enum MarkerAnchorKind
{
    Branch,
    Connector,
}

public static class Choices
{
    public static IReadOnlyList<Choice<ConnectorKind>> ConnectorKinds { get; } =
    [
        new(ConnectorKind.Pull, UiText.ChoiceConnectorKindPull),
        new(ConnectorKind.Merge, UiText.ChoiceConnectorKindMerge),
    ];

    public static IReadOnlyList<Choice<ProgressState>> ProgressStates { get; } =
    [
        new(ProgressState.Planned, UiText.ChoiceProgressStatePlanned),
        new(ProgressState.Done, UiText.ChoiceProgressStateDone),
    ];

    public static IReadOnlyList<Choice<MarkerKind>> MarkerKinds { get; } =
    [
        new(MarkerKind.ReleaseBuild, UiText.ChoiceMarkerKindReleaseBuild),
        new(MarkerKind.IntegrationTest, UiText.ChoiceMarkerKindIntegrationTest),
        new(MarkerKind.Testing, UiText.ChoiceMarkerKindTesting),
        new(MarkerKind.Custom, UiText.ChoiceMarkerKindCustom),
    ];

    public static IReadOnlyList<Choice<MarkerAnchorKind>> MarkerAnchorKinds { get; } =
    [
        new(MarkerAnchorKind.Branch, UiText.ChoiceMarkerAnchorBranch),
        new(MarkerAnchorKind.Connector, UiText.ChoiceMarkerAnchorConnector),
    ];

    public static IReadOnlyList<IdChoice> Branches(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return [.. plan.Branches.Select(branch => new IdChoice(branch.Id.Value, BranchText(branch)))];
    }

    public static IReadOnlyList<IdChoice> Connectors(Plan plan, Scenario scenario)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(scenario);
        Dictionary<BranchId, Branch> branches = plan.Branches.ToDictionary(branch => branch.Id);
        string NameOf(BranchId id) => branches.TryGetValue(id, out Branch? branch) ? BranchText(branch) : id.Value;
        return
        [
            .. scenario.Connectors.Select(connector => new IdChoice(
                connector.Id.Value,
                UiText.Format(
                    UiText.ChoiceConnectorFormat,
                    ConnectorKinds.First(kind => kind.Value == connector.Kind).Text,
                    NameOf(connector.FromBranchId),
                    NameOf(connector.ToBranchId),
                    connector.Step))),
        ];
    }

    public static string BranchText(Branch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        return string.IsNullOrWhiteSpace(branch.Name) ? branch.Id.Value : branch.Name;
    }
}
