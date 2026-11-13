namespace IntegrationPlanner.Layout;

public sealed record LayoutOptions
{
    public static LayoutOptions Default { get; } = new();

    public double StepWidth { get; init; } = 90;

    public double LaneSpacing { get; init; } = 70;

    // Horizontal run per unit of vertical drop, so every connector and fork has the same slant.
    public double Slope { get; init; } = 0.3;

    public double TitleFontSize { get; init; } = 16;

    public double ScenarioNameFontSize { get; init; } = 15;

    public double BranchNameFontSize { get; init; } = 15;

    public double BranchSummaryFontSize { get; init; } = 10.5;

    public double ConnectorLabelFontSize { get; init; } = 11;

    public double MarkerLabelFontSize { get; init; } = 14;

    public double SummaryWrapWidth { get; init; } = 230;

    // Summaries wrap to their lane's length so they stay above their own line, but never narrower than this.
    public double MinimumSummaryWrapWidth { get; init; } = 120;

    public double ConnectorLabelWrapWidth { get; init; } = 150;

    public double LabelGap { get; init; } = 4;

    public double LabelPadding { get; init; } = 3;

    public double MarkerShaftWidth { get; init; } = 28;

    public double MarkerHeadWidth { get; init; } = 64;

    public double MarkerHeadLength { get; init; } = 30;

    public double MarkerGap { get; init; } = 2;

    public double TitleGap { get; init; } = 40;

    public double Margin { get; init; } = 20;
}
