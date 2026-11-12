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
```

Or all at once: `dotnet test --solution IntegrationPlanner.slnx`.

## Run

```powershell
dotnet run --project src\IntegrationPlanner.UI\IntegrationPlanner.UI.csproj
```

## Layout

| Path | Contents |
|---|---|
| `src/IntegrationPlanner.Core` | Model and undoable editing commands |
| `src/IntegrationPlanner.Layout` | Diagram layout (P2; skeleton only) |
| `src/IntegrationPlanner.Persistence` | `*.iplan.json` load/save |
| `src/IntegrationPlanner.UI` | WPF application and composition root |
| `tests/` | xUnit test projects |
| `samples/scenario3.iplan.json` | Scenario 3 of the reference diagram |
