using System.Collections.Immutable;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout;

// Greedy, order-dependent placement: each label takes its first candidate that does not overlap anything placed before it.
internal sealed class LabelPlacer
{
    private readonly List<SceneRect> _occupied = [];

    public void Occupy(SceneRect area) => _occupied.Add(area);

    public ImmutableArray<SceneText> Place(IReadOnlyList<ImmutableArray<SceneText>> candidates)
    {
        ImmutableArray<SceneText> chosen = candidates.FirstOrDefault(IsFree, candidates[0]);
        foreach (SceneText text in chosen)
        {
            Occupy(text.Bounds);
        }

        return chosen;
    }

    private bool IsFree(ImmutableArray<SceneText> candidate) =>
        !candidate.Any(text => _occupied.Any(area => area.Overlaps(text.Bounds)));
}
