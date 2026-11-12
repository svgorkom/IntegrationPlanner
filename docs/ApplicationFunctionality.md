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
| UI | WPF shell: DI composition root, theme and `Strings.resx`. | `src/IntegrationPlanner.UI` |
| Sample | Scenario 3 from the reference diagram. | `samples/scenario3.iplan.json` |

## Available in P2

| Area | Functionality | Where |
|---|---|---|
| Layout | Pure, deterministic Scenario -> `DiagramScene` (lines, texts, arrows with style tokens). Lanes stacked by `Order` (mainline y = 0); x = step x step width; slanted connectors and optional fork lines; branch name + wrapped summary at the lane start; cancelled branches red and struck through; markers as thick arrows with the label in the shaft; Planned = dashed, Done = solid; labels moved until they do not overlap. Text is measured through `ITextMeasurer`, so Layout has no WPF reference. | `src/IntegrationPlanner.Layout` |
| Viewer | Read-only rendering of one scenario. Open a plan (toolbar or Ctrl+O), choose the scenario in the toolbar. A plan path given as the first command-line argument is opened at start-up. | `src/IntegrationPlanner.UI/MainWindow.xaml`, `ViewModels/MainViewModel.cs` |
| Zoom and pan | Toolbar - / + (25 % to 400 %), click the percentage to reset; Ctrl+mouse wheel, Ctrl+Plus, Ctrl+Minus, Ctrl+0. Pan with the scroll bars or by dragging with the left mouse button. | `Behaviors/ScrollViewerBehaviors.cs` |
| Export | PNG (rendered at 2x for sharpness) and SVG, written atomically. Copy to clipboard (Ctrl+Shift+C) as an image. | `src/IntegrationPlanner.UI/Export` |
| Logging | Rolling daily log file `%LocalAppData%\IntegrationPlanner\logs\integrationplanner-yyyyMMdd.log`, 7 files kept, level Information and up. Plan contents are never logged. A logging failure never reaches the app. | `src/IntegrationPlanner.UI/Logging` |
| Errors | An unexpected error is logged and shown as a friendly message without technical details; the app keeps running when the error happened on the UI thread. | `src/IntegrationPlanner.UI/Diagnostics` |

## File loading outcomes

| File | Outcome |
|---|---|
| Valid | Loaded; dropped dangling references (if any) are reported. |
| Invalid JSON, missing `schemaVersion` or required field, unknown enum value | `Corrupt`; file untouched. |
| `schemaVersion` newer than supported | `UnsupportedSchemaVersion`; file untouched. |
| Missing file / I/O error | Exception propagates to the caller; the viewer shows a "could not read" message. |

In the viewer, `Corrupt` and `UnsupportedSchemaVersion` are shown as messages, and dropped references are reported with their count.

## Planned (later phases)

| Phase | Functionality |
|---|---|
| P3 | Editing UI (property panel and canvas drag), scenario management, help pages. |
| Later | Jira/PR integration (D13). |
