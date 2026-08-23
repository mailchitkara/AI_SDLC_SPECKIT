---

description: "Task list for Overly Permissive Access Control Detection"
---

# Tasks: Overly Permissive Access Control Detection

**Input**: Design documents from `specs/006-security-risk-rules/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Included, mirroring `SecretDetectedRuleTests.cs`'s structure.

**Organization**: Single user story (this feature is one focused rule addition) — no Foundational phase needed beyond the pattern-definitions file itself, which is small enough to fold into the story.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Run `dotnet build backend/AgentGuard.sln` and `dotnet test backend/AgentGuard.sln` to confirm a clean baseline (59/59 tests, per `005-risk-engine-foundation`) before starting.

---

## Phase 2: User Story 1 - Catch a Newly-Loosened Access Control Change (Priority: P1) 🎯 MVP

**Goal**: A PR that newly introduces a wildcard CORS policy, a disabled-authorization marker, or a wildcard allowed-hosts config produces a finding; a PR that only touches a file containing a pre-existing, unchanged instance of one of these does not.

**Independent Test**: `quickstart.md` Scenarios 1–4.

### Tests for User Story 1

- [X] T002 [US1] Write `backend/AgentGuard.Core.Tests/Rules/OverlyPermissiveAccessRuleTests.cs` (new file, mirroring `SecretDetectedRuleTests.cs`): one case per pattern in `data-model.md`'s table (5 cases) confirming each fires on newly-added content with correct evidence/remediation; a case confirming a pre-existing, unchanged occurrence produces no finding (count unchanged); a case confirming a genuinely new *second* occurrence of an already-present pattern is still flagged (count-based diff, not value-based — research.md §2); a case confirming a *removed* occurrence produces no finding; a case confirming a file with no content (binary/unretrievable) produces no finding without erroring.

### Implementation for User Story 1

- [X] T003 [US1] Define `PermissivePatterns` (5 entries: name, regex, remediation hint) in `backend/AgentGuard.Core/Rules/PermissivePatterns.cs`, per `data-model.md`'s table.
- [X] T004 [US1] Add the `OverlyPermissiveAccess` entry (`RuleId: "OVERLY_PERMISSIVE_ACCESS_CONTROL"`, `Severity.High`, `RiskDimension.Security`) to `backend/AgentGuard.Core/Rules/RuleCatalog.cs`, appended after `SecretDetected` to preserve the original five rules' relative order. Depends on T003.
- [X] T005 [US1] Implement `OverlyPermissiveAccessRule.Evaluate` in `backend/AgentGuard.Core/Rules/OverlyPermissiveAccessRule.cs`: for each changed file, for each pattern, count matches in `OldContent` vs `NewContent`; emit one finding per pattern whose count increased, per `data-model.md`'s evaluation logic. Depends on T003, T004.
- [X] T006 [US1] Wire the new rule into `backend/AgentGuard.Core/AgentGuardAnalyzer.cs`'s fixed rule pipeline (add to the `findingsByRule` array alongside the existing five, after `SecretDetected`). Depends on T005.
- [X] T007 [US1] Update `backend/AgentGuard.Api.Tests/PrRiskAnalysisEndpointTests.cs`'s `body.Checks.Should().HaveCount(5)` assertion to `6` — this is expected, correct maintenance (a 6th rule now exists), not a regression of `005`'s guarantee, which only covers the original five rules' own behavior. Depends on T006.
- [X] T008 [US1] Run `quickstart.md` Scenarios 1–4 against a locally running `dotnet run` instance and confirm. Depends on T006, T007.

**Checkpoint**: User Story 1 (the whole feature) is functional and testable.

---

## Phase 3: Polish & Cross-Cutting Concerns

- [X] T009 Run the full backend suite (`dotnet test backend/AgentGuard.sln`) and confirm everything passes, including all new tests.

---

## Dependencies & Execution Order

- Setup → Tests (T002) → Implementation (T003 → T004 → T005 → T006 → T007) → Live validation (T008) → Polish (T009).
- T002 (tests) is written before T003–T006 (implementation) per TDD, but will not compile until T003–T005 exist — write it first per the template's convention, expect it red until the implementation tasks land.

## Implementation Strategy

Single story, single PR — this feature *is* the MVP. No incremental multi-story delivery needed; validate end-to-end (T008–T009) before shipping.
