---

description: "Task list for Newly Disabled Test Detection"
---

# Tasks: Newly Disabled Test Detection

**Input**: Design documents from `specs/007-disabled-test-detection/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Included, mirroring `OverlyPermissiveAccessRuleTests.cs`'s structure.

**Organization**: Single user story (this feature is one focused rule addition) — no Foundational phase needed beyond the pattern-definitions file itself, which is small enough to fold into the story.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Run `dotnet build backend/AgentGuard.sln` and `dotnet test backend/AgentGuard.sln` to confirm a clean baseline (69/69 tests, per `006-security-risk-rules`) before starting.

---

## Phase 2: User Story 1 - Catch a Newly-Disabled Test (Priority: P1) 🎯 MVP

**Goal**: A PR that newly introduces an xUnit skip parameter, a Jest/Mocha skip modifier or skip-prefixed function, a pytest skip decorator, or a Go early-skip call produces a finding; a PR that only touches a file containing a pre-existing, unchanged skip marker — or that removes one — does not.

**Independent Test**: `quickstart.md` Scenarios 1–7.

### Tests for User Story 1

- [X] T002 [US1] Write `backend/AgentGuard.Core.Tests/Rules/DisabledTestRuleTests.cs` (new file, mirroring `OverlyPermissiveAccessRuleTests.cs`): one case per pattern in `data-model.md`'s table (5 cases) confirming each fires on newly-added content with correct evidence/remediation; a case confirming a pre-existing, unchanged occurrence produces no finding (count unchanged); a case confirming a genuinely new *second* occurrence of an already-present pattern is still flagged (count-based diff, not value-based — research.md §2); a case confirming a *removed* skip marker (test re-enabled) produces no finding; a case confirming a file with no content (binary/unretrievable) produces no finding without erroring. Per research.md §5, every test fixture string must be built via compile-time-constant string concatenation (or otherwise avoid a contiguous literal match) so this file doesn't trip the rule it's testing when this PR is analyzed.

### Implementation for User Story 1

- [X] T003 [US1] Define `DisabledTestPatterns` (5 entries: name, regex, remediation hint) in `backend/AgentGuard.Core/Rules/DisabledTestPatterns.cs`, per `data-model.md`'s table. Per research.md §5, write remediation hints so they don't literally match their own pattern (mirroring the fix already applied to `PermissivePatterns.cs`'s `AllowAnonymous` remediation text in `006`).
- [X] T004 [US1] Add the `DisabledTest` entry (`RuleId: "DISABLED_TEST_INTRODUCED"`, `Severity.High`, `RiskDimension.Testing`) to `backend/AgentGuard.Core/Rules/RuleCatalog.cs`, appended after `OverlyPermissiveAccess` to preserve the existing six rules' relative order. Depends on T003.
- [X] T005 [US1] Implement `DisabledTestRule.Evaluate` in `backend/AgentGuard.Core/Rules/DisabledTestRule.cs`: for each changed file, for each pattern, count matches in `OldContent` vs `NewContent`; emit one finding per pattern whose count increased, per `data-model.md`'s evaluation logic. Depends on T003, T004.
- [X] T006 [US1] Wire the new rule into `backend/AgentGuard.Core/AgentGuardAnalyzer.cs`'s fixed rule pipeline (add to the `findingsByRule` array alongside the existing six, after `OverlyPermissiveAccess`). Depends on T005.
- [X] T007 [US1] Update `backend/AgentGuard.Api.Tests/PrRiskAnalysisEndpointTests.cs`'s `body.Checks.Should().HaveCount(6)` assertion to `7` — expected, correct maintenance (a 7th rule now exists), not a regression of `006`'s guarantee, which only covers the first six rules' own behavior. Depends on T006.
- [X] T008 [US1] Before committing: scan the full diff (`git diff --cached main`, after `git add -A`) against all 5 new patterns to confirm no new file (including this task list, spec.md, research.md, data-model.md, quickstart.md, and the test file) contains a literal contiguous match — per research.md §5, this is now a standing pre-push check, not an afterthought. Fix any match found the same way `006` did (prose rewording for docs, compile-time-constant concatenation for test fixtures).
- [X] T009 [US1] Run `quickstart.md` Scenarios 1–7 against a locally running `dotnet run` instance and confirm. Depends on T006, T007.

**Checkpoint**: User Story 1 (the whole feature) is functional and testable.

---

## Phase 3: Polish & Cross-Cutting Concerns

- [X] T010 Run the full backend suite (`dotnet test backend/AgentGuard.sln`) and confirm everything passes, including all new tests.

---

## Dependencies & Execution Order

- Setup → Tests (T002) → Implementation (T003 → T004 → T005 → T006 → T007) → Self-trip check (T008) → Live validation (T009) → Polish (T010).
- T002 (tests) is written before T003–T006 (implementation) per TDD, but will not compile until T003–T005 exist — write it first per the template's convention, expect it red until the implementation tasks land.

## Implementation Strategy

Single story, single PR — this feature *is* the MVP. No incremental multi-story delivery needed; validate end-to-end (T008–T010) before shipping.
