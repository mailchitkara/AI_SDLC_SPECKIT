---

description: "Task list for Risk Engine Foundation"
---

# Tasks: Risk Engine Foundation

**Input**: Design documents from `specs/005-risk-engine-foundation/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/risk-analysis-response-extensions.md](./contracts/risk-analysis-response-extensions.md), [quickstart.md](./quickstart.md)

**Tests**: Included — FR-013's regression guarantee (existing rules' score/classification/recommendation must not change) is only actually enforced by tests, not by inspection.

**Organization**: Tasks are grouped by user story (US1/US2/US3, per `spec.md`'s priorities), after a Foundational phase that changes `Finding`'s shape once for all three stories rather than three separate breaking changes to the same record.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no unmet dependencies)
- **[Story]**: Which user story this task belongs to
- Every task lists its exact file path

---

## Phase 1: Setup

- [X] T001 Run `dotnet build backend/AgentGuard.sln`, `dotnet test backend/AgentGuard.sln`, and `npm run build && npm test -- --run` in `frontend/` to confirm a clean baseline before starting.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: `Finding`'s record shape changes exactly once here (four new fields) rather than once per user story — US1 consumes `Dimension`/`Confidence`/`Kind`, US3 consumes `MandatoryOverride`, but the record itself only breaks compatibility with old constructor calls a single time.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Define `RiskDimension` enum (`Security, Testing, Compatibility, Architecture, ChangeManagement, Dependencies, Reliability, Configuration`) in `backend/AgentGuard.Core/RiskEngine/RiskDimension.cs`, per `data-model.md`.
- [X] T003 [P] Define `Confidence` enum (`Certain, High, Medium, Low`) in `backend/AgentGuard.Core/RiskEngine/Confidence.cs`.
- [X] T004 [P] Define `FindingKind` enum (`Deterministic, Contextual`) in `backend/AgentGuard.Core/Findings/FindingKind.cs`.
- [X] T005 [P] Define `ThresholdConfiguration` record (`LowMax, MediumMax, HighMax`) with a `Default` matching V1's fixed bands (24, 49, 74) in `backend/AgentGuard.Core/RiskEngine/ThresholdConfiguration.cs`.
- [X] T006 Replace `enum RuleId` with `readonly record struct RuleId(string Value)` in `backend/AgentGuard.Core/Rules/RuleId.cs`, per research.md §1.
- [X] T007 Update `backend/AgentGuard.Core/Rules/RuleCatalog.cs`: `Rule` gains `DefaultDimension`; all five entries construct their `RuleId` from the exact existing wire-format strings (`"LARGE_CHANGE_SIZE"`, etc.) and are assigned dimensions per `data-model.md`'s mapping table. Depends on T002, T006.
- [X] T008 Extend `Finding` in `backend/AgentGuard.Core/Findings/Finding.cs` with `Dimension`, `Confidence`, `Kind`, and `MandatoryOverride` (defaulting to `false`). Depends on T002, T003, T004, T006.
- [X] T009 Update all five rule files (`LargeChangeSizeRule.cs`, `MissingRelatedTestsRule.cs`, `ApiContractBreakingChangeRule.cs`, `ArchitectureViolationRule.cs`, `SecretDetectedRule.cs` in `backend/AgentGuard.Core/Rules/`) to construct their findings with `Dimension` = the rule's `DefaultDimension`, `Confidence: Confidence.Certain`, `Kind: FindingKind.Deterministic`, `MandatoryOverride: false` — per research.md §3, `SecretDetectedRule` does NOT set `MandatoryOverride: true`. Depends on T007, T008.
- [X] T010 Update `backend/AgentGuard.Api/Contracts/EnumMappings.cs`: `RuleId.ToApiString()` becomes a `.Value` passthrough (no behavior change to the strings themselves); add `ToApiString` for `RiskDimension`, `Confidence`, `FindingKind` (SCREAMING_SNAKE_CASE, matching the existing convention). Depends on T002, T003, T004, T006.
- [X] T011 Build the solution and run the existing test suite (`dotnet test backend/AgentGuard.sln`); fix any direct `Finding`/`RuleId` construction in existing tests broken by the new required fields. Confirm all pre-existing assertions (score, classification, recommendation, severity, evidence) still pass unchanged. Depends on T009, T010.

**Checkpoint**: Foundation ready — the data model exists and compiles; no user-visible behavior has changed yet (nothing reads the new fields outside Core).

---

## Phase 3: User Story 1 - Understand a Finding's Full Context (Priority: P1) 🎯 MVP

**Goal**: Every finding's dimension and confidence are visible through both existing API endpoints and the UI, with zero change to existing score/classification/recommendation values.

**Independent Test**: `quickstart.md` Scenarios 1–2.

### Tests for User Story 1

- [X] T012 [US1] Add assertions to `backend/AgentGuard.Api.Tests/PrRiskAnalysisEndpointTests.cs` and `PrReferenceAnalysisEndpointTests.cs`: existing scenarios' `score`/`classification`/`recommendation` are byte-for-byte unchanged (regression per FR-013); the `SECRET_DETECTED` finding shows `dimension: "SECURITY"`, `confidence: "CERTAIN"`, `kind: "DETERMINISTIC"`, `mandatoryOverride: false`; `recommendationForcedByOverride: false` is present on every existing-scenario result.

### Implementation for User Story 1

- [X] T013 [US1] Extend `backend/AgentGuard.Api/Contracts/RiskAnalysisResultResponse.cs`: `FindingResponse` gains `Dimension`, `Confidence`, `Kind`, `MandatoryOverride`; `RiskAnalysisResultResponse` gains `RecommendationForcedByOverride`; update `RiskAnalysisResultResponseMapping.ToResponse` accordingly. Depends on T010.
- [X] T014 [US1] Confirm both `PrRiskAnalysisEndpoint.cs` and `PrReferenceAnalysisEndpoint.cs` surface the new fields with no endpoint-specific changes needed (mapping is centralized in T013) — add a comment noting this if the mapping call site needs no edit; otherwise update the call site. Depends on T013.
- [X] T015 [US1] Extend `frontend/src/types/riskAnalysis.ts`: add `RiskDimension`, `Confidence`, `FindingKind` string-union types; add `dimension`, `confidence`, `kind`, `mandatoryOverride` to `Finding`; add `recommendationForcedByOverride` to `RiskAnalysisResult`.
- [X] T016 [US1] Render dimension and confidence badges alongside the existing severity badge in `frontend/src/components/FindingsList.tsx`, with matching styles in `FindingsList.module.css` (reusing the existing tinted-pill pattern). Depends on T015.
- [X] T017 [US1] Run `quickstart.md` Scenarios 1–2 against a locally running `dotnet run` instance and confirm.

**Checkpoint**: User Story 1 is independently functional and testable.

---

## Phase 4: User Story 2 - Tune Risk Thresholds (Priority: P2)

**Goal**: A caller can optionally supply custom classification score bands per request; omitting them preserves V1's exact default behavior.

**Independent Test**: `quickstart.md` Scenarios 3–4.

### Tests for User Story 2

- [X] T018 [US2] Add `AgentGuard.Core.Tests` cases for `RiskEngine.Evaluate` with a custom `ThresholdConfiguration` (band-boundary cases) and with `thresholds: null` (must match V1 defaults exactly). Add `AgentGuard.Api.Tests` cases for a valid custom `thresholds` object changing classification, and for invalid ones (partial, out-of-order, out-of-range) returning `400`.

### Implementation for User Story 2

- [X] T019 [US2] Update `backend/AgentGuard.Core/RiskEngine/RiskEngine.cs`: `Evaluate` accepts `ThresholdConfiguration? thresholds = null`, defaults to `ThresholdConfiguration.Default`, and derives classification from it instead of the hardcoded bands. Depends on T005.
- [X] T020 [US2] Update `backend/AgentGuard.Core/AgentGuardAnalyzer.cs`: `Analyze` accepts an optional `ThresholdConfiguration?` and passes it through to `RiskEngine.Evaluate`. Depends on T019.
- [X] T021 [US2] Define `ThresholdConfigurationRequest` DTO and its validator (`0 <= lowMax < mediumMax < highMax < 100`, all-or-none) in `backend/AgentGuard.Api/Contracts/`. Depends on T005.
- [X] T022 [US2] Add an optional `Thresholds` field to `PullRequestChangeSetRequest` and `PrReferenceAnalysisRequest`, validate it in their respective validators (`400` on failure, reusing each endpoint's existing error-response shape), map it to `ThresholdConfiguration`, and pass it to `AgentGuardAnalyzer.Analyze` from both endpoints. Depends on T020, T021.
- [X] T023 [US2] Run `quickstart.md` Scenarios 3–4 and confirm.

**Checkpoint**: User Stories 1 and 2 both independently functional.

---

## Phase 5: User Story 3 - Mandatory Override (Priority: P3)

**Goal**: A finding can force `BLOCK_MERGE` independent of score, and the result makes clear when that happened.

**Independent Test**: `quickstart.md` Scenario 5 (Core-level, since no existing rule triggers this yet).

### Tests for User Story 3

- [X] T024 [US3] Add `AgentGuard.Core.Tests` cases: a `Finding` with `Severity: Low` and `MandatoryOverride: true` → `RiskEngine.Evaluate` returns `Recommendation.BlockMerge` and `RecommendationForcedByOverride: true`, regardless of score or configured thresholds; a result with no override-flagged finding → `RecommendationForcedByOverride: false`, recommendation purely score-derived (unchanged from today). Add an `AgentGuard.Api.Tests` case confirming a `SECRET_DETECTED` result still shows `recommendationForcedByOverride: false` (research.md §3).

### Implementation for User Story 3

- [X] T025 [US3] Extend `ScoredRisk` and `RiskEngine.Evaluate` (`backend/AgentGuard.Core/RiskEngine/RiskEngine.cs`) to detect any finding with `MandatoryOverride: true`, force `Recommendation.BlockMerge` when present, and report `RecommendationForcedByOverride`. Depends on T019 (extends the same method).
- [X] T026 [US3] Propagate `RecommendationForcedByOverride` from `ScoredRisk` through `RiskAnalysisResult` (`backend/AgentGuard.Core/RiskEngine/RiskAnalysisResult.cs`) and `AgentGuardAnalyzer.Analyze`. Depends on T020, T025.
- [X] T027 [US3] Show a visible "blocked by mandatory override" indicator in `frontend/src/components/RiskSummary.tsx` when `recommendationForcedByOverride` is true, styled in `RiskSummary.module.css`. Depends on T015.
- [X] T028 [US3] Run `quickstart.md` Scenario 5 (Core-level unit test) and confirm the `SECRET_DETECTED` regression case from T024.

**Checkpoint**: All three user stories independently functional — feature complete per `spec.md`.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T029 [P] Update [docs/github-pr-import.md](../../docs/github-pr-import.md)'s response-shape description (or add a short new doc) to mention the new `dimension`/`confidence`/`kind`/`mandatoryOverride`/`recommendationForcedByOverride`/`thresholds` fields, linking to `contracts/risk-analysis-response-extensions.md`.
- [X] T030 Run the full backend suite (`dotnet test backend/AgentGuard.sln`) and the full frontend suite (`npm run build && npm run lint && npm test -- --run`) and confirm everything passes, including all new tests.
- [X] T031 Run `quickstart.md` Scenario 6 (frontend renders the new fields) via a live dev server.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories. Changes `Finding`'s shape exactly once.
- **User Story 1 (Phase 3)**: Depends on Foundational only. This is the MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational only — independent of US1's API/frontend work, since it extends `RiskEngine`/`AgentGuardAnalyzer` and the request side of the contracts, not the finding-display side.
- **User Story 3 (Phase 5)**: Depends on Foundational, and on T019 specifically (extends the same `RiskEngine.Evaluate` method US2 modifies) — sequenced after US2 for that reason, though its own user-facing behavior is independent of US2's.
- **Polish (Phase 6)**: Depends on all three user stories being complete.

### Within Each User Story

- Tests before implementation (T012 before T013; T018 before T019; T024 before T025).
- Core (`AgentGuard.Core`) changes before the API contract changes that expose them, before the frontend changes that display them.

### Parallel Opportunities

- T002, T003, T004, T005 (Phase 2) are independent new files and can run in parallel.
- T013 (US1) and T019–T021 (US2) touch disjoint files and can proceed in parallel once Foundational is done, despite the sequencing note above about US3 depending on T019.
- T029 (Phase 6) is independent of T030/T031 and can run in parallel.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Setup + Foundational.
2. User Story 1.
3. **STOP and VALIDATE**: `quickstart.md` Scenarios 1–2 — confirms the regression guarantee (FR-013) holds before any further behavior (thresholds, override) is layered on.

### Incremental Delivery

1. Setup + Foundational → data model ready, nothing user-visible changed yet.
2. US1 → richer findings visible via API and UI → validate independently.
3. US2 → configurable thresholds → validate independently.
4. US3 → mandatory override → validate independently.
5. Polish → docs, full-suite confirmation, frontend live check.
