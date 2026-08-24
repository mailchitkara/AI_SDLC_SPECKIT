---

description: "Task list for Vulnerable Dependency Adapter"
---

# Tasks: Vulnerable Dependency Adapter

**Input**: Design documents from `specs/012-vulnerable-dependency-adapter/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/vulnerable-dependencies-field.md](./contracts/vulnerable-dependencies-field.md), [quickstart.md](./quickstart.md)

**Tests**: Included.

**Organization**: Single user story. Touches both `AgentGuard.Core` and `AgentGuard.Api` — the first Phase 2 increment to do so.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Run `dotnet build backend/AgentGuard.sln` and `dotnet test backend/AgentGuard.sln` to confirm a clean baseline (120/120 tests, per `011-insecure-configuration-detection`) before starting.

---

## Phase 2: User Story 1 - Surface Externally-Detected Vulnerable Dependencies (Priority: P1) 🎯 MVP

**Goal**: An analysis request can optionally include already-identified vulnerable dependencies; each produces a finding with severity mapped from its own external severity; omitting the field is behaviorally identical to before this feature; malformed entries are rejected.

**Independent Test**: `quickstart.md` Scenarios 1–6.

### Tests for User Story 1

- [X] T002 [P] [US1] Write `backend/AgentGuard.Core.Tests/Rules/VulnerableDependencyRuleTests.cs` (new file): one finding per entry for each of the 4 severity levels (asserting the Critical->High cap specifically), a case confirming multiple entries produce multiple independent findings, a case confirming an empty list produces no findings, a case confirming evidence includes the advisory id when present and omits it gracefully when absent.
- [X] T003 [P] [US1] Add request-validation test cases to `backend/AgentGuard.Api.Tests/PrRiskAnalysisEndpointTests.cs` (or a new test file) covering: a well-formed entry produces the expected finding end-to-end; an unrecognized severity returns 400; a missing packageName/version returns 400; omitting the field entirely produces the same result as an empty list and updates the "12 checks" baseline.

### Implementation for User Story 1

- [X] T004 [P] [US1] Define `VulnerableDependency` record and `ExternalSeverity` enum in `backend/AgentGuard.Core/Dependencies/VulnerableDependency.cs`, per `data-model.md`.
- [X] T005 [US1] Add the `VulnerableDependency` rule entry (`RuleId: "VULNERABLE_DEPENDENCY_DETECTED"`, nominal `Severity.High`, `RiskDimension.Dependencies`) to `backend/AgentGuard.Core/Rules/RuleCatalog.cs`, appended after `InsecureConfiguration`. Depends on T004.
- [X] T006 [US1] Implement `VulnerableDependencyRule.Evaluate(IReadOnlyList<VulnerableDependency>)` in `backend/AgentGuard.Core/Rules/VulnerableDependencyRule.cs`, per `data-model.md`'s evaluation logic (severity mapping capped at High, no content scanning). Depends on T004, T005.
- [X] T007 [US1] Change `AgentGuardAnalyzer.Analyze`'s signature to accept a third optional `IReadOnlyList<VulnerableDependency>? vulnerableDependencies = null` parameter, and wire the new rule into the fixed pipeline using `vulnerableDependencies ?? []`. Depends on T006.
- [X] T008 [US1] Add `VulnerableDependencyRequest` (DTO + validator + `ToVulnerableDependency()` mapping) to `backend/AgentGuard.Api/Contracts/VulnerableDependencyRequest.cs`, and `TryParseExternalSeverity` to `backend/AgentGuard.Api/Contracts/EnumMappings.cs`, per `contracts/vulnerable-dependencies-field.md`. Depends on T004.
- [X] T009 [US1] Add `VulnerableDependencies` field to `PullRequestChangeSetRequest` and `PrReferenceAnalysisRequest`; extend `PullRequestChangeSetValidator` and `PrReferenceAnalysisRequestValidator` to validate each entry. Depends on T008.
- [X] T010 [US1] Wire both endpoints (`PrRiskAnalysisEndpoint`, `PrReferenceAnalysisEndpoint`) to map and pass the new field into `analyzer.Analyze(...)`'s third parameter. Depends on T007, T009.
- [X] T011 [US1] Update `backend/AgentGuard.Api.Tests/PrRiskAnalysisEndpointTests.cs`'s check-count assertion 11→12. Depends on T010.
- [X] T012 [US1] Run `quickstart.md` Scenarios 1–6 against a locally running `dotnet run` instance and confirm. Depends on T010, T011.

**Checkpoint**: User Story 1 (the whole feature) is functional and testable.

---

## Phase 3: Polish & Cross-Cutting Concerns

- [X] T013 Run the full backend suite (`dotnet test backend/AgentGuard.sln`) and confirm everything passes, including all new tests.

---

## Dependencies & Execution Order

- Setup → Tests (T002, T003) → Core implementation (T004 → T005 → T006 → T007) → Api implementation (T008 → T009 → T010 → T011) → Live validation (T012) → Polish (T013).
- T002/T003 (tests) written before the implementation tasks per TDD; T002 will not compile until T004–T006 exist, T003 not until T007–T011 exist.

## Implementation Strategy

Single story, single PR — this feature *is* the MVP. This closes out Phase 2 (Core Deterministic Risk Rules) entirely — all seven areas from the governance doc's Section 23 will be shipped after this PR merges.
