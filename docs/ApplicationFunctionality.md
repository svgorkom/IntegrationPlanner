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

## Available in P3

| Area | Functionality | Where |
|---|---|---|
| Documents | New (Ctrl+N) creates "Untitled plan" with one 10-step scenario. Save (Ctrl+S) writes to the current file, or asks for one; Save As (Ctrl+Shift+S) always asks. The window title shows `*` when there are unsaved changes; undoing every edit clears it. Before New, Open or closing the app with unsaved changes the app asks Yes (save) / No (discard) / Cancel (D18). A failed save shows a message and keeps the changes. | `ViewModels/MainViewModel.cs` |
| Explorer (left) | Scenarios: add, duplicate, delete, select (replaces the P2 toolbar selector). Branch catalogue: add, select. Adding a branch also shows it in the current scenario. | `MainWindow.xaml`, `ViewModels/MainViewModel.Editing.cs` |
| Toolbar | Add branch, Add pull, Add merge, Add marker (pull/merge/marker need at least one lane in the scenario), Delete (Del), Undo (Ctrl+Z), Redo (Ctrl+Y). New items get defaults (`Editing/PlanItemFactory.cs`) and are selected, so they can be adjusted in the property panel. | `MainWindow.xaml` |
| Property panel (right) | Shows the selection: plan title and current scenario (name, step count) when nothing is selected; branch (catalogue fields shared by all scenarios, plus the lane in this scenario: shown, order, start/end step, fork); pull/merge (kind, from, to, step, label, state); marker (kind, label, angle, length, state, anchor on a branch step or a connector; a selected marker also shows a handle at its tail that is dragged to turn and stretch it smoothly, as one undo step). A field commits when it loses focus or on Enter; each change is one undo step. No value is rejected (D1). | `ViewModels/Properties`, `Themes/PropertyPanel.xaml` |
| Canvas | Click selects (lane, fork or branch label selects the branch; a connector label selects its connector); click on empty space selects the plan. Left-drag on a lane moves it between lane rows (`Order`); on a pull/merge or a branch-anchored marker it moves between steps (`Step`); the drag snaps and shows a preview, and the result is one undo step. Attached markers move along. Left-drag on empty space pans (D16). | `Rendering/SceneCanvas.cs`, `Rendering/SceneHitTester.cs`, `Editing/DragPlanner.cs` |
| Delete | Deleting a selected branch removes it from the catalogue, so from every scenario, with its lanes, connectors and markers (cascade, undoable). To hide a branch in one scenario only, clear "Shown in this scenario" in the property panel. | `ViewModels/MainViewModel.Editing.cs` |

## Jira Data Center (one-way import)

| Area | Functionality | Where |
|---|---|---|
| Connection | Menu "Jira connection...": server address (context path allowed, http allowed), Personal Access Token (Bearer, REST API v2), mainline branch name (default `develop`). The token is stored in Windows Credential Manager, not in the settings file. The dialog has a 4-step help box and an "Open Jira profile" button. | `ViewModels/JiraSettingsViewModel.cs`, `src/IntegrationPlanner.Jira` |
| Add from ticket | Menu "Add branch from Jira ticket...": pick a ticket; the branch name is generated from the ticket and can be overridden. One undo step adds the branch, its lane and a Pull from the mainline lane (the mainline branch is created if missing). A ticket or name that already exists is rejected. | `ViewModels/AddBranchFromTicketViewModel.cs`, `Editing/TicketBranchFactory.cs` |
| Ticket label | A ticket branch shows only the ticket key on the canvas; hovering shows the full name and the ticket summary as a tooltip. | `Layout/SceneBuilder.cs`, `Rendering/SceneCanvas.cs` |
| Property panel | A ticket branch shows its Jira key and an "Open in Jira" button (`<server>/browse/<KEY>`). | `Themes/PropertyPanel.xaml` |

## Zoom: fit to window

The "Fit" toolbar button (on by default) scales the diagram to fill the canvas area and follows window resizes and scene changes (25 % to 400 %). Zooming manually switches fit off.

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
| Later | Two-way Jira sync (status/summary snapshots), PR integration (D13). |
