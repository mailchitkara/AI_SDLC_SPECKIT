---

description: "Task list for Newly Introduced TODO/Stub Detection"
---

# Tasks: Newly Introduced TODO/Stub Detection

**Input**: Design documents from `specs/010-todo-stub-detection/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Included.

**Organization**: Single user story — no Foundational phase needed.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Run `dotnet build backend/AgentGuard.sln` and `dotnet test backend/AgentGuard.sln` to confirm a clean baseline (99/99 tests, per `009-generated-file-contamination`) before starting.

---

## Phase 2: User Story 1 - Catch Newly-Introduced Incomplete Work (Priority: P1) 🎯 MVP

**Goal**: A PR that newly introduces a TODO/FIXME/HACK marker, a C# not-implemented stub, or a Python not-implemented stub produces a finding; a PR that only touches a file with a pre-existing, unchanged occurrence — or that only contains an unrelated word like "Hackathon" — does not.

**Independent Test**: `quickstart.md` Scenarios 1–6.

### Tests for User Story 1

- [X] T002 [US1] Write `backend/AgentGuard.Core.Tests/Rules/TodoStubRuleTests.cs` (new file, mirroring `SwallowedExceptionRuleTests.cs`): one case per pattern (3 cases) confirming each fires on newly-added content; a case confirming a word-boundary false positive (e.g. "Hackathon") does not fire; a case confirming a pre-existing, unchanged occurrence produces no finding; a case confirming a genuinely new second occurrence is still flagged (count-based); a case confirming a removed occurrence produces no finding; a case confirming a file with no content produces no finding without erroring. All fixtures via compile-time-constant string concatenation (research.md §6).

### Implementation for User Story 1

- [X] T003 [US1] Define `TodoStubPatterns` (3 entries) in `backend/AgentGuard.Core/Rules/TodoStubPatterns.cs`, per `data-model.md`'s table.
- [X] T004 [US1] Add the `TodoStub` entry (`RuleId: "TODO_STUB_INTRODUCED"`, `Severity.Medium`, `RiskDimension.ChangeManagement`) to `backend/AgentGuard.Core/Rules/RuleCatalog.cs`, appended after `GeneratedFileModified`. Depends on T003.
- [X] T005 [US1] Implement `TodoStubRule.Evaluate` in `backend/AgentGuard.Core/Rules/TodoStubRule.cs`, mirroring `DisabledTestRule.Evaluate`'s shape exactly. Depends on T003, T004.
- [X] T006 [US1] Wire the new rule into `backend/AgentGuard.Core/AgentGuardAnalyzer.cs`'s fixed rule pipeline, after `GeneratedFileModified`. Depends on T005.
- [X] T007 [US1] Update `backend/AgentGuard.Api.Tests/PrRiskAnalysisEndpointTests.cs`'s check-count assertion 9→10. Depends on T006.
- [X] T008 [US1] Before committing: scan the full staged diff (`git add -A && git diff --cached main`) against all 3 new patterns to confirm no new file contains a literal contiguous match (research.md §6). Fix any match found.
- [X] T009 [US1] Run `quickstart.md` Scenarios 1–6 against a locally running `dotnet run` instance and confirm. Depends on T006, T007.

**Checkpoint**: User Story 1 (the whole feature) is functional and testable.

---

## Phase 3: Polish & Cross-Cutting Concerns

- [X] T010 Run the full backend suite (`dotnet test backend/AgentGuard.sln`) and confirm everything passes.

---

## Dependencies & Execution Order

- Setup → Tests (T002) → Implementation (T003 → T004 → T005 → T006 → T007) → Self-trip check (T008) → Live validation (T009) → Polish (T010).

## Implementation Strategy

Single story, single PR — this feature *is* the MVP.
