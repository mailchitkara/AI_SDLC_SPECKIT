# Implementation Plan: Newly Swallowed Exception Detection

**Branch**: `feature/reliability-risk-rules` | **Date**: 2026-08-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-swallowed-exception-detection/spec.md`

## Summary

Add one new deterministic rule, `SwallowedExceptionRule`, under the Reliability risk dimension established in `005-risk-engine-foundation`. Scans each changed file's content for a fixed set of swallowed-error patterns (empty catch block for C#/JS/TS, Python bare-except-with-pass, Go ignored-error-check) — same count-based old-vs-new diffing shape as `006`/`007`. No new API contract, no new UI work.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged).

**Primary Dependencies**: None new — `System.Text.Regex`.

**Storage**: N/A — stateless, same as every existing rule.

**Testing**: xUnit, mirroring `DisabledTestRuleTests.cs`'s structure.

**Target Platform**: Same deployed `agentguard-api` service.

**Project Type**: Extension of `AgentGuard.Core`. No `AgentGuard.Api` contract changes — existing "7 checks" assertions become 8.

**Performance Goals**: No new target — same cost class as the existing pattern-based rules.

**Constraints**: MUST NOT change any of the seven existing rules' behavior. MUST NOT implement a general static-analysis/control-flow engine (FR-008). Per the now-standing precaution (research.md §5 in `007`), every new spec/doc/test string is checked against this rule's own patterns before push.

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
specs/008-swallowed-exception-detection/
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
│       ├── SwallowedExceptionPatterns.cs  # new
│       ├── SwallowedExceptionRule.cs      # new
│       └── RuleCatalog.cs                 # changed: + SwallowedException entry
├── AgentGuard.Core/
│   └── AgentGuardAnalyzer.cs              # changed: wire new rule in
└── AgentGuard.Core.Tests/
    └── Rules/
        └── SwallowedExceptionRuleTests.cs # new

backend/AgentGuard.Api.Tests/
└── (existing endpoint test files)         # "HaveCount(7)" -> 8
```

**Structure Decision**: Purely additive within `AgentGuard.Core`, mirroring the existing rule/pattern-file pairs exactly.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
