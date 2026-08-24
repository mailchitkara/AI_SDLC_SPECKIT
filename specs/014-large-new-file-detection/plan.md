# Implementation Plan: Large New File Detection

**Branch**: `feature/large-new-file-detection` | **Date**: 2026-08-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/014-large-new-file-detection/spec.md`

## Summary

Add one new deterministic rule, `LargeNewFileRule`, under the ChangeManagement risk dimension. Flags a newly-added (`ChangeType.Added`) file whose `LinesAdded` meets or exceeds a fixed threshold. No new data source — evaluates only fields every `ChangedFile` already carries. No new API contract, no new UI work.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged).

**Primary Dependencies**: None new.

**Storage**: N/A — stateless.

**Testing**: xUnit, mirroring `LargeChangeSizeRuleTests.cs`'s structure (this rule's closest sibling — both threshold-based, both operate on line counts already present on `ChangedFile`).

**Target Platform**: Same deployed `agentguard-api` service.

**Project Type**: Extension of `AgentGuard.Core` only. No `AgentGuard.Api` changes — no new dimension needed (reuses `ChangeManagement`), no request/response shape change.

**Performance Goals**: No new target — O(n) over already-supplied changed files, no regex, no content scanning.

**Constraints**: MUST NOT change any of the thirteen existing rules' behavior. MUST NOT require git history, an external API call, or an LLM (FR-008) — deliberately narrower than the phase's harder "true novelty" signal, deferred to a later increment.

**Scale/Scope**: One new rule file, one `RuleCatalog` entry, one `AgentGuardAnalyzer` wiring change, test-count updates. No new project, no new endpoint, no new dimension.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Separation of Concerns**: New rule lives entirely in `AgentGuard.Core`, exactly like every existing rule.
- **UI Contract**: No change needed — existing badges render any rule's findings generically.
- **Deterministic/Contextual track**: This rule is Deterministic (`FindingKind.Deterministic`, `Confidence.Certain`) — a literal threshold comparison on data already in hand, not an inference. Unaffected by, and doesn't touch, the Contextual track's constitutional constraints.
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged.*

## Project Structure

### Documentation (this feature)

```text
specs/014-large-new-file-detection/
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
│   ├── Rules/
│   │   ├── LargeNewFileRule.cs   # new: Evaluate(changeSet) -> findings
│   │   └── RuleCatalog.cs        # changed: + LargeNewFile entry
│   └── AgentGuardAnalyzer.cs     # changed: wire new rule into the fixed pipeline
└── AgentGuard.Core.Tests/
    └── Rules/
        └── LargeNewFileRuleTests.cs  # new

backend/AgentGuard.Api.Tests/
└── (existing endpoint test files)   # "HaveCount(13)" -> 14
```

**Structure Decision**: Purely additive within `AgentGuard.Core`, mirroring `LargeChangeSizeRule`'s file layout and threshold-constant style exactly. No `AgentGuard.Api` source changes at all — the first Phase 4 (or any recent) rule to need none.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
