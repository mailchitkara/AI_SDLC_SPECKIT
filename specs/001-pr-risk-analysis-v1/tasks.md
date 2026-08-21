---

description: "Task list for AgentGuard V1 - PR Risk Analysis"
---

# Tasks: AgentGuard V1 - PR Risk Analysis

**Input**: Design documents from `/specs/001-pr-risk-analysis-v1/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/openapi.yaml](./contracts/openapi.yaml), [quickstart.md](./quickstart.md)

**Tests**: Included. `plan.md` designates dedicated test projects (`AgentGuard.Core.Tests`, `AgentGuard.Api.Tests`, frontend `tests/`) as first-class parts of the architecture, and several success criteria (SC-002 determinism, SC-006 BLOCKER→BLOCK MERGE, SC-007 secret masking) are only verifiable through automated tests.

**Organization**: Tasks are grouped by user story per spec.md priorities (P1/P2/P3). Because the overall risk score (FR-013) is a sum over findings from **all five rules**, the complete backend analysis pipeline and its API endpoint are shared prerequisites for every story and live in Phase 2 (Foundational) rather than being split across stories. Each user story phase then adds exactly the frontend slice that story's acceptance scenarios require, and is independently testable against the already-complete backend.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Every task includes exact file path(s)

## Path Conventions

Per `plan.md` Project Structure: `backend/AgentGuard.Core/`, `backend/AgentGuard.Api/`, `backend/AgentGuard.Core.Tests/`, `backend/AgentGuard.Api.Tests/`, `frontend/src/`, `frontend/tests/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and tooling

- [X] T001 Create backend solution `backend/AgentGuard.sln` and four projects — `backend/AgentGuard.Core/` (class library), `backend/AgentGuard.Api/` (ASP.NET Core .NET 8 Web API), `backend/AgentGuard.Core.Tests/`, `backend/AgentGuard.Api.Tests/` — with project references `AgentGuard.Api → AgentGuard.Core`, `AgentGuard.Core.Tests → AgentGuard.Core`, `AgentGuard.Api.Tests → AgentGuard.Api`
- [X] T002 [P] Scaffold frontend React + TypeScript + Vite project in `frontend/` (`frontend/package.json`, `frontend/src/`, `frontend/vite.config.ts`)
- [X] T003 [P] Add xUnit + FluentAssertions to `backend/AgentGuard.Core.Tests/AgentGuard.Core.Tests.csproj` and `Microsoft.AspNetCore.Mvc.Testing` to `backend/AgentGuard.Api.Tests/AgentGuard.Api.Tests.csproj` (xUnit is included by the project template; FluentAssertions and Mvc.Testing added explicitly, pinned to the net8.0-compatible 8.0.x line)
- [X] T004 [P] Add Vitest + React Testing Library to `frontend/package.json` and create `frontend/vitest.config.ts`
- [X] T005 [P] Add `backend/.editorconfig` and enable nullable reference types across backend projects (`<Nullable>enable</Nullable>` already set by the .NET 8 templates in all four `.csproj` files)
- [X] T006 [P] Configure ESLint + Prettier for `frontend/` (`frontend/eslint.config.js`, `frontend/.prettierrc`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The complete, tested backend analysis pipeline (all 5 rules → deterministic score → classification → recommendation) and the API endpoint that exposes it, plus the frontend's data layer — required before any user story's UI can show correct results

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Shared data model (data-model.md)

- [X] T007 [P] Define `Severity` enum with weight lookup (INFO=0, LOW=10, MEDIUM=20, HIGH=35, BLOCKER=100) in `backend/AgentGuard.Core/RiskEngine/Severity.cs`
- [X] T008 [P] Define `ChangeType` enum and `ChangedFile`, `PullRequestChangeSet` records in `backend/AgentGuard.Core/PullRequestChangeSet.cs`
- [X] T009 [P] Define `Finding`, `CheckResult`, `RiskClassification` enum, `Recommendation` enum, `RiskAnalysisResult` records in `backend/AgentGuard.Core/Findings/Finding.cs` and `backend/AgentGuard.Core/RiskEngine/RiskAnalysisResult.cs`
- [X] T010 [P] Define the fixed 5-entry `Rule` catalog (id, name, default severity) in `backend/AgentGuard.Core/Rules/RuleCatalog.cs`

### Risk engine mechanics (FR-012..FR-017)

- [X] T011 Implement `RiskEngine` in `backend/AgentGuard.Core/RiskEngine/RiskEngine.cs`: sum finding weights capped at 100 (FR-013), derive classification from score bands 0-24/25-49/50-74/75-100 (FR-015), derive recommendation from classification (FR-016) — depends on T007, T009
- [X] T012 [P] Implement stable finding ordering (severity descending, then rule id) in `backend/AgentGuard.Core/Findings/FindingOrdering.cs`

### Rule evaluators (FR-003..FR-007) — each is an independent, parallelizable unit

- [X] T013 [P] Implement `LargeChangeSizeRule` (fires when total changed lines > 500 OR changed files > 20; severity LOW) in `backend/AgentGuard.Core/Rules/LargeChangeSizeRule.cs`
- [X] T014 [P] Implement `FileClassifier` (classifies a path as Source/Test/Other using configurable patterns, default patterns per research.md §3) in `backend/AgentGuard.Core/Rules/FileClassifier.cs`
- [X] T015 [P] Implement `MissingRelatedTestsRule` (fires when a Source file changes and no Test file changes; severity MEDIUM) in `backend/AgentGuard.Core/Rules/MissingRelatedTestsRule.cs` — depends on T014
- [X] T016 [P] Implement `ApiContractBreakingChangeRule` (recognizes OpenAPI/Swagger contract files, diffs old/new for: endpoint removed, HTTP method removed, response property removed, optional→required request property; severity HIGH; no other diff flagged) in `backend/AgentGuard.Core/Rules/ApiContractBreakingChangeRule.cs`
- [X] T017 [P] Implement `ForbiddenDependencyConfig` loader (static `{from, to}` list, empty V1 default) in `backend/AgentGuard.Core/PolicyEngine/ForbiddenDependencyConfig.cs`
- [X] T018 [P] Implement `ArchitectureViolationRule` (text-level scan of added imports/usings against configured forbidden relationships; severity HIGH) in `backend/AgentGuard.Core/Rules/ArchitectureViolationRule.cs` — depends on T017
- [X] T019 [P] Implement secret pattern set and `EvidenceMasking` (mask at construction time, e.g. keep first/last 4 characters) in `backend/AgentGuard.Core/Rules/SecretPatterns.cs` and `backend/AgentGuard.Core/Findings/EvidenceMasking.cs`
- [X] T020 [P] Implement `SecretDetectedRule` (matches recognized secret patterns; severity BLOCKER; constructs findings only from masked evidence, per FR-010) in `backend/AgentGuard.Core/Rules/SecretDetectedRule.cs` — depends on T019

### Orchestration & API

- [X] T021 Implement `AgentGuardAnalyzer.Analyze(PullRequestChangeSet)` in `backend/AgentGuard.Core/AgentGuardAnalyzer.cs`, running all 5 rules and passing findings through `RiskEngine` to produce a `RiskAnalysisResult` with all 5 `CheckResult` entries (FR-002, FR-011) — depends on T011-T020
- [X] T022 [P] Define request/response DTOs matching `contracts/openapi.yaml` in `backend/AgentGuard.Api/Contracts/PullRequestChangeSetRequest.cs` and `backend/AgentGuard.Api/Contracts/RiskAnalysisResultResponse.cs`
- [X] T023 Implement input validation (required `repositoryName`/`prNumber`/`prTitle`/`changedFiles`, empty `changedFiles` is valid) returning a `ValidationError` body on failure in `backend/AgentGuard.Api/Contracts/PullRequestChangeSetValidator.cs` (FR-002)
- [X] T024 Implement `POST /api/pr-risk-analysis` endpoint in `backend/AgentGuard.Api/Endpoints/PrRiskAnalysisEndpoint.cs`, wiring validation (T023) → `AgentGuardAnalyzer` (T021) → response DTO mapping (T022) — depends on T021, T022, T023
- [X] T025 Wire endpoint routing, DI registrations, and local-dev CORS policy for the frontend origin in `backend/AgentGuard.Api/Program.cs` — depends on T024

### Frontend data layer

- [X] T026 [P] Define TypeScript types mirroring `contracts/openapi.yaml` in `frontend/src/types/riskAnalysis.ts`
- [X] T027 [P] Implement `analyzePullRequest()` API client in `frontend/src/services/riskAnalysisClient.ts` — depends on T026
- [X] T028 Implement `PrRiskAnalysisPage` shell (submission entry point, loading state, error state; no result rendering yet) in `frontend/src/pages/PrRiskAnalysisPage.tsx` — depends on T027

### Foundational tests

- [X] T029 [P] Unit tests for `RiskEngine` (weight table application, 100 cap, all 4 classification bands, all 4 recommendation mappings, BLOCKER→score 100→CRITICAL→BLOCK MERGE invariant) in `backend/AgentGuard.Core.Tests/RiskEngineTests.cs`
- [X] T030 [P] Unit tests for all 5 rules, one test class each, covering boundary cases (exactly 500 lines/20 files does not trigger; PR with only test files does not trigger missing-tests; each of the 4 API-breaking conditions individually plus a non-breaking change that must NOT trigger; empty forbidden-dependency config never triggers; secret finding's evidence never contains the raw fixture secret) in `backend/AgentGuard.Core.Tests/Rules/LargeChangeSizeRuleTests.cs`, `MissingRelatedTestsRuleTests.cs`, `ApiContractBreakingChangeRuleTests.cs`, `ArchitectureViolationRuleTests.cs`, `SecretDetectedRuleTests.cs`
- [X] T031 [P] Determinism test: analyzing the same `PullRequestChangeSet` twice yields byte-for-byte identical `RiskAnalysisResult` (FR-013, SC-002) in `backend/AgentGuard.Core.Tests/DeterminismTests.cs`
- [X] T032 Integration test for `POST /api/pr-risk-analysis` via `WebApplicationFactory` — happy path and the 400 validation-error case — in `backend/AgentGuard.Api.Tests/PrRiskAnalysisEndpointTests.cs` — depends on T025

**Checkpoint**: Backend analysis pipeline and API are complete and verified; frontend can successfully call the API and receive a `RiskAnalysisResult`. All three user stories below only need to render pieces of that already-correct result.

---

## Phase 3: User Story 1 - View Overall PR Risk Summary (Priority: P1) 🎯 MVP

**Goal**: A developer sees repository, PR number/title, overall risk score, classification, and recommendation on the analysis screen.

**Independent Test**: Submit a clean-PR fixture (no findings) and verify the screen shows a low score, LOW classification, SAFE TO REVIEW; submit a secret-triggering fixture and verify score 100, CRITICAL, BLOCK MERGE.

### Tests for User Story 1

- [X] T033 [P] [US1] Component test for `RiskSummary` — renders repo/PR number/title/score/classification/recommendation for a clean-PR fixture and a BLOCKER fixture — in `frontend/tests/RiskSummary.test.tsx`

### Implementation for User Story 1

- [X] T034 [US1] Implement `RiskSummary` component in `frontend/src/components/RiskSummary.tsx`
- [X] T035 [US1] Integrate `RiskSummary` into `frontend/src/pages/PrRiskAnalysisPage.tsx` — depends on T034, T028
- [X] T036 [US1] Apply responsive layout and accessible labels/contrast to `RiskSummary` (frontend/src/components/RiskSummary.tsx`) per constitution UI Principles

**Checkpoint**: User Story 1 is fully functional and independently testable/demoable.

---

## Phase 4: User Story 2 - Review Individual Findings by Severity (Priority: P2)

**Goal**: A developer inspects individual findings (rule id, name, severity, explanation, evidence, location, remediation) and filters/groups them by severity.

**Independent Test**: Submit a fixture that trips at least two rules; verify each finding shows all required fields, a location-less finding omits location, and filtering by severity narrows the list correctly.

### Tests for User Story 2

- [X] T037 [P] [US2] Component test for `FindingsList` — renders all finding fields, omits location when absent, filters to a single severity on selection — in `frontend/tests/FindingsList.test.tsx`

### Implementation for User Story 2

- [X] T038 [US2] Implement `FindingsList` component with severity filter/group control in `frontend/src/components/FindingsList.tsx`
- [X] T039 [US2] Integrate `FindingsList` into `frontend/src/pages/PrRiskAnalysisPage.tsx` — depends on T038, T035
- [X] T040 [US2] Make the severity filter control keyboard-navigable with accessible labels in `frontend/src/components/FindingsList.tsx` (native `<select>` + associated `<label htmlFor>`)

**Checkpoint**: User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Review Passed/Failed Checks Summary (Priority: P3)

**Goal**: A developer sees, at a glance, which of the five rules passed and which failed.

**Independent Test**: Submit a fixture that trips exactly two of the five rules; verify those two show failed and the remaining three show passed.

### Tests for User Story 3

- [X] T041 [P] [US3] Component test for `ChecksSummary` — all 5 checks rendered with correct pass/fail per fixture — in `frontend/tests/ChecksSummary.test.tsx`

### Implementation for User Story 3

- [X] T042 [US3] Implement `ChecksSummary` component in `frontend/src/components/ChecksSummary.tsx`
- [X] T043 [US3] Integrate `ChecksSummary` into `frontend/src/pages/PrRiskAnalysisPage.tsx` — depends on T042, T039

**Checkpoint**: All user stories are independently functional together on one screen.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature validation spanning all stories

- [X] T044 [P] Execute all 7 validation steps in `specs/001-pr-risk-analysis-v1/quickstart.md` end-to-end against the running backend + frontend (steps 3/4/6 via curl, step 5 via a real headless-browser session: clean PR → SAFE_TO_REVIEW; secret PR → score 100/CRITICAL/BLOCK_MERGE with masked evidence; multi-rule PR rendered correctly with severity filtering; identical requests byte-for-byte identical; automated suites green)
- [X] T045 [P] Accessibility pass on the full `frontend/src/pages/PrRiskAnalysisPage.tsx` (keyboard navigation across all controls, ARIA labels, contrast check) per constitution UI Principles — verified via headless browser: severity filter is reachable and operable via keyboard alone (Tab to focus, Arrow+Enter to change value) and has a properly associated `<label for>`
- [X] T046 Manually verify no unmasked secret value appears in any API response body, browser network output, or backend console/log output for a secret-triggering fixture (FR-010, SC-007) — verified via curl (raw response body) and headless browser (network response body, rendered result DOM, console); the raw secret appears only in the developer's own pasted textarea input, never in any system-produced output

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories (the full analysis pipeline and API must exist and be correct first, since score/classification/recommendation depend on all 5 rules together)
- **User Stories (Phase 3-5)**: All depend on Foundational (Phase 2) completion. US2 and US3 also integrate into the same `PrRiskAnalysisPage` that US1 establishes (T035), so within this feature they are implemented in priority order rather than fully parallel, though each remains independently testable via its own component test.
- **Polish (Phase 6)**: Depends on all three user stories being complete

### Within Each User Story

- Component test before the component it tests
- Component implementation before page integration
- Story complete (including page integration) before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] (T002-T006) can run in parallel after T001
- Within Foundational: data model tasks T007-T010 in parallel; all 5 rule evaluators (T013, T014→T015, T016, T017→T018, T019→T020) in parallel with each other since each touches a different file; all foundational test tasks T029-T031 in parallel
- Within each user story, the [P]-marked component test can run alongside setup of that story's other groundwork, but must finish before its corresponding implementation task since the test drives the component

---

## Parallel Example: Foundational Rule Evaluators

```bash
# After T007-T012 (shared model + engine) are done, launch all 5 rule evaluators together:
Task: "Implement LargeChangeSizeRule in backend/AgentGuard.Core/Rules/LargeChangeSizeRule.cs"
Task: "Implement FileClassifier in backend/AgentGuard.Core/Rules/FileClassifier.cs"
Task: "Implement ApiContractBreakingChangeRule in backend/AgentGuard.Core/Rules/ApiContractBreakingChangeRule.cs"
Task: "Implement ForbiddenDependencyConfig in backend/AgentGuard.Core/PolicyEngine/ForbiddenDependencyConfig.cs"
Task: "Implement secret pattern set + EvidenceMasking in backend/AgentGuard.Core/Rules/SecretPatterns.cs"
```

## Parallel Example: User Story 1

```bash
Task: "Component test for RiskSummary in frontend/tests/RiskSummary.test.tsx"
# T034 (implementation) starts once T033 exists and fails as expected
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — the entire deterministic analysis pipeline and API; unlike typical CRUD features, this cannot be deferred per-story because the score is a function of all 5 rules)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Run quickstart.md steps 3, 4, and 6 (clean PR, secret PR, determinism) against User Story 1's UI
5. Deploy/demo if ready — this is a usable risk-summary tool even before findings/checks detail is visible

### Incremental Delivery

1. Setup + Foundational → backend fully correct and tested, frontend can call it
2. Add User Story 1 → validate → demo (MVP)
3. Add User Story 2 → validate (quickstart step 5) → demo
4. Add User Story 3 → validate (quickstart step 5) → demo
5. Polish (Phase 6) → full quickstart + accessibility + secret-leak verification

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Because scoring depends on all 5 rules together, "independently testable" for US1-US3 means independently testable **at the UI layer** against a complete, already-verified backend — not independently deployable backends per story
- Commit after each task or logical group
- Stop at any checkpoint to validate a story independently
