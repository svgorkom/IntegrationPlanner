using CommunityToolkit.Mvvm.ComponentModel;
using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.UI.ViewModels.Properties;

public sealed partial class ConnectorPropertiesViewModel : PropertiesViewModel
{
    private IReadOnlyList<IdChoice> _branches = [];

    [ObservableProperty]
    private ConnectorKind _kind;

    [ObservableProperty]
    private string _fromId = string.Empty;

    [ObservableProperty]
    private string _toId = string.Empty;

    [ObservableProperty]
    private int _step;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private ProgressState _state;

    public ConnectorPropertiesViewModel(PlanEditor editor, ScenarioId scenarioId, ConnectorId connectorId)
        : base(editor, scenarioId)
    {
        ConnectorId = connectorId;
        Refresh();
    }

    public ConnectorId ConnectorId { get; }

    public IReadOnlyList<IdChoice> Branches => _branches;

    public IReadOnlyList<Choice<ConnectorKind>> Kinds => Choices.ConnectorKinds;

    public IReadOnlyList<Choice<ProgressState>> States => Choices.ProgressStates;

    protected override bool Load(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (CurrentConnector() is not Connector connector)
        {
            return false;
        }

        SetChoices(ref _branches, Choices.Branches(plan), nameof(Branches));
        Kind = connector.Kind;
        FromId = connector.FromBranchId.Value;
        ToId = connector.ToBranchId.Value;
        Step = connector.Step;
        Label = connector.Label ?? string.Empty;
        State = connector.State;
        return true;
    }

    partial void OnKindChanged(ConnectorKind value) => Commit();

    partial void OnFromIdChanged(string value) => Commit();

    partial void OnToIdChanged(string value) => Commit();

    partial void OnStepChanged(int value) => Commit();

    partial void OnLabelChanged(string value) => Commit();

    partial void OnStateChanged(ProgressState value) => Commit();

    private Connector? CurrentConnector() => CurrentScenario?.Connectors.Find(connector => connector.Id == ConnectorId);

    private void Commit()
    {
        if (IsRefreshing || CurrentConnector() is not Connector current || string.IsNullOrEmpty(FromId) || string.IsNullOrEmpty(ToId))
        {
            return;
        }

        Connector updated = current with
        {
            Kind = Kind,
            FromBranchId = new BranchId(FromId),
            ToBranchId = new BranchId(ToId),
            Step = Step,
            Label = string.IsNullOrEmpty(Label) ? null : Label,
            State = State,
        };
        if (updated != current)
        {
            Execute(new UpdateConnectorCommand(ScenarioId!.Value, updated));
        }
    }
}
