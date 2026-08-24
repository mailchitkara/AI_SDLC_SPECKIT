---

description: "Task list for Hand-Edited Generated File Detection"
---

# Tasks: Hand-Edited Generated File Detection

**Input**: Design documents from `specs/009-generated-file-contamination/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Included.

**Organization**: Single user story — no Foundational phase needed.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Run `dotnet build backend/AgentGuard.sln` and `dotnet test backend/AgentGuard.sln` to confirm a clean baseline (90/90 tests, per `008-swallowed-exception-detection`) before starting.

---

## Phase 2: User Story 1 - Catch a Hand-Edited Generated File (Priority: P1) 🎯 MVP

**Goal**: A PR that modifies the content of a file recognized as generated (by extension or content marker) produces a finding; a newly-added generated file, an unchanged generated file, or an ordinary source file does not.

**Independent Test**: `quickstart.md` Scenarios 1–6.

### Tests for User Story 1

- [X] T002 [US1] Write `backend/AgentGuard.Core.Tests/Rules/GeneratedFileModifiedRuleTests.cs` (new file): a case per signal (extension, content marker) confirming each fires on a modified file with changed content; a case confirming a newly-added generated file produces no finding; a case confirming an unchanged generated file produces no finding; a case confirming an ordinary source file produces no finding; a case confirming a file matching both signals produces two findings; a case confirming a file with no content produces no finding without erroring.

### Implementation for User Story 1

- [X] T003 [US1] Define `GeneratedFileSignals` (extension pattern + content-marker pattern) in `backend/AgentGuard.Core/Rules/GeneratedFileSignals.cs`, per `data-model.md`.
- [X] T004 [US1] Add the `GeneratedFileModified` entry (`RuleId: "GENERATED_FILE_MODIFIED"`, `Severity.Medium`, `RiskDimension.ChangeManagement`) to `backend/AgentGuard.Core/Rules/RuleCatalog.cs`, appended after `SwallowedException`. Depends on T003.
- [X] T005 [US1] Implement `GeneratedFileModifiedRule.Evaluate` in `backend/AgentGuard.Core/Rules/GeneratedFileModifiedRule.cs`, per `data-model.md`'s evaluation logic (Modified-only, content-actually-changed, independent extension/marker checks). Depends on T003, T004.
- [X] T006 [US1] Wire the new rule into `backend/AgentGuard.Core/AgentGuardAnalyzer.cs`'s fixed rule pipeline, after `SwallowedException`. Depends on T005.
- [X] T007 [US1] Update `backend/AgentGuard.Api.Tests/PrRiskAnalysisEndpointTests.cs`'s check-count assertion 8→9. Depends on T006.
- [X] T008 [US1] Before committing: scan the full staged diff for both signal patterns; verify the three genuinely-*Modified* existing files this PR touches (`RuleCatalog.cs`, `AgentGuardAnalyzer.cs`, the endpoint test file) don't contain a literal match (research.md §5). The newly-added spec/doc/source/test files are structurally exempt (research.md §3) but are scanned anyway as a sanity check.
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

## Post-Implementation Note

T009's live validation surfaced a real, previously-latent bug unrelated to this rule's own logic:
`FindingOrdering.Stable`'s `.ThenBy(f => f.RuleId)` relied on `Comparer<RuleId>.Default`, which
throws at runtime because `RuleId` (a record struct) doesn't implement `IComparable`. This had
never been exercised because no two rules sharing the same severity had ever both fired in one
request until `GENERATED_FILE_MODIFIED`'s Medium severity collided with `MissingRelatedTests`'s.
Fixed in `backend/AgentGuard.Core/Findings/FindingOrdering.cs` (key on `RuleId.Value` instead),
with a new regression test suite (`FindingOrderingTests.cs`) added outside this feature's own
`Rules/` test directory since it covers pre-existing shared infrastructure, not this rule.
