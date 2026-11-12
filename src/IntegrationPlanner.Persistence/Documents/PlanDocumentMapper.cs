using System.Collections.Immutable;
using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Persistence.Documents;

internal static class PlanDocumentMapper
{
    public static PlanDocument ToDocument(Plan plan, int schemaVersion) =>
        new()
        {
            SchemaVersion = schemaVersion,
            Title = plan.Title,
            Branches = [.. plan.Branches.Select(ToDocument)],
            Scenarios = [.. plan.Scenarios.Select(ToDocument)],
        };

    public static Plan ToPlan(PlanDocument document) =>
        new(
            document.Title ?? string.Empty,
            [.. (document.Branches ?? []).Select(ToBranch)],
            [.. (document.Scenarios ?? []).Select(ToScenario)]);

    private static BranchDocument ToDocument(Branch branch) =>
        new()
        {
            Id = branch.Id.Value,
            Name = branch.Name,
            Summary = branch.Summary,
            IsMainline = branch.IsMainline,
            IsCancelled = branch.IsCancelled,
        };

    private static ScenarioDocument ToDocument(Scenario scenario) =>
        new()
        {
            Id = scenario.Id.Value,
            Name = scenario.Name,
            StepCount = scenario.StepCount,
            Lanes = [.. scenario.Lanes.Select(ToDocument)],
            Connectors = [.. scenario.Connectors.Select(ToDocument)],
            Markers = [.. scenario.Markers.Select(ToDocument)],
        };

    private static LaneDocument ToDocument(Lane lane) =>
        new()
        {
            BranchId = lane.BranchId.Value,
            Order = lane.Order,
            StartStep = lane.StartStep,
            EndStep = lane.EndStep,
            ShowFork = lane.ShowFork,
            ForkFromBranchId = lane.ForkFromBranchId?.Value,
            ForkStep = lane.ForkStep,
        };

    private static ConnectorDocument ToDocument(Connector connector) =>
        new()
        {
            Id = connector.Id.Value,
            Kind = connector.Kind,
            FromBranchId = connector.FromBranchId.Value,
            ToBranchId = connector.ToBranchId.Value,
            Step = connector.Step,
            Label = connector.Label,
            State = connector.State,
        };

    private static MarkerDocument ToDocument(Marker marker) =>
        new()
        {
            Id = marker.Id.Value,
            Kind = marker.Kind,
            Label = marker.Label,
            Anchor = marker.Anchor switch
            {
                BranchStepAnchor anchor => new AnchorDocument { BranchId = anchor.BranchId.Value, Step = anchor.Step },
                ConnectorAnchor anchor => new AnchorDocument { ConnectorId = anchor.ConnectorId.Value },
                _ => throw new ArgumentOutOfRangeException(nameof(marker), marker.Anchor, $"Unsupported {nameof(MarkerAnchor)} type."),
            },
            Direction = marker.Direction,
            State = marker.State,
        };

    private static Branch ToBranch(BranchDocument document) =>
        new(
            new BranchId(Require(document.Id, "branch.id")),
            Require(document.Name, "branch.name"),
            document.Summary ?? string.Empty,
            document.IsMainline,
            document.IsCancelled);

    private static Scenario ToScenario(ScenarioDocument document) =>
        new(
            new ScenarioId(Require(document.Id, "scenario.id")),
            Require(document.Name, "scenario.name"),
            Require(document.StepCount, "scenario.stepCount"),
            [.. (document.Lanes ?? []).Select(ToLane)],
            [.. (document.Connectors ?? []).Select(ToConnector)],
            [.. (document.Markers ?? []).Select(ToMarker)]);

    private static Lane ToLane(LaneDocument document) =>
        new(
            new BranchId(Require(document.BranchId, "lane.branchId")),
            Require(document.Order, "lane.order"),
            Require(document.StartStep, "lane.startStep"),
            document.EndStep,
            document.ShowFork,
            string.IsNullOrWhiteSpace(document.ForkFromBranchId) ? null : new BranchId(document.ForkFromBranchId),
            document.ForkStep);

    private static Connector ToConnector(ConnectorDocument document) =>
        new(
            new ConnectorId(Require(document.Id, "connector.id")),
            Require(document.Kind, "connector.kind"),
            new BranchId(Require(document.FromBranchId, "connector.fromBranchId")),
            new BranchId(Require(document.ToBranchId, "connector.toBranchId")),
            Require(document.Step, "connector.step"),
            document.Label,
            Require(document.State, "connector.state"));

    private static Marker ToMarker(MarkerDocument document) =>
        new(
            new MarkerId(Require(document.Id, "marker.id")),
            Require(document.Kind, "marker.kind"),
            document.Label ?? string.Empty,
            ToAnchor(document.Anchor),
            Require(document.Direction, "marker.direction"),
            Require(document.State, "marker.state"));

    private static MarkerAnchor ToAnchor(AnchorDocument? document)
    {
        if (document is null)
        {
            throw new PlanFormatException("Missing value for 'marker.anchor'.");
        }

        if (!string.IsNullOrWhiteSpace(document.ConnectorId))
        {
            return new ConnectorAnchor(new ConnectorId(document.ConnectorId));
        }

        return new BranchStepAnchor(
            new BranchId(Require(document.BranchId, "marker.anchor.branchId")),
            Require(document.Step, "marker.anchor.step"));
    }

    private static string Require(string? value, string property) =>
        string.IsNullOrWhiteSpace(value) ? throw new PlanFormatException($"Missing value for '{property}'.") : value;

    private static T Require<T>(T? value, string property)
        where T : struct =>
        value ?? throw new PlanFormatException($"Missing value for '{property}'.");
}
