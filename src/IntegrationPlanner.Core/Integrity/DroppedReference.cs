using IntegrationPlanner.Core.Model;

namespace IntegrationPlanner.Core.Integrity;

public sealed record DroppedReference(ScenarioId ScenarioId, DroppedItemKind ItemKind, string ItemId, string MissingId);
