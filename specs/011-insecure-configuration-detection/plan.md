# Implementation Plan: Insecure Configuration Detection

**Branch**: `feature/configuration-risk-rules` | **Date**: 2026-08-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/011-insecure-configuration-detection/spec.md`

## Summary

Add one new deterministic rule, `InsecureConfigurationRule`, under the Configuration risk dimension established in `005-risk-engine-foundation`. Scans each changed file's content for a fixed set of insecure-configuration patterns (Django debug mode, .NET/Node.js/Python TLS-certificate-validation-disabling) — same count-based old-vs-new diffing shape as `006`/`007`/`008`/`010`. No new API contract, no new UI work.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged).

**Primary Dependencies**: None new — `System.Text.Regex`.

**Storage**: N/A — stateless.

**Testing**: xUnit, mirroring `OverlyPermissiveAccessRuleTests.cs`'s structure (this rule is its closest sibling — both Security-adjacent, both High severity).

**Target Platform**: Same deployed `agentguard-api` service.

**Project Type**: Extension of `AgentGuard.Core`. No `AgentGuard.Api` contract changes — existing "10 checks" assertions become 11.

**Performance Goals**: No new target.

**Constraints**: MUST NOT change any of the ten existing rules' behavior. MUST NOT implement a general configuration/infrastructure-as-code analyzer (FR-008). Standing self-tripping-pattern check applies.

**Scale/Scope**: One new rule file, one new pattern-definitions file, one `RuleCatalog` entry, one `AgentGuardAnalyzer` wiring change, test-count updates. No new project, no new endpoint. This is the last count-based-diff rule planned for Phase 2 — the one remaining area (dependency scanning) is explicitly an adapter to external tool output, not a new pattern-matching rule, per the governance doc.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Separation of Concerns**: New rule lives entirely in `AgentGuard.Core`, exactly like every existing rule.
- **UI Contract**: No change needed — existing badges render any rule's findings generically.
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged.*

## Project Structure

### Documentation (this feature)

```text
specs/011-insecure-configuration-detection/
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
│       ├── InsecureConfigurationPatterns.cs  # new
│       ├── InsecureConfigurationRule.cs      # new
│       └── RuleCatalog.cs                    # changed: + InsecureConfiguration entry
├── AgentGuard.Core/
│   └── AgentGuardAnalyzer.cs                 # changed: wire new rule in
└── AgentGuard.Core.Tests/
    └── Rules/
        └── InsecureConfigurationRuleTests.cs # new

backend/AgentGuard.Api.Tests/
└── (existing endpoint test files)            # "HaveCount(10)" -> 11
```

**Structure Decision**: Purely additive within `AgentGuard.Core`, mirroring the existing count-based-diff rule/pattern-file pairs exactly.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
