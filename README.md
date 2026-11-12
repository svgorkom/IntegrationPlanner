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

## Layout

| Path | Contents |
|---|---|
| `src/IntegrationPlanner.Core` | Model and undoable editing commands |
| `src/IntegrationPlanner.Layout` | Pure diagram layout: scenario -> `DiagramScene` |
| `src/IntegrationPlanner.Persistence` | `*.iplan.json` load/save |
| `src/IntegrationPlanner.UI` | WPF viewer (rendering, zoom/pan, export), logging and composition root |
| `tests/` | xUnit test projects |
| `samples/scenario3.iplan.json` | Scenario 3 of the reference diagram |
