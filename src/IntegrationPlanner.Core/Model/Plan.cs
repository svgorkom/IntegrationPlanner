using System.Collections.Immutable;

namespace IntegrationPlanner.Core.Model;

public sealed record Plan(string Title, ImmutableList<Branch> Branches, ImmutableList<Scenario> Scenarios)
{
    public static Plan Create(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        return new Plan(title, [], []);
    }

    public Scenario GetScenario(ScenarioId scenarioId) => Scenarios[IndexOfScenario(scenarioId)];

    public Plan WithScenario(ScenarioId scenarioId, Func<Scenario, Scenario> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        int index = IndexOfScenario(scenarioId);
        return this with { Scenarios = Scenarios.SetItem(index, update(Scenarios[index])) };
    }

    private int IndexOfScenario(ScenarioId scenarioId)
    {
        int index = Scenarios.FindIndex(scenario => scenario.Id == scenarioId);
        return index >= 0
            ? index
            : throw new KeyNotFoundException($"{nameof(Scenario)} '{scenarioId}' does not exist.");
    }
}
