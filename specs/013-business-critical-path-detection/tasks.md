---

description: "Task list for Business-Critical Path Detection"
---

# Tasks: Business-Critical Path Detection

**Input**: Design documents from `specs/013-business-critical-path-detection/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [quickstart.md](./quickstart.md)

**Tests**: Included, mirroring `ArchitectureViolationRuleTests.cs`'s structure.

**Organization**: Single user story — no Foundational phase needed.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [X] T001 Run `dotnet build backend/AgentGuard.sln` and `dotnet test backend/AgentGuard.sln` to confirm a clean baseline (132/132 tests, per `012-vulnerable-dependency-adapter`) before starting.

---

## Phase 2: User Story 1 - Flag a Change Landing in a Business-Critical Area (Priority: P1) 🎯 MVP

**Goal**: A PR touching a file matching a configured critical-path pattern produces a finding naming the pattern's label; with no configuration supplied, zero findings are produced regardless of what the PR touches.

**Independent Test**: `quickstart.md` Scenarios 1–4.

### Tests for User Story 1

- [X] T002 [US1] Write `backend/AgentGuard.Core.Tests/Rules/BusinessCriticalPathRuleTests.cs` (new file, mirroring `ArchitectureViolationRuleTests.cs`): a case confirming a matching file produces a finding with the correct label/evidence; a case confirming a file matching two configured patterns produces two findings; a case confirming an empty (default) configuration produces zero findings regardless of changed files; a case confirming a non-matching file produces no finding; a case confirming a deleted (not just modified/added) matching file still fires.

### Implementation for User Story 1

- [X] T003 [US1] Add `RiskDimension.BusinessCriticality` to `backend/AgentGuard.Core/RiskEngine/RiskDimension.cs`, and `RiskDimension.BusinessCriticality => "BUSINESS_CRITICALITY"` to `backend/AgentGuard.Api/Contracts/EnumMappings.cs`.
- [X] T004 [US1] Define `BusinessCriticalPath` record and `BusinessCriticalPathConfig` class in `backend/AgentGuard.Core/PolicyEngine/BusinessCriticalPathConfig.cs`, mirroring `ForbiddenDependency`/`ForbiddenDependencyConfig`'s matching semantics exactly, per `data-model.md`.
- [X] T005 [US1] Add the `BusinessCriticalPath` rule entry (`RuleId: "BUSINESS_CRITICAL_PATH_TOUCHED"`, `Severity.Medium`, `RiskDimension.BusinessCriticality`) to `backend/AgentGuard.Core/Rules/RuleCatalog.cs`, appended after `VulnerableDependency`. Depends on T003, T004.
- [X] T006 [US1] Implement `BusinessCriticalPathRule.Evaluate(changeSet, config)` in `backend/AgentGuard.Core/Rules/BusinessCriticalPathRule.cs`, per `data-model.md`'s evaluation logic (no count-based diffing — every matching file in the PR fires, mirroring `ArchitectureViolationRule`'s empty-config short-circuit). Depends on T004, T005.
- [X] T007 [US1] Change `AgentGuardAnalyzer`'s constructor to accept a second optional `BusinessCriticalPathConfig? businessCriticalPathConfig = null` parameter (defaulting to `.Empty`), and wire the new rule into the fixed pipeline. Depends on T006.
- [X] T008 [US1] Run `quickstart.md` Scenarios 1–4 against a locally running `dotnet run` instance and confirm — Scenario 3 (no config) demonstrates the existing production deployment's behavior is unaffected, since `AgentGuard.Api`'s DI registration doesn't supply a `BusinessCriticalPathConfig` in this increment (a consuming team wires one in later, exactly as `ForbiddenDependencyConfig` already works). Depends on T007.

**Checkpoint**: User Story 1 (the whole feature) is functional and testable.

---

## Phase 3: Polish & Cross-Cutting Concerns

- [X] T009 Run the full backend suite (`dotnet test backend/AgentGuard.sln`) and confirm everything passes.

---

## Dependencies & Execution Order

- Setup → Tests (T002) → Implementation (T003 → T004 → T005 → T006 → T007) → Live validation (T008) → Polish (T009).

## Implementation Strategy

Single story, single PR — this feature *is* the MVP.
