# IntegrationPlanner — Design (v0.1, awaiting approval)

Status: **DESIGN ONLY. Nothing implemented. Waiting for approval.**
Decision log: 2026-11-11 — Jira/PR sync **deferred** to a later extension (user decision). v1 = planning + diagram + manual status.
Target folder: `C:\Git\IntegrationPlanner` (exists; contains `.github`/`.claude` scaffolding copied from Urenkompas; not a git repo).

---

## 1. Problem understanding

### 1.1 Restatement
Engineers need a desktop tool (WPF/C#) to **plan how feature branches integrate** into each other and into a mainline (e.g. `develop`), and to render that plan as a **branching-strategy diagram** like the reference screenshot:

- Horizontal lanes per branch (`Develop`, `AD-15777`, sub-branches above/below).
- Diagonal connectors for **fork** (branch off), **sync** ("Pull develop") and **merge** ("Merge to develop").
- Each branch labelled with ticket key + summary; cancelled tickets shown red + strikethrough.
- **Action markers** at points in time on a lane (big arrows: "Release build", "Lots of testing!", "Start integration test").
- Title + scenario subtitle.

Additionally, the plan should **auto-update from Jira** (ticket closed, PR merged).

### 1.2 Goals
| # | Goal |
|---|------|
| G1 | Model a plan: branches, fork/sync/merge events, action markers, ordered in time |
| G2 | Render the plan in the screenshot's visual style, deterministically |
| G3 | Edit the plan interactively (add/move/remove items) |
| G4 | Save/load plans as files; export diagram as PNG (and SVG) |
| G5 | ~~Jira sync~~ **Deferred** (see §2.4). v1: user sets branch status / realizes events manually |
| G6 | Distinguish **planned** vs **realized** (dashed vs solid) so progress is visible |

### 1.3 Scope / Non-goals
| In scope (v1) | Out of scope (non-goal) | Why deferred / revisit trigger |
|---|---|---|
| Single-user desktop app, local plan files | Multi-user/real-time collaboration, server backend | Large complexity; revisit if plans are shared daily by a team |
| Logical timeline (ordered steps), optional planned date per event | Gantt-style calendar scaling | Screenshot is ordinal, not dated; revisit when dates drive decisions |
| Ticket key stored per branch (plain text) | **Any Jira/PR integration** (read or write) | User decision 2026-11-11; revisit as next extension after v1. Domain already holds `TicketKey` and `RealizedBy`, so no model change needed |
| Action markers as **annotations** with status | Actually **triggering** pipelines (Bamboo/Jenkins "start integration test") | Needs CI credentials + side effects; revisit as a separate extension point (`IActionExecutor`) |
| — | Deriving the plan from real git history | Plan is forward-looking; revisit for "import existing branches" |
| PNG/SVG export | Visio / PowerPoint / draw.io export | Revisit if requested; renderer is format-agnostic |
| Undo/redo of edits | — | In scope (cheap with command pattern, needed for usable editing) |

### 1.4 Assumptions (confidence < 90% marked)
| # | Assumption | Confidence |
|---|---|---|
| A1 | .NET 10 / WPF (SDK 10.0.403 installed; existing `ci.yml` targets .NET 10) | High |
| A5 | ~~Adapt scaffolding~~ — resolved by Q4 (delete) | — |

### 1.5 Open questions (will be asked one at a time)
- ~~Q1–Q3~~ Jira/PR questions: moot, integration deferred (2026-11-11).
- ~~Q4~~ Decided 2026-11-11: **delete** existing `.github`/`.claude` scaffolding and start clean (includes `copilot-instructions.md`; its design-first rules then no longer apply to future sessions unless re-added).
- ~~Q5~~ Decided 2026-11-11: **English only** in v1; all strings still in `Strings.resx` so a language is added by dropping in a satellite `.resx`.

---

## 2. Solution design

### 2.1 Architecture (Clean Architecture)

```
IntegrationPlanner.UI  (WPF, MVVM, DI composition root)
        │
        ├──> IntegrationPlanner.Application  (use cases, commands, ports)
        │            │
        │            └──> IntegrationPlanner.Domain  (entities, invariants; no deps)
        │
        ├──> IntegrationPlanner.Layout  (pure C#: Plan -> DiagramScene geometry)
        │            └──> Domain
        │
        └──> IntegrationPlanner.Infrastructure (JSON files, Jira HTTP, DPAPI, logging, audit)
                     └──> Application (implements ports), Domain
```

| Project | Responsibility | May depend on | Must NOT depend on |
|---|---|---|---|
| Domain | Plan aggregate, invariants | BCL only | Everything else |
| Application | Use cases (AddBranch, ScheduleMerge, RealizeEvent…), ports `IPlanRepository`, `IAuditTrail`, `IClock` | Domain | WPF, HTTP, file system |
| Layout | Deterministic layout → `DiagramScene` (lines, labels, arrows, styles as semantic tokens) | Domain | WPF (`System.Windows`) |
| Infrastructure | Adapters for ports (JSON files, audit, logging) | Application, Domain | UI |
| UI | Views, ViewModels, `SceneRenderer` (scene → WPF `DrawingVisual`), PNG/SVG exporters | All above (composition root only wires Infrastructure) | — ViewModels never reference `System.Windows.Controls` |

Key decision: **layout is separated from drawing**. Layout produces a pure geometry model that is unit-testable (positions, overlaps); the WPF renderer is a thin translation. The same scene feeds PNG and SVG export.

### 2.2 Domain model (proposed `DomainModel.md` content)

**IntegrationPlan** (aggregate root)
- `Id`, `Title` ("Branching strategy."), `Scenario` ("Scenario 3: …"), `SchemaVersion`
- `Branches`, `Events`, `Actions`, `TimelineSlots` (ordered steps; optional planned date each)

**Branch**
- `Id`, `Name`, `Kind` (Mainline | Feature | SubFeature), `ParentBranchId?`
- `TicketKey?` (e.g. `AD-16384`), `Summary`, `Lane` (display order relative to parent: above/below)
- `Status`: Planned | Active | Integrated | Cancelled (Cancelled ⇒ red strikethrough)

**IntegrationEvent** (the diagonal lines)
- `Id`, `Type`: Fork | Sync (pull parent into child) | Merge (child into target)
- `SourceBranchId`, `TargetBranchId`, `Slot`, `Label?` ("Pull develop before OrderPool split reverse")
- `State`: Planned | Realized (realized by user or by Jira/PR sync), `RealizedBy?` (`ExternalRef` e.g. PR id)

**ActionMarker** (the big arrows)
- `Id`, `Kind` (ReleaseBuild | IntegrationTest | Testing | Freeze | Custom), `Label`, `BranchId`, `Slot`, `Direction` (FromAbove | FromRight …), `State` (Planned | Done)

**Invariants & business rules**
| # | Rule | Enforced | Tested |
|---|---|---|---|
| I1 | Exactly one Mainline; it has no parent | Plan methods reject | Unit |
| I2 | Non-mainline branch has exactly one Fork event from its parent, and it is its earliest event | `AddBranch` creates the Fork atomically | Unit |
| I3 | No parent cycles | `ChangeParent` validates | Unit |
| I4 | Sync/Merge slot > Fork slot of the source branch | Event creation validates | Unit (Scenario Outline) |
| I5 | No events on a branch after its final Merge into its parent (branch closed) | Validation | Unit |
| I6 | Sync source must be an ancestor of the target | Validation | Unit |
| I7 | Cancelled branch cannot be merged; its planned events are rendered but flagged | Validation + layout style | Unit |
| I8 | A Realized event cannot precede (in slot order) a Planned prerequisite Fork | Validation | Unit |
| I9 | TicketKey unique per plan (one branch per ticket) | Validation | Unit |
| I10 | (future) Jira sync never deletes user data; it only changes `Status`/`State` fields | Sync use case design | Integration |

Rule violations return a `Result` with a machine-readable error code (localized at the UI), not exceptions.

### 2.3 Layout algorithm (screenshot style)
1. Mainline lane at y = 0. Children assigned lanes by `Lane` hint; siblings stacked outward; grandchildren nest further out (`AD-16221` below `AD-xxxxxx`).
2. x = `slot × slotWidth`; branch line spans fork-x … merge-x (or to the end).
3. Event connectors: diagonal from (x_source, y_source) to (x + skew, y_target); skew constant gives the slanted look.
4. Labels: ticket key (large) + wrapped summary (small) placed at the branch start; collision pass shifts labels vertically.
5. Action markers: thick arrow glyph pointing at (x, lane-y), label rotated along the shaft.
6. Output: `DiagramScene` with style *tokens* (`MainlineStroke`, `CancelledText`, `PlannedDash`) resolved to brushes from the theme resource dictionary in the UI.

Deterministic: same plan ⇒ identical scene (no randomness, stable ordering by Id for ties).

### 2.4 Jira / PR synchronization — DEFERRED (not in v1)
Kept below as the intended direction for the follow-up extension; **none of this is built in v1**. v1 provides: `TicketKey` on branches, manual `Status`/`State` changes (audited), and `RealizedBy` free-text reference.

- **Polling** `JiraSyncService` (interval configurable, default 5 min, manual "Sync now").
- Fetch only ticket keys present in the plan via JQL `key in (…)` (batched ≤ 50).
- **Mapping rules** (default, configurable per plan if Q3 = yes):
  - Ticket status category *Done* ⇒ branch `Integrated` (if its Merge event is realized) else flagged "ticket done, merge not seen".
  - Ticket resolution *Won't Do / Cancelled* ⇒ branch `Cancelled`.
  - PR from branch → target merged ⇒ matching Merge event `Realized`, `RealizedBy = PR id`.
  - First commit/PR on branch ⇒ branch `Active`.
- PR source: Jira dev-status info or direct Bitbucket/GitHub API behind `IPullRequestSource` (decided by Q2). Note: Jira's dev-status endpoint is not an officially documented public API — I have not verified its contract; direct VCS API is the safer option.
- Sync produces a **change set preview**; user can enable auto-apply. Every applied change is audited.
- Idempotent: applying the same remote state twice yields no further changes (compare-before-write).

### 2.5 Persistence
- One plan = one `*.iplan.json` file, `System.Text.Json`, camelCase, `schemaVersion` field + migration step on load.
- Atomic save (write temp, then replace). No user path concatenation; paths only from file dialogs, normalized with `Path.GetFullPath`.
- App settings (Jira URL, poll interval, language) in `%LocalAppData%\IntegrationPlanner\settings.json`.

### 2.6 Alternatives considered
| Alternative | Rejected because |
|---|---|
| Mermaid `gitGraph` rendered in WebView2 | Cannot draw action arrows, cancelled styling, sub-branch lane control or interactive editing; adds browser runtime |
| Graphviz / MSAGL auto-layout | General graph layout produces node-link diagrams, not lane/timeline style; native dependency |
| Jira webhooks | Desktop app has no public endpoint; would require a server component (non-goal) |
| Infer plan from real git repo | Plans are forward-looking; actual history is only used to *realize* planned events |
| Drawing directly in ViewModel/code-behind | Untestable, violates MVVM rules; layout engine keeps it testable |
| SQLite storage | Plans are small documents, file-per-plan is shareable via git/email and simpler |

---

## 3. UI & multilanguage
- Main window: plan explorer / diagram canvas (zoom, pan) / property panel for selected item.
- Editing via toolbar + context menu: Add branch, Add sync, Add merge, Add action marker, Set status. Drag along timeline to change slot.
- All UI text in `Resources\Strings.resx` (English only in v1, per Q5); new language = add a `.resx` satellite, no code change. Language switch at runtime via a localization markup extension.
- `FlowDirection` derived from current culture (RTL supported).
- Dates/numbers formatted with current culture; plurals via separate resource keys per form.
- **Diagram content (ticket summaries, labels) is user data and is not translated.** Action-marker *default* labels come from resources.
- Theme: new `Themes\Theme.xaml` (colors from screenshot: dark teal `#1F5670`-ish, red for cancelled — exact values TBD) — all styles in resource dictionaries, none inline.
- Accessibility: keyboard navigation of diagram items, automation names, high-contrast theme compatible tokens.

## 4. Audit trail
- `IAuditTrail.Record(actionKey, entityId, actor, details)` written as append-only JSONL in `%LocalAppData%\IntegrationPlanner\audit\`.
- Keys are locale-independent: `Plan.Create`, `Plan.Save`, `Branch.Add`, `Branch.StatusChange`, `Event.Add`, `Event.Realize`, `Action.Add`, `Action.StatusChange`, `Settings.Change`.
- Actor: Windows user name (future sync would use `System.JiraSync`).
- All mutations go through Application use cases, which always audit → UI cannot bypass it.
- Details: field names / old→new status only; never tokens.

## 5. Failure modes
| Scenario | Behaviour |
|---|---|
| Invalid edit (violates I1–I9) | Rejected with localized message; plan unchanged |
| Corrupt / newer-schema plan file | Load fails with clear message; file untouched |
| Save fails (disk/locked) | Temp file discarded, original intact, error shown |
| Export fails (path/permissions) | Error shown, no partial file left |

## 6. Performance
Plans are small (≈10–200 branches). Layout O(n log n), redraw on change only. Scalability beyond ~500 branches **considered but deferred** (virtualized rendering would be the path).

## 7. Observability
`Microsoft.Extensions.Logging` + file sink (rolling, `%LocalAppData%\IntegrationPlanner\logs`). Logs load/save/export outcomes and durations, validation rejections by error code — never plan contents. Kept separate from audit trail.

## 8. Security
- v1 handles no credentials. (Future Jira: token via DPAPI, never in plan/settings/logs.)
- Strongly-typed deserialization only; no `BinaryFormatter`.
- Plan files contain ticket keys/summaries (possibly internal info) — user responsibility to share; noted in help.

## 9. Configuration vs code
| Configurable | Fixed in code |
|---|---|
| Language, slot width/colours via theme | Invariants I1–I10, audit keys, file format, layout algorithm |

## 10. Extensibility
- `IDiagramExporter` (PNG, SVG; Visio later). Next extension: Jira/PR sync as new Application use case + `IIssueTracker` adapter — added when built, not speculatively now. Future `IActionExecutor` (trigger CI).
- Intentionally hard: changing invariants (they define the meaning of a plan).

## 11. Complexity
Main source: layout/label collision. Mitigated by keeping both pure and unit-tested. No plugin system, no server, no database.

## 12. Test strategy
| Level | Project | Focus |
|---|---|---|
| Unit (xUnit) | `Domain.Tests` | Invariants I1–I9, status transitions |
| Unit | `Layout.Tests` | Lane assignment, x-positions, no label overlap, determinism; golden "screenshot scenario" scene |
| Unit | `Application.Tests` | Use cases + audit always recorded; undo/redo |
| Integration | `Infrastructure.Tests` | JSON round-trip + schema migration, atomic save |
| BDD (Reqnroll) | `Specs` | Business scenarios in plain language, e.g. "A cancelled ticket is shown as cancelled in the plan" |
| UI smoke | deferred | Revisit when UI stabilises (mirrors existing ci.yml approach) |

## 13. Documentation / help artifacts
- `ApplicationFunctionality.md` (new), `DomainModel.md` (new), `README.md`.
- **Help**: no help framework exists yet (new app). Proposal: `Help\<culture>\*.md` pages rendered in a Help window, one page per feature (Plans, Branches & events, Action markers, Export). This defines the framework future features must follow.

## 14. Change impact
New solution. Existing `.github`/`.claude` scaffolding is deleted (Q4); a fresh minimal `ci.yml` (build + test `IntegrationPlanner.slnx`) is added in P1.

---

## 15. Delivery phases
| Phase | Deliverable | Done when |
|---|---|---|
| P1 | Solution skeleton, Domain + invariants, JSON persistence, docs | `dotnet build` green; Domain/Infra tests pass |
| P2 | Layout engine + WPF renderer; screenshot scenario reproduced from a sample plan file; PNG/SVG export | Layout tests pass; visual check against screenshot |
| P3 | Editing UI (add/move/delete, undo/redo), action markers, localization, help | App tests pass; manual walkthrough |
| P4 | Planned-vs-realized styling, manual realize/status changes | Tests pass |
| Later | Jira / PR sync extension (separate design, §2.4) | — |

## 16. Definition of Done (per phase)
- `dotnet build IntegrationPlanner.slnx` succeeds with zero warnings-as-errors.
- `dotnet test` for each affected test project passes.
- `DomainModel.md`, `ApplicationFunctionality.md`, Help pages updated for that phase.
- Every state change audited; no hard-coded UI strings; no plan contents in logs.

## 17. Self-review
| Mandatory section | Covered |
|---|---|
| Scope & non-goals | §1.3 |
| Alternatives | §2.6 |
| Audit | §4 |
| Functional description / Domain model / Help | §13, §2.2 |
| Tests | §12 |
| Failure modes | §5 |
| Performance | §6 |
| Backward compat | New app; plan file `schemaVersion` + migration from day one (§2.5) |
| Observability | §7 |
| Boundaries | §2.1 |
| Extensibility / Complexity / Readability | §10, §11, ubiquitous naming in §2.2 |
| Determinism / idempotency | §2.3, §2.4 |
| Config vs code | §9 |
| Security | §8 |
| Multilanguage / theming | §3 |

**Remaining gaps:** none. All open questions resolved (Jira deferred, scaffolding deleted, English only). Approval should not be final until answered.

