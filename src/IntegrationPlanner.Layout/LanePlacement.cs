using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Layout;

internal sealed record LanePlacement(Lane Lane, Branch Branch, double Y, double StartX, double EndX, bool IsForked);
