# Implementation Plan: Business-Critical Path Detection

**Branch**: `feature/business-critical-path-detection` | **Date**: 2026-08-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/013-business-critical-path-detection/spec.md`

## Summary

Add one new deterministic rule, `BusinessCriticalPathRule`, under a new `RiskDimension.BusinessCriticality` dimension — the first Phase 4 ("Contextual Risk Intelligence") increment, and the narrowest possible one: a configuration-driven path match with an empty default, mirroring `ArchitectureViolationRule`'s `ForbiddenDependencyConfig` shape exactly. No git history, no external data, no LLM — pure structural context about *where* a change lands, evaluated entirely from the PR's own already-supplied changed-file paths.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged).

**Primary Dependencies**: None new.

**Storage**: N/A — stateless; configuration is supplied via DI, not persisted, matching `ForbiddenDependencyConfig`.

**Testing**: xUnit, mirroring `ArchitectureViolationRuleTests.cs`'s structure for a configurable-list rule.

**Target Platform**: Same deployed `agentguard-api` service.

**Project Type**: Extension of `AgentGuard.Core` (new config type, new rule, new `RiskDimension` value) and `AgentGuard.Api` (new dimension-string mapping only — no request/response shape change, since configuration is DI-supplied like `ForbiddenDependencyConfig`, not request data). No `frontend` changes — the existing dimension badge already renders any dimension value generically.

**Performance Goals**: No new target — same cost class as `ArchitectureViolationRule`'s existing pattern matching.

**Constraints**: MUST NOT change any of the twelve existing rules' behavior. MUST default to empty configuration, producing zero findings, matching FR-002. MUST NOT fetch git history, call an external service, or use an LLM (FR-010) — this increment is deliberately scoped below Phase 4's harder areas.

**Scale/Scope**: One new config type (`BusinessCriticalPathConfig`, mirroring `ForbiddenDependencyConfig`), one new rule file, one `RuleCatalog` entry, one new `RiskDimension` enum value (+ its `EnumMappings.ToApiString` case), `AgentGuardAnalyzer` constructor gains a second optional config parameter (alongside the existing `ForbiddenDependencyConfig`). No new endpoint, no request contract change.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Separation of Concerns**: New rule and config type live entirely in `AgentGuard.Core`, exactly like `ArchitectureViolationRule`/`ForbiddenDependencyConfig`. Only the dimension's wire-format string mapping lives in `AgentGuard.Api`, matching where every other dimension's mapping already lives.
- **UI Contract**: No change needed — the existing dimension badge renders any `RiskDimension` value generically; no new UI code is required for a new enum value to display correctly.
- **Deterministic/Contextual track (new, per the constitution's recently-amended Analysis Engine section)**: This rule is squarely Deterministic — `FindingKind.Deterministic`, `Confidence.Certain`, a literal, reproducible pattern match with no inference. It does not touch, and is not constrained by, the Contextual track's constraints (those govern Phase 3 only).
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged.*

## Project Structure

### Documentation (this feature)

```text
specs/013-business-critical-path-detection/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output
```

No `contracts/` directory — configuration is DI-supplied, not request data; no API request/response shape changes.

### Source Code (repository root)

```text
backend/
├── AgentGuard.Core/
│   ├── PolicyEngine/
│   │   └── BusinessCriticalPathConfig.cs   # new: mirrors ForbiddenDependencyConfig exactly
│   ├── RiskEngine/
│   │   └── RiskDimension.cs                # changed: + BusinessCriticality value
│   ├── Rules/
│   │   ├── BusinessCriticalPathRule.cs     # new: Evaluate(changeSet, config) -> findings
│   │   └── RuleCatalog.cs                  # changed: + BusinessCriticalPath entry
│   └── AgentGuardAnalyzer.cs               # changed: constructor gains 2nd optional config param
├── AgentGuard.Api/
│   └── Contracts/
│       └── EnumMappings.cs                 # changed: + BusinessCriticality -> "BUSINESS_CRITICALITY"
└── AgentGuard.Core.Tests/
    └── Rules/
        └── BusinessCriticalPathRuleTests.cs  # new
```

**Structure Decision**: Mirrors `ArchitectureViolationRule`/`ForbiddenDependencyConfig`'s exact file layout and configuration-injection shape — no new architectural pattern, a direct reuse of an already-proven one for a new dimension.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
