# IntegrationPlanner

WPF tool to plan and visualise branch integration (pulls, merges, release builds) as diagrams. See `docs/Design.md` (design), `docs/DomainModel.md` and `docs/ApplicationFunctionality.md`.

## Prerequisites

- Windows (the UI is WPF).
- .NET SDK 10.0.403 or a later 10.0 feature band (pinned in `global.json`).

## Build

```powershell
dotnet build IntegrationPlanner.slnx
```

Warnings are treated as errors (`Directory.Build.props`). Package versions are pinned centrally in `Directory.Packages.props`.

## Test

Tests use xUnit v3 on Microsoft.Testing.Platform (enabled in `global.json`).

```powershell
dotnet test --project tests\IntegrationPlanner.Core.Tests\IntegrationPlanner.Core.Tests.csproj
dotnet test --project tests\IntegrationPlanner.Persistence.Tests\IntegrationPlanner.Persistence.Tests.csproj
dotnet test --project tests\IntegrationPlanner.Layout.Tests\IntegrationPlanner.Layout.Tests.csproj
dotnet test --project tests\IntegrationPlanner.UI.Tests\IntegrationPlanner.UI.Tests.csproj
```

Or all at once: `dotnet test --solution IntegrationPlanner.slnx`.

The Layout golden test compares the scene of `samples/scenario3.iplan.json` with `tests/IntegrationPlanner.Layout.Tests/Golden/scenario3.scene.txt`. After an intentional layout change, regenerate it with `$env:IPLAN_UPDATE_GOLDEN='1'`, run the Layout tests, remove the variable and review the diff.

## Run

```powershell
dotnet run --project src\IntegrationPlanner.UI\IntegrationPlanner.UI.csproj
dotnet run --project src\IntegrationPlanner.UI\IntegrationPlanner.UI.csproj -- samples\scenario3.iplan.json
```

The optional argument is a plan file to open at start-up. Logs go to `%LocalAppData%\IntegrationPlanner\logs` (daily files, 7 kept).

## Editing

| Action | How |
|---|---|
| New / Open / Save / Save As | Toolbar, or Ctrl+N / Ctrl+O / Ctrl+S / Ctrl+Shift+S. Unsaved changes are marked `*` in the title and asked about before New, Open and closing. |
| Scenarios, branch catalogue | Explorer on the left: add, duplicate, delete scenarios; add and select branches. |
| Add items | Toolbar: Add branch, Add pull, Add merge, Add marker. |
| Change items | Select on the canvas or in the explorer, edit in the property panel on the right (commits on leaving the field or Enter). |
| Drag | Drag a lane up/down to reorder; drag a pull, merge or branch-anchored marker left/right to change its step. Drag empty space to pan. |
| Delete, undo, redo | Del, Ctrl+Z, Ctrl+Y (or the toolbar). |
| Jira | Menu "Jira connection..." (Jira Data Center, Personal Access Token; the dialog explains each field), then "Add branch from Jira ticket..." to create a branch from a ticket. Import only. |
| Fit | The "Fit" toolbar button scales the diagram to the window. |

Details: `docs/ApplicationFunctionality.md`.

## Layout

| Path | Contents |
|---|---|
| `src/IntegrationPlanner.Core` | Model and undoable editing commands |
| `src/IntegrationPlanner.Layout` | Pure diagram layout: scenario -> `DiagramScene` |
| `src/IntegrationPlanner.Persistence` | `*.iplan.json` load/save |
| `src/IntegrationPlanner.Jira` | Jira Data Center client, settings and branch naming |
| `src/IntegrationPlanner.UI` | WPF editor (rendering, editing, zoom/pan, export), logging and composition root |
| `tests/` | xUnit test projects |
| `samples/scenario3.iplan.json` | Scenario 3 of the reference diagram |
