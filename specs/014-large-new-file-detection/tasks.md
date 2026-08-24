---

description: "Task list for Large New File Detection"
---

# Tasks: Large New File Detection

**Input**: Design documents from `specs/014-large-new-file-detection/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Included, mirroring `LargeChangeSizeRuleTests.cs`'s structure.

**Organization**: Single user story — no Foundational phase needed.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Run `dotnet build backend/AgentGuard.sln` and `dotnet test backend/AgentGuard.sln` to confirm a clean baseline (138/138 tests, per `013-business-critical-path-detection`) before starting.

---

## Phase 2: User Story 1 - Flag a Substantial Brand-New File (Priority: P1) 🎯 MVP

**Goal**: A PR that adds a new file at or above the line-count threshold produces a finding; a small new file, or any change to an existing file, does not.

**Independent Test**: `quickstart.md` Scenarios 1–5.

### Tests for User Story 1

- [X] T002 [US1] Write `backend/AgentGuard.Core.Tests/Rules/LargeNewFileRuleTests.cs` (new file, mirroring `LargeChangeSizeRuleTests.cs`): a case confirming a new file at/above the threshold fires; a case confirming a new file below the threshold does not; a case confirming a large *modified* (not new) file does not fire; a case confirming a large *deleted* file does not fire; a case confirming two qualifying new files produce two independent findings.

### Implementation for User Story 1

- [X] T003 [US1] Add the `LargeNewFile` entry (`RuleId: "LARGE_NEW_FILE_INTRODUCED"`, `Severity.Medium`, `RiskDimension.ChangeManagement`) to `backend/AgentGuard.Core/Rules/RuleCatalog.cs`, appended after `BusinessCriticalPath`.
- [X] T004 [US1] Implement `LargeNewFileRule.Evaluate` in `backend/AgentGuard.Core/Rules/LargeNewFileRule.cs`, per `data-model.md`'s evaluation logic (a fixed 200-line threshold, `ChangeType.Added` only). Depends on T003.
- [X] T005 [US1] Wire the new rule into `backend/AgentGuard.Core/AgentGuardAnalyzer.cs`'s fixed rule pipeline, after `BusinessCriticalPath`. Depends on T004.
- [X] T006 [US1] Update `backend/AgentGuard.Api.Tests/PrRiskAnalysisEndpointTests.cs`'s check-count assertion 13→14. Depends on T005.
- [X] T007 [US1] Run `quickstart.md` Scenarios 1–5 against a locally running `dotnet run` instance and confirm. Depends on T005, T006.

**Checkpoint**: User Story 1 (the whole feature) is functional and testable.

---

## Phase 3: Polish & Cross-Cutting Concerns

- [X] T008 Run the full backend suite (`dotnet test backend/AgentGuard.sln`) and confirm everything passes.

---

## Dependencies & Execution Order

- Setup → Tests (T002) → Implementation (T003 → T004 → T005 → T006) → Live validation (T007) → Polish (T008).

## Implementation Strategy

Single story, single PR — this feature *is* the MVP.
