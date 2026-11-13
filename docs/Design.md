# IntegrationPlanner — Requirements & Design (v0.2)

Status: **Approved 2026-11-11 (v0.2)** Supersedes v0.1 (rule-based design).
Reference diagram: `docs/reference/branching-strategy-scenario3.png`.

## Decision log (2026-11-11, user)
| # | Decision |
|---|---|
| D1 | **No business rules.** The app is a planning and visualization tool; it never blocks an edit. |
| D2 | Structured model (branches, pulls, merges, markers); the app computes the layout. |
| D3 | Merges/pulls may connect **any** two branches (e.g. AD-xxxxxx → sibling AD-15777). |
| D4 | Cancelled branches keep all their lines; only the label style changes (red, strikethrough). |
| D5 | Fork line is **optional per branch** (screenshot hides most forks). |
| D6 | Markers anchor to a **point on a branch** or to a **pull/merge connector**. |
| D7 | Horizontal axis = **ordered steps only**; no dates. |
| D8 | Progress tracking: pulls, merges and markers are *Planned* or *Done*; drawn differently (dashed vs solid). |
| D9 | One plan file holds **several scenarios** sharing the same branch catalogue. |
| D10 | Editing via forms/property panel **and** drag on canvas (reorder steps and lanes). |
| D11 | Sharing: PNG + SVG export and copy-to-clipboard. |
| D12 | No audit log; undo/redo + git history of the plan file suffice. |
| D13 | Jira Data Center integration (PAT, one-way import of tickets into branches) lives in `IntegrationPlanner.Jira`; two-way sync and PR integration deferred. |
| D14 | UI English only; all strings in `Strings.resx`. |
| D15 | WPF on .NET 10. Old `.github`/`.claude` scaffolding removed. |
| D16 | (P3, user) Canvas drag: left-drag on an item moves it; left-drag on empty space pans. Lanes move vertically (`Order`); connectors and branch-anchored markers move horizontally (`Step`). Nothing else is draggable. |
| D17 | (P3, user) No help function; help pages dropped from scope. |
| D18 | (P3, user) New, Save and Save As; unsaved changes prompt (Yes (save) / No (discard) / Cancel) before New, Open and closing the app. |

## 1. Scope
| In scope (v1) | Out of scope | Revisit when |
|---|---|---|
| Plans with multiple scenarios, branch catalogue, pulls/merges/forks, markers | Validation / business rules (D1) | Never by default; maybe non-blocking *hints* if requested |
| Auto layout in screenshot style, zoom/pan | Free-form drawing | Requested |
| Form + canvas-drag editing, undo/redo | Multi-user editing | Plans shared daily by team |
| Planned/Done styling | Dates, calendar scaling (D7) | Dates drive decisions |
| Save/load `*.iplan.json`; PNG/SVG export; clipboard | Jira/PR sync (D13), triggering CI from markers | Next extension |
| | Audit log (D12) | Compliance need |

## 2. Domain model (data only, no rules)
**Plan**: `Title`, `SchemaVersion`, `Branches` (catalogue), `Scenarios`.

**Branch** (shared across scenarios): `Id`, `Name` (e.g. "Develop" or "AD-16384"), `Summary`, `IsMainline` (styling only), `IsCancelled`.

**Scenario**: `Id`, `Name` ("Scenario 3: Merge OrderPool reverse^2 to AD-15777"), `StepCount`, `Lanes`, `Connectors`, `Markers`.
- **Lane**: `BranchId`, `Order` (vertical position; negative = above mainline, positive = below), `StartStep`, `EndStep?`, `ShowFork`, `ForkFromBranchId?`, `ForkStep?`. A branch appears in a scenario only if it has a lane.
- **Connector**: `Id`, `Kind` (Pull | Merge), `FromBranchId`, `ToBranchId`, `Step`, `Label?`, `State` (Planned | Done).
- **Marker**: `Id`, `Kind` (ReleaseBuild | IntegrationTest | Testing | Custom), `Label`, `Anchor` (Branch+Step | Connector), `Direction` (FromTop | FromBottom | FromLeft | FromRight), `State`.

Data consistency only (not business rules): a reference to a deleted branch/connector is removed along with it (delete branch ⇒ its lanes, connectors, markers removed, undoable). Loading a file with dangling references drops them and reports what was dropped.

## 3. Architecture
| Project | Responsibility | Depends on |
|---|---|---|
| `IntegrationPlanner.Core` | Model + editing commands (undoable) | — |
| `IntegrationPlanner.Layout` | Pure: Scenario → `DiagramScene` (lines, labels, arrows, style tokens); deterministic | Core |
| `IntegrationPlanner.Persistence` | JSON load/save (System.Text.Json, camelCase, `schemaVersion`, atomic save) | Core |
| `IntegrationPlanner.UI` | WPF MVVM, scene renderer (DrawingVisual), PNG/SVG export, clipboard, DI root | all |

Simplified from v0.1: no Application/ports layer and no audit, because without rules or integrations it would be ceremony. Revisit when Jira sync arrives (it gets its own project).

## 4. Layout (screenshot style)
- Mainline at y=0; lanes stacked by `Order`. x = step × stepWidth.
- Lane line from `StartStep` to `EndStep` (or last step). Optional slanted fork line.
- Connector = slanted line from source lane at step to target lane at step+skew; label along it (rotated) or beside it.
- Branch label: name (large) + summary (small, wrapped) at lane start; cancelled ⇒ red strikethrough.
- Markers: thick arrow towards the anchor, label inside the shaft.
- Planned = dashed, Done = solid.
- Label collision pass; deterministic output.

## 5. UI
- Left: plan explorer (scenarios, branch catalogue). Centre: canvas (zoom/pan, select, drag steps/lanes, D16). Right: property panel for the selection.
- Toolbar: New/Open/Save/Save As, Add branch, Add pull, Add merge, Add marker, Delete, Undo/Redo, Export PNG/SVG, Copy.
- Strings in `Strings.resx`; styles/colours in `Themes/Theme.xaml` (teal `#1F5670`-like, red for cancelled).
- No help pages (D17).

## 6. Failure handling
| Case | Behaviour |
|---|---|
| Corrupt/newer-schema file | Not loaded, clear message, file untouched |
| Dangling references in file | Dropped and reported |
| Save/export fails | Original intact, temp removed, message shown |
| Unsaved changes on New, Open or close | Asked: Yes (save) / No (discard) / Cancel (D18); a failed or cancelled save keeps the plan open |
| Unhandled exception | Logged (Microsoft.Extensions.Logging, file sink), friendly dialog, no stack trace shown |

## 7. Tests
| Project | Focus |
|---|---|
| `Core.Tests` (xUnit) | Editing commands, undo/redo, cascade delete |
| `Layout.Tests` | Lane y-positions, connector geometry, label no-overlap, determinism, golden test for Scenario 3 sample |
| `Persistence.Tests` | Round-trip, atomic save, newer schema rejected, dangling refs dropped |
| Reqnroll specs (optional) | Business-language scenarios |

## 8. Phases
| Phase | Deliverable | Done when |
|---|---|---|
| P1 | Solution skeleton, Core model + commands + undo/redo, Persistence, `samples/scenario3.iplan.json` (screenshot), docs (`DomainModel.md`, `ApplicationFunctionality.md`, README), CI | Build with zero warnings; Core + Persistence tests pass |
| P2 | Layout + read-only WPF rendering of the sample; PNG/SVG/clipboard | Layout tests pass; visual match with screenshot |
| P3 | Editing UI (forms + drag), scenarios management, New/Save/Save As (help dropped, D17) | Tests pass; manual walkthrough |
| Later | Jira/PR sync | Separate design |

DoD per phase: `dotnet build IntegrationPlanner.slnx` zero warnings; `dotnet test` per affected test project passes; docs updated.
