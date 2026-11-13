using CommunityToolkit.Mvvm.ComponentModel;
using IntegrationPlanner.Core.Editing;
using IntegrationPlanner.Core.Editing.Commands;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.UI.ViewModels.Properties;

public sealed partial class MarkerPropertiesViewModel : PropertiesViewModel
{
    private IReadOnlyList<IdChoice> _branches = [];
    private IReadOnlyList<IdChoice> _connectors = [];

    [ObservableProperty]
    private MarkerKind _kind;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private double _angle;

    [ObservableProperty]
    private double _length;

    [ObservableProperty]
    private ProgressState _state;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBranchAnchor), nameof(IsConnectorAnchor))]
    private MarkerAnchorKind _anchorKind;

    [ObservableProperty]
    private string _anchorBranchId = string.Empty;

    [ObservableProperty]
    private int _anchorStep;

    [ObservableProperty]
    private string _anchorConnectorId = string.Empty;

    public MarkerPropertiesViewModel(PlanEditor editor, ScenarioId scenarioId, MarkerId markerId)
        : base(editor, scenarioId)
    {
        MarkerId = markerId;
        Refresh();
    }

    public MarkerId MarkerId { get; }

    public bool IsBranchAnchor => AnchorKind == MarkerAnchorKind.Branch;

    public bool IsConnectorAnchor => AnchorKind == MarkerAnchorKind.Connector;

    public IReadOnlyList<IdChoice> Branches => _branches;

    public IReadOnlyList<IdChoice> Connectors => _connectors;

    public IReadOnlyList<Choice<MarkerKind>> Kinds => Choices.MarkerKinds;

    public IReadOnlyList<Choice<ProgressState>> States => Choices.ProgressStates;

    public IReadOnlyList<Choice<MarkerAnchorKind>> AnchorKinds => Choices.MarkerAnchorKinds;

    protected override bool Load(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (CurrentScenario is not Scenario scenario || CurrentMarker() is not Marker marker)
        {
            return false;
        }

        SetChoices(ref _branches, Choices.Branches(plan), nameof(Branches));
        SetChoices(ref _connectors, Choices.Connectors(plan, scenario), nameof(Connectors));
        Kind = marker.Kind;
        Label = marker.Label;
        Angle = marker.Angle;
        Length = marker.Length;
        State = marker.State;
        AnchorKind = marker.Anchor is ConnectorAnchor ? MarkerAnchorKind.Connector : MarkerAnchorKind.Branch;
        AnchorBranchId = marker.Anchor is BranchStepAnchor branchAnchor ? branchAnchor.BranchId.Value : string.Empty;
        AnchorStep = marker.Anchor is BranchStepAnchor stepAnchor ? stepAnchor.Step : 0;
        AnchorConnectorId = marker.Anchor is ConnectorAnchor connectorAnchor ? connectorAnchor.ConnectorId.Value : string.Empty;
        return true;
    }

    partial void OnKindChanged(MarkerKind value) => Commit();

    partial void OnLabelChanged(string value) => Commit();

    partial void OnAngleChanged(double value) => Commit();

    partial void OnLengthChanged(double value) => Commit();

    partial void OnStateChanged(ProgressState value) => Commit();

    partial void OnAnchorKindChanged(MarkerAnchorKind value) => Commit();

    partial void OnAnchorBranchIdChanged(string value) => Commit();

    partial void OnAnchorStepChanged(int value) => Commit();

    partial void OnAnchorConnectorIdChanged(string value) => Commit();

    private Marker? CurrentMarker() => CurrentScenario?.Markers.Find(marker => marker.Id == MarkerId);

    // Switching the anchor kind picks the first available target; without one the change is not applied.
    private MarkerAnchor? BuildAnchor(Scenario scenario)
    {
        if (AnchorKind == MarkerAnchorKind.Connector)
        {
            string connectorId = string.IsNullOrEmpty(AnchorConnectorId) ? scenario.Connectors.FirstOrDefault()?.Id.Value ?? string.Empty : AnchorConnectorId;
            return connectorId.Length == 0 ? null : new ConnectorAnchor(new ConnectorId(connectorId));
        }

        string branchId = string.IsNullOrEmpty(AnchorBranchId)
            ? scenario.Lanes.OrderBy(lane => lane.Order).FirstOrDefault()?.BranchId.Value ?? string.Empty
            : AnchorBranchId;
        return branchId.Length == 0 ? null : new BranchStepAnchor(new BranchId(branchId), AnchorStep);
    }

    private void Commit()
    {
        if (IsRefreshing || CurrentScenario is not Scenario scenario || CurrentMarker() is not Marker current)
        {
            return;
        }

        MarkerAnchor? anchor = BuildAnchor(scenario);
        if (anchor is null)
        {
            Refresh();
            return;
        }

        Marker updated = current with
        {
            Kind = Kind,
            Label = Label,
            Angle = MarkerDirection.Normalize(Angle),
            Length = Math.Max(Length, Marker.MinimumLength),
            State = State,
            Anchor = anchor,
        };
        if (updated != current)
        {
            Execute(new UpdateMarkerCommand(scenario.Id, updated));
        }
    }
}
