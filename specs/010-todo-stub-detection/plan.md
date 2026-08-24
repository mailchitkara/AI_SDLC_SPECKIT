# Implementation Plan: Newly Introduced TODO/Stub Detection

**Branch**: `feature/todo-stub-detection` | **Date**: 2026-08-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/010-todo-stub-detection/spec.md`

## Summary

Add one new deterministic rule, `TodoStubRule`, under the ChangeManagement risk dimension. Scans each changed file's content for a fixed set of incompleteness patterns (TODO/FIXME/HACK comment marker, C# `NotImplementedException` stub, Python `NotImplementedError` stub) — same count-based old-vs-new diffing shape as `006`/`007`/`008`. No new API contract, no new UI work.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged).

**Primary Dependencies**: None new — `System.Text.Regex`.

**Storage**: N/A — stateless.

**Testing**: xUnit, mirroring `DisabledTestRuleTests.cs`'s/`SwallowedExceptionRuleTests.cs`'s structure.

**Target Platform**: Same deployed `agentguard-api` service.

**Project Type**: Extension of `AgentGuard.Core`. No `AgentGuard.Api` contract changes — existing "9 checks" assertions become 10.

**Performance Goals**: No new target.

**Constraints**: MUST NOT change any of the nine existing rules' behavior. MUST NOT implement a general code-completeness/static-analysis engine (FR-008). Standing self-tripping-pattern check applies (research.md §5 in `008`).

**Scale/Scope**: One new rule file, one new pattern-definitions file, one `RuleCatalog` entry, one `AgentGuardAnalyzer` wiring change, test-count updates. No new project, no new endpoint.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Separation of Concerns**: New rule lives entirely in `AgentGuard.Core`, exactly like every existing rule.
- **UI Contract**: No change needed — existing badges render any rule's findings generically.
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged.*

## Project Structure

### Documentation (this feature)

```text
specs/010-todo-stub-detection/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output
```

No `contracts/` directory — no API request/response shape changes.

### Source Code (repository root)

```text
backend/
├── AgentGuard.Core/
│   └── Rules/
│       ├── TodoStubPatterns.cs  # new
│       ├── TodoStubRule.cs      # new
│       └── RuleCatalog.cs       # changed: + TodoStub entry
├── AgentGuard.Core/
│   └── AgentGuardAnalyzer.cs    # changed: wire new rule in
└── AgentGuard.Core.Tests/
    └── Rules/
        └── TodoStubRuleTests.cs # new

backend/AgentGuard.Api.Tests/
└── (existing endpoint test files)  # "HaveCount(9)" -> 10
```

**Structure Decision**: Purely additive within `AgentGuard.Core`, mirroring the existing count-based-diff rule/pattern-file pairs exactly (`OverlyPermissiveAccessRule`, `DisabledTestRule`, `SwallowedExceptionRule`).

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
