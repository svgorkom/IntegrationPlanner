# Application functionality

IntegrationPlanner is a planning and visualization tool for branch integration diagrams such as `docs/reference/branching-strategy-scenario3.png`. It has no business rules (D1). Phases and scope: `docs/Design.md`.

## Goals

- Model a plan as structured data (branches, pulls, merges, markers) and let the app compute the diagram layout (D2).
- Keep several scenarios in one plan file, sharing one branch catalogue (D9).
- Track progress: pulls, merges and markers are Planned or Done (D8).
- Share diagrams as PNG/SVG and via the clipboard (D11).

## Available in P1

| Area | Functionality | Where |
|---|---|---|
| Editing | Commands: set plan title; add/update/delete branch; add/update/delete scenario; set/remove lane; add/update/delete connector; add/update/delete marker. | `src/IntegrationPlanner.Core/Editing/Commands` |
| Undo/redo | `PlanEditor` executes commands and keeps snapshot history. A new command clears redo. A failing command leaves plan and history unchanged. | `src/IntegrationPlanner.Core/Editing/PlanEditor.cs` |
| Consistency | Cascade deletes; dangling references dropped on load and reported. | `src/IntegrationPlanner.Core/Integrity` |
| Files | Load/save `*.iplan.json` (camelCase, `schemaVersion` 1). Save is atomic: written to a temp file in the same folder, then moved over the target. | `src/IntegrationPlanner.Persistence/PlanFileStore.cs` |
| UI | WPF shell: empty main window, DI composition root, theme and `Strings.resx`. | `src/IntegrationPlanner.UI` |
| Sample | Scenario 3 from the reference diagram. | `samples/scenario3.iplan.json` |

## File loading outcomes

| File | Outcome |
|---|---|
| Valid | Loaded; dropped dangling references (if any) are reported. |
| Invalid JSON, missing `schemaVersion` or required field, unknown enum value | `Corrupt`; file untouched. |
| `schemaVersion` newer than supported | `UnsupportedSchemaVersion`; file untouched. |
| Missing file / I/O error | Exception propagates to the caller. |

## Planned (later phases)

| Phase | Functionality |
|---|---|
| P2 | Automatic layout in the screenshot style, read-only rendering, PNG/SVG export, copy to clipboard. |
| P3 | Editing UI (property panel and canvas drag), scenario management, help pages. |
| Later | Jira/PR integration (D13). |
