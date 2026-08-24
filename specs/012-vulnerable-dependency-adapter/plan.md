# Implementation Plan: Vulnerable Dependency Adapter

**Branch**: `feature/dependency-risk-rules` | **Date**: 2026-08-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/012-vulnerable-dependency-adapter/spec.md`

## Summary

Add one new rule, `VulnerableDependencyRule`, under the Dependencies risk dimension established in `005-risk-engine-foundation`. Unlike every prior Phase 2 rule, it does not scan `ChangedFile` content — it maps an optional, caller-supplied list of already-identified vulnerable dependencies (from an external scanner the caller already ran) into AgentGuard's `Finding` shape. Requires an additive, optional request field on both analysis endpoints, mirroring exactly how `005` added `Thresholds` as a sibling parameter to `AgentGuardAnalyzer.Analyze` rather than a field on `PullRequestChangeSet` — keeping the change orthogonal to every existing rule's diff-scanning logic.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged).

**Primary Dependencies**: None new.

**Storage**: N/A — stateless, same as every existing rule; the supplied list is never persisted.

**Testing**: xUnit. This rule's tests take a plain `IReadOnlyList<VulnerableDependency>` directly (no `PullRequestChangeSet` needed), since it doesn't scan file content — a genuinely different test shape from every prior Phase 2 rule.

**Target Platform**: Same deployed `agentguard-api` service.

**Project Type**: Extension of both `AgentGuard.Core` (new rule, new `VulnerableDependency`/`ExternalSeverity` types, `AgentGuardAnalyzer.Analyze` gains a third optional parameter) and `AgentGuard.Api` (new optional request field on both `PullRequestChangeSetRequest` and `PrReferenceAnalysisRequest`, new validator, new enum-string mapping). No `frontend` changes — existing generic finding rendering already covers this rule's output.

**Performance Goals**: No new target — this rule is O(n) over a caller-supplied list, no regex/content scanning at all.

**Constraints**: MUST NOT change any of the eleven existing rules' behavior — the new `Analyze` parameter defaults to `null`/empty, so every existing call site and test is unaffected (mirrors `005`'s `thresholds` parameter precedent exactly). MUST NOT implement dependency-tree resolution or vulnerability-database querying (FR-008) — translation only. External "critical" severity MUST cap at AgentGuard's High, never Blocker (FR-004, preserving `006`'s established Blocker-exclusivity invariant for `SECRET_DETECTED`).

**Scale/Scope**: New Core types (`VulnerableDependency`, `ExternalSeverity`), one new rule file, one `RuleCatalog` entry, `AgentGuardAnalyzer` signature change (additive), new Api contract type + validator + enum mapping, both endpoints wired, both request validators extended. No new endpoint, no new UI work. This is the seventh and final Phase 2 area — after this ships, Phase 2 is complete per the governance doc's Section 23.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Separation of Concerns**: The new rule's actual logic (mapping severities, building findings) lives entirely in `AgentGuard.Core`. The new request field and its validation live in `AgentGuard.Api`, matching exactly where `Thresholds` and its validator already live — no new pattern, a direct continuation of `005`'s established shape for optional, additive request data.
- **UI Contract**: No frontend change needed — `005`'s existing dimension/severity/confidence badges already render any rule's findings generically, and this rule's findings carry no new fields.
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged.*

## Project Structure

### Documentation (this feature)

```text
specs/012-vulnerable-dependency-adapter/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── contracts/           # Phase 1 output — this feature changes the API request shape
│   └── vulnerable-dependencies-field.md
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
backend/
├── AgentGuard.Core/
│   ├── Dependencies/
│   │   └── VulnerableDependency.cs        # new: VulnerableDependency record, ExternalSeverity enum
│   ├── Rules/
│   │   ├── VulnerableDependencyRule.cs    # new: Evaluate(vulnerableDependencies) -> findings
│   │   └── RuleCatalog.cs                 # changed: + VulnerableDependency entry
│   └── AgentGuardAnalyzer.cs              # changed: Analyze gains a 3rd optional parameter
├── AgentGuard.Api/
│   ├── Contracts/
│   │   ├── VulnerableDependencyRequest.cs         # new: request DTO + validator + mapping
│   │   ├── EnumMappings.cs                        # changed: + TryParseExternalSeverity
│   │   ├── PullRequestChangeSetRequest.cs         # changed: + VulnerableDependencies field
│   │   ├── PullRequestChangeSetValidator.cs       # changed: validate the new field
│   │   ├── PrReferenceAnalysisRequest.cs          # changed: + VulnerableDependencies field
│   │   └── PrReferenceAnalysisRequestValidator.cs # changed: validate the new field
│   └── Endpoints/
│       ├── PrRiskAnalysisEndpoint.cs              # changed: pass mapped list to Analyze
│       └── PrReferenceAnalysisEndpoint.cs         # changed: pass mapped list to Analyze
└── AgentGuard.Core.Tests/
    └── Rules/
        └── VulnerableDependencyRuleTests.cs       # new

backend/AgentGuard.Api.Tests/
└── (existing endpoint test files)   # + new tests for the field; "HaveCount(11)" -> 12
```

**Structure Decision**: The only Phase 2 increment so far to touch `AgentGuard.Api`'s request contracts — every prior increment (`006`–`011`) was Core-only. This mirrors `005-risk-engine-foundation`'s `Thresholds` addition shape exactly, not a new architectural pattern.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
