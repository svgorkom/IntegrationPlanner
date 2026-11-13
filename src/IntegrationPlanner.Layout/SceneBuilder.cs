using System.Collections.Immutable;
using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout;

internal sealed class SceneBuilder
{
    private static readonly double[] RotatedLabelPositions = [0.5, 0.4, 0.6, 0.3, 0.7, 0.2, 0.8];
    private static readonly double[] BesideLabelPositions = [0.5, 0.35, 0.65];
    private const int BranchLabelShiftsPerLane = 8;
    private const double RotatedLabelMaxFraction = 0.8;

    private readonly Plan _plan;
    private readonly Scenario _scenario;
    private readonly TextBlockFactory _text;
    private readonly LayoutOptions _options;
    private readonly List<SceneLine> _lines = [];
    private readonly List<SceneText> _texts = [];
    private readonly List<SceneArrow> _arrows = [];
    private readonly LabelPlacer _placer = new();
    private readonly Dictionary<BranchId, LanePlacement> _lanes = [];
    private readonly List<LanePlacement> _orderedLanes = [];
    private readonly Dictionary<ConnectorId, SceneLine> _connectors = [];

    public SceneBuilder(Plan plan, Scenario scenario, TextBlockFactory text, LayoutOptions options)
    {
        _plan = plan;
        _scenario = scenario;
        _text = text;
        _options = options;
    }

    public DiagramScene Build()
    {
        PlaceLanes();
        PlaceConnectors();
        PlaceMarkers();
        PlaceBranchLabels();
        PlaceConnectorLabels();
        PlaceTitle();
        return new DiagramScene(ComputeBounds(), [.. _lines], [.. _texts], [.. _arrows]);
    }

    private double X(int step) => step * _options.StepWidth;

    private ScenePoint SlantEnd(ScenePoint from, double toY) =>
        new(from.X + (Math.Abs(toY - from.Y) * _options.Slope), toY);

    private void PlaceLanes()
    {
        Dictionary<BranchId, Branch> catalogue = _plan.Branches.ToDictionary(branch => branch.Id);
        List<(Lane Lane, Branch Branch)> lanes = _scenario.Lanes
            .Where(lane => catalogue.ContainsKey(lane.BranchId))
            .OrderBy(lane => lane.Order)
            .ThenBy(lane => lane.StartStep)
            .ThenBy(lane => lane.BranchId.Value, StringComparer.Ordinal)
            .Select(lane => (lane, catalogue[lane.BranchId]))
            .ToList();
        Dictionary<BranchId, double> laneY = lanes.ToDictionary(entry => entry.Lane.BranchId, entry => entry.Lane.Order * _options.LaneSpacing);

        foreach ((Lane lane, Branch branch) in lanes)
        {
            double y = laneY[lane.BranchId];
            double startX = X(lane.StartStep);
            bool isForked = false;
            if (lane.ShowFork && lane.ForkFromBranchId is { } parentId && laneY.TryGetValue(parentId, out double parentY))
            {
                ScenePoint forkFrom = new(X(lane.ForkStep ?? lane.StartStep), parentY);
                ScenePoint forkTo = SlantEnd(forkFrom, y);
                _lines.Add(new SceneLine(SceneKeys.Fork(lane.BranchId), forkFrom, forkTo, LineStyle.Fork));
                startX = forkTo.X;
                isForked = true;
            }

            double endX = Math.Max(X(lane.EndStep ?? Math.Max(_scenario.StepCount - 1, 0)), startX);
            _lines.Add(new SceneLine(SceneKeys.Lane(lane.BranchId), new ScenePoint(startX, y), new ScenePoint(endX, y), LineStyle.Lane));
            LanePlacement placement = new(lane, branch, y, startX, endX, isForked);
            _lanes[lane.BranchId] = placement;
            _orderedLanes.Add(placement);
        }
    }

    private void PlaceConnectors()
    {
        foreach (Connector connector in _scenario.Connectors)
        {
            if (!_lanes.TryGetValue(connector.FromBranchId, out LanePlacement? from) || !_lanes.TryGetValue(connector.ToBranchId, out LanePlacement? to))
            {
                continue;
            }

            ScenePoint start = new(X(connector.Step), from.Y);
            LineStyle style = connector.State == ProgressState.Done ? LineStyle.Connector : LineStyle.PlannedConnector;
            SceneLine line = new(SceneKeys.Connector(connector.Id), start, SlantEnd(start, to.Y), style);
            _lines.Add(line);
            _connectors[connector.Id] = line;
        }
    }

    private void PlaceMarkers()
    {
        foreach (Marker marker in _scenario.Markers)
        {
            ScenePoint? anchor = AnchorPoint(marker.Anchor);
            if (anchor is null)
            {
                continue;
            }

            (SceneArrow arrow, SceneText label) = MarkerArrowFactory.Create(marker, anchor.Value, _text);
            _arrows.Add(arrow);
            _texts.Add(label);
            _placer.Occupy(arrow.Bounds);
        }
    }

    private ScenePoint? AnchorPoint(MarkerAnchor anchor) => anchor switch
    {
        BranchStepAnchor branchStep when _lanes.TryGetValue(branchStep.BranchId, out LanePlacement? lane) => new ScenePoint(X(branchStep.Step), lane.Y),
        ConnectorAnchor connectorAnchor when _connectors.TryGetValue(connectorAnchor.ConnectorId, out SceneLine? line) => Interpolate(line, 0.5),
        _ => null,
    };

    private void PlaceBranchLabels()
    {
        foreach (LanePlacement lane in _orderedLanes)
        {
            TextBlock block = BranchLabel(lane);
            double centerX;
            double top;
            // A name with a summary sits above its own line, so lanes sharing a row never cover each other's labels.
            if (lane.IsForked || block.Lines.Length > 1)
            {
                centerX = lane.StartX + _options.LabelGap + (block.Width / 2);
                top = lane.Y - _options.LabelGap - block.Height;
            }
            else
            {
                // A bare name reads as a heading of its line, like "Develop ——".
                centerX = lane.StartX - _options.LabelGap - (block.Width / 2);
                top = lane.Y + (block.LastLineHeight / 2) - block.Height;
            }

            // Move up in small steps until clear; the range covers the label's own height plus one lane.
            double shift = _options.LaneSpacing / BranchLabelShiftsPerLane;
            int shiftCount = (int)Math.Ceiling((block.Height + _options.LaneSpacing) / shift);
            List<ImmutableArray<SceneText>> candidates = [.. Enumerable.Range(0, shiftCount + 1).Select(index => block.At(SceneKeys.BranchLabel(lane.Branch.Id), centerX, top - (index * shift)))];
            _texts.AddRange(_placer.Place(candidates));
        }
    }

    private TextBlock BranchLabel(LanePlacement lane)
    {
        Branch branch = lane.Branch;
        TextStyle nameStyle = branch.IsCancelled ? TextStyle.CancelledBranchName : TextStyle.BranchName;
        TextStyle summaryStyle = branch.IsCancelled ? TextStyle.CancelledBranchSummary : TextStyle.BranchSummary;
        if (!string.IsNullOrWhiteSpace(branch.JiraKey))
        {
            // A ticket branch shows only its key; the full name and summary are in the tooltip.
            string toolTip = string.IsNullOrWhiteSpace(branch.Summary) ? branch.Name : $"{branch.Name}{Environment.NewLine}{branch.Summary}";
            return new TextBlock([_text.Line(branch.JiraKey, nameStyle, _options.BranchNameFontSize)], toolTip);
        }

        double wrapWidth = Math.Clamp(lane.EndX - lane.StartX - (2 * _options.LabelGap), _options.MinimumSummaryWrapWidth, _options.SummaryWrapWidth);
        ImmutableArray<TextLine> summary = _text.Wrapped(branch.Summary, summaryStyle, _options.BranchSummaryFontSize, wrapWidth);
        return new TextBlock([_text.Line(branch.Name, nameStyle, _options.BranchNameFontSize), .. summary]);
    }

    private void PlaceConnectorLabels()
    {
        foreach (Connector connector in _scenario.Connectors)
        {
            if (string.IsNullOrWhiteSpace(connector.Label) || !_connectors.TryGetValue(connector.Id, out SceneLine? line))
            {
                continue;
            }

            _texts.AddRange(_placer.Place(ConnectorLabelCandidates(SceneKeys.ConnectorLabel(connector.Id), connector.Label, line)));
        }
    }

    private List<ImmutableArray<SceneText>> ConnectorLabelCandidates(string key, string label, SceneLine line)
    {
        List<ImmutableArray<SceneText>> candidates = [];
        double dx = line.To.X - line.From.X;
        double dy = line.To.Y - line.From.Y;
        double length = Math.Sqrt((dx * dx) + (dy * dy));

        TextLine single = _text.Line(label, TextStyle.ConnectorLabel, _options.ConnectorLabelFontSize);
        SceneSize padded = new(single.Size.Width + (2 * _options.LabelPadding), single.Size.Height);
        if (padded.Width <= length * RotatedLabelMaxFraction)
        {
            double angle = ReadableAngle(Math.Atan2(dy, dx) * 180.0 / Math.PI);
            candidates.AddRange(RotatedLabelPositions.Select(position =>
                ImmutableArray.Create(new SceneText(key, label, Interpolate(line, position), padded, angle, single.FontSize, single.Style, HasBackground: true))));
        }

        TextBlock block = new(_text.Wrapped(label, TextStyle.ConnectorLabel, _options.ConnectorLabelFontSize, _options.ConnectorLabelWrapWidth));
        double run = Math.Abs(dy) < double.Epsilon ? 0 : Math.Abs(dx / dy) * block.Height / 2;
        double offset = run + (2 * _options.LabelGap) + (block.Width / 2);
        foreach (double position in BesideLabelPositions)
        {
            ScenePoint point = Interpolate(line, position);
            double top = point.Y - (block.Height / 2);
            candidates.Add(block.At(key, point.X + offset, top));
            candidates.Add(block.At(key, point.X - offset, top));
        }

        return candidates;
    }

    private void PlaceTitle()
    {
        SceneRect content = ContentBounds();
        double bottom = content.Top - _options.TitleGap;
        List<TextLine> lines = [];
        if (!string.IsNullOrWhiteSpace(_plan.Title))
        {
            lines.Add(_text.Line(_plan.Title, TextStyle.Title, _options.TitleFontSize));
        }

        if (!string.IsNullOrWhiteSpace(_scenario.Name))
        {
            lines.Add(_text.Line(_scenario.Name, TextStyle.ScenarioName, _options.ScenarioNameFontSize));
        }

        double top = bottom - lines.Sum(line => line.Size.Height);
        foreach (TextLine line in lines)
        {
            ScenePoint center = new(content.Left + (line.Size.Width / 2), top + (line.Size.Height / 2));
            _texts.Add(new SceneText(SceneKeys.Title, line.Text, center, line.Size, 0, line.FontSize, line.Style, HasBackground: false));
            top += line.Size.Height;
        }
    }

    private SceneRect ContentBounds()
    {
        IEnumerable<SceneRect> areas = _lines.Select(line => line.Bounds)
            .Concat(_texts.Select(text => text.Bounds))
            .Concat(_arrows.Select(arrow => arrow.Bounds));
        return areas.Aggregate((SceneRect?)null, (total, area) => total?.Union(area) ?? area) ?? new SceneRect(0, 0, 0, 0);
    }

    private SceneRect ComputeBounds() => ContentBounds().Inflate(_options.Margin);

    private static ScenePoint Interpolate(SceneLine line, double fraction) =>
        new(line.From.X + ((line.To.X - line.From.X) * fraction), line.From.Y + ((line.To.Y - line.From.Y) * fraction));

    // Keeps rotated text from being drawn upside down.
    private static double ReadableAngle(double angle) => angle switch
    {
        > 90 => angle - 180,
        <= -90 => angle + 180,
        _ => angle,
    };
}
