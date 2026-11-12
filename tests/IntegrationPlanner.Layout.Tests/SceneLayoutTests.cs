using IntegrationPlanner.Core.Model;
using IntegrationPlanner.Layout.Scene;

namespace IntegrationPlanner.Layout.Tests;

public sealed class SceneLayoutTests
{
    [Fact]
    public void Scene_IsHeadedByThePlanTitleAndScenarioName()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline()]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.Equal(["Plan title", "Scenario name"], scene.Texts(SceneKeys.Title).Select(text => text.Text));
    }

    [Fact]
    public void Title_SitsAboveTheDiagram()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline(), TestPlan.LaneOf(TestPlan.Feature, -2, 0, 4)]);

        DiagramScene scene = TestPlan.Layout(plan);

        Assert.True(scene.Texts(SceneKeys.Title).BoundsOf().Bottom < scene.Texts(SceneKeys.BranchLabel(TestPlan.Feature)).BoundsOf().Top);
    }

    [Fact]
    public void Bounds_ContainEveryElement()
    {
        Plan plan = TestPlan.Create(
            [TestPlan.Mainline(), TestPlan.LaneOf(TestPlan.Feature, -2, 0, 4)],
            [TestPlan.Merge("merge", TestPlan.Feature, TestPlan.Develop, 4, "Merge")],
            [TestPlan.MarkerOn(new BranchStepAnchor(TestPlan.Develop, 6), MarkerDirection.FromBottom)]);

        DiagramScene scene = TestPlan.Layout(plan);

        SceneRect content = scene.Texts.Select(text => text.Bounds).Concat(scene.Lines.Select(line => line.Bounds)).Concat(scene.Arrows.Select(arrow => arrow.Bounds)).Aggregate((total, area) => total.Union(area));
        Assert.Equal(content.Inflate(TestPlan.Options.Margin), scene.Bounds);
    }

    [Fact]
    public void UnknownScenario_IsRejected()
    {
        Plan plan = TestPlan.Create([TestPlan.Mainline()]);
        DiagramLayoutEngine engine = new(new FixedWidthTextMeasurer(), TestPlan.Options);

        Assert.Throws<KeyNotFoundException>(() => engine.Layout(plan, new ScenarioId("other")));
    }
}
