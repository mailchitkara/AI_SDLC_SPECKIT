---

description: "Task list for Mandatory Review Gate by Risk Dimension"
---

# Tasks: Mandatory Review Gate by Risk Dimension

**Input**: Design documents from `specs/016-mandatory-review-gate/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/governance-policy-response-field.md](./contracts/governance-policy-response-field.md), [quickstart.md](./quickstart.md)

**Tests**: Included.

**Organization**: Single user story. Touches shared `RiskEngine` scoring code for the first time since `005` — the change itself is additive, not a rewrite.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Run `dotnet build backend/AgentGuard.sln` and `dotnet test backend/AgentGuard.sln` to confirm a clean baseline (152/152 tests, per `015-policy-as-code`) before starting.

---

## Phase 2: User Story 1 - Guarantee Human Review for Configured Risk Dimensions (Priority: P1) 🎯 MVP

**Goal**: A configured mandatory-review dimension floors the recommendation at `HUMAN_REVIEW_REQUIRED` whenever a matching finding exists and the score alone wouldn't reach it; an empty configuration is a byte-for-byte regression match; an already-`BLOCK_MERGE`/`HUMAN_REVIEW_REQUIRED` PR is unaffected and correctly attributed.

**Independent Test**: `quickstart.md` Scenarios 1–4.

### Tests for User Story 1

- [X] T002 [P] [US1] Add governance-floor test cases to `backend/AgentGuard.Core.Tests/RiskEngineTests.cs`: a case confirming a matching-dimension finding with an otherwise-low score floors the recommendation to `HumanReviewRequired` and sets `RecommendationForcedByGovernancePolicy: true`; a case confirming no configured policy leaves behavior unchanged; a case confirming a `MandatoryOverride` finding that already reaches `BlockMerge` reports `RecommendationForcedByGovernancePolicy: false` even if it's also in a governed dimension; a case confirming a matching-dimension finding whose score already independently reaches `HumanReviewRequired` reports `RecommendationForcedByGovernancePolicy: false` (the floor didn't change anything).
- [X] T003 [P] [US1] Add `mandatoryReviewDimensions` test cases to `backend/AgentGuard.Api.Tests/Configuration/PolicyFileLoaderTests.cs`: a case confirming a well-formed dimension list loads correctly; a case confirming an unrecognized dimension name throws with a clear message; a case confirming an absent section defaults to empty.
- [X] T004 [P] [US1] Add one end-to-end case to `backend/AgentGuard.Api.Tests/PrRiskAnalysisEndpointTests.cs` confirming the new `recommendationForcedByGovernancePolicy` response field defaults to `false` when no policy is configured (matching the existing "safe to review" baseline test's byte-for-byte-unchanged expectation).

### Implementation for User Story 1

- [X] T005 [US1] Define `RiskGovernancePolicy` in `backend/AgentGuard.Core/RiskEngine/RiskGovernancePolicy.cs`, per `data-model.md`.
- [X] T006 [US1] Update `ScoredRisk` and `RiskAnalysisResult` (in `backend/AgentGuard.Core/RiskEngine/RiskEngine.cs` and `RiskAnalysisResult.cs`) to add `RecommendationForcedByGovernancePolicy`. Depends on T005.
- [X] T007 [US1] Update `RiskEngine.Evaluate` to accept an optional `RiskGovernancePolicy? governancePolicy` parameter and apply the floor per `data-model.md`'s logic (causation-aware: only `true` when the floor actually changed the outcome, per research.md §3). Depends on T006.
- [X] T008 [US1] Update `AgentGuardAnalyzer`'s constructor to accept a third optional `RiskGovernancePolicy? riskGovernancePolicy` parameter and pass it through to `RiskEngine.Evaluate`. Depends on T007.
- [X] T009 [US1] Add `EnumMappings.TryParseRiskDimension` to `backend/AgentGuard.Api/Contracts/EnumMappings.cs` (parsing the same wire-format strings `ToApiString` already produces).
- [X] T010 [US1] Update `backend/AgentGuard.Api/Configuration/PolicyFileLoader.cs` to read the new `mandatoryReviewDimensions` section, using `TryParseRiskDimension`, throwing on an unrecognized value (FR-006). Update `LoadedPolicy` to carry the resulting `RiskGovernancePolicy`. Depends on T005, T009.
- [X] T011 [US1] Update `backend/AgentGuard.Api/Program.cs` to register the `RiskGovernancePolicy` from the loaded policy alongside the existing two configs. Depends on T010.
- [X] T012 [US1] Add `RecommendationForcedByGovernancePolicy` to `RiskAnalysisResultResponse` and its mapping in `backend/AgentGuard.Api/Contracts/RiskAnalysisResultResponse.cs`. Depends on T006.
- [X] T013 [US1] Run `quickstart.md` Scenarios 1–4 against a locally running `dotnet run` instance and confirm. Depends on T011, T012.

**Checkpoint**: User Story 1 (the whole feature) is functional and testable.

---

## Phase 3: Polish & Cross-Cutting Concerns

- [X] T014 Run the full backend suite (`dotnet test backend/AgentGuard.sln`) and confirm everything passes, including all new tests.

---

## Dependencies & Execution Order

- Setup → Tests (T002-T004) → Core implementation (T005 → T006 → T007 → T008) → Api implementation (T009 → T010 → T011 → T012) → Live validation (T013) → Polish (T014).

## Implementation Strategy

Single story, single PR — this feature *is* the MVP.
