---

description: "Task list for Policy-as-Code Configuration Loading"
---

# Tasks: Policy-as-Code Configuration Loading

**Input**: Design documents from `specs/015-policy-as-code/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Included.

**Organization**: Single user story — no Foundational phase needed.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Run `dotnet build backend/AgentGuard.sln` and `dotnet test backend/AgentGuard.sln` to confirm a clean baseline (143/143 tests, per `014-large-new-file-detection`) before starting.

---

## Phase 2: User Story 1 - Configure Forbidden Dependencies and Business-Critical Paths Without Forking AgentGuard (Priority: P1) 🎯 MVP

**Goal**: Both `ForbiddenDependencyConfig` and `BusinessCriticalPathConfig` can be populated from an external JSON file at startup; an unset/missing file behaves exactly as today; a malformed present file fails startup loudly.

**Independent Test**: `quickstart.md` Scenarios 1–4.

### Tests for User Story 1

- [X] T002 [US1] Write `backend/AgentGuard.Api.Tests/Configuration/PolicyFileLoaderTests.cs` (new file): a case confirming a well-formed file with both sections loads both configs correctly; a case confirming a null/empty path returns both configs empty; a case confirming a path to a nonexistent file returns both configs empty without throwing; a case confirming a malformed JSON file throws with a clear message; a case confirming a file with only one section populated leaves the other empty; a case confirming an unrecognized extra JSON field is ignored rather than causing a failure.

### Implementation for User Story 1

- [X] T003 [US1] Implement `PolicyFileLoader.Load` and the `LoadedPolicy` record in `backend/AgentGuard.Api/Configuration/PolicyFileLoader.cs`, per `data-model.md`.
- [X] T004 [US1] Update `backend/AgentGuard.Api/Program.cs` to call `PolicyFileLoader.Load(Environment.GetEnvironmentVariable("AGENTGUARD_POLICY_FILE_PATH"))` and register both resulting configs, replacing the hardcoded `ForbiddenDependencyConfig.Empty` registration and adding the previously-missing `BusinessCriticalPathConfig` one. Depends on T003.
- [X] T005 [US1] Run `quickstart.md` Scenarios 1–4 locally (starting the API with different `AGENTGUARD_POLICY_FILE_PATH` values) and confirm. Depends on T004.

**Checkpoint**: User Story 1 (the whole feature) is functional and testable.

---

## Phase 3: Polish & Cross-Cutting Concerns

- [X] T006 Run the full backend suite (`dotnet test backend/AgentGuard.sln`) and confirm everything passes, including all new tests.

---

## Dependencies & Execution Order

- Setup → Tests (T002) → Implementation (T003 → T004) → Live validation (T005) → Polish (T006).

## Implementation Strategy

Single story, single PR — this feature *is* the MVP.
