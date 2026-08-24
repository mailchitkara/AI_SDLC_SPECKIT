# Implementation Plan: Hand-Edited Generated File Detection

**Branch**: `feature/generated-file-contamination` | **Date**: 2026-08-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/009-generated-file-contamination/spec.md`

## Summary

Add one new deterministic rule, `GeneratedFileModifiedRule`, under the ChangeManagement risk dimension. Unlike `006`/`007`/`008` (which flag a *newly introduced occurrence* of a fixed pattern), this rule flags a *content change to an already-existing file recognized as generated* — a structurally different check: "is this a recognized generated file, and did its content change at all in this PR." No new API contract, no new UI work.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged).

**Primary Dependencies**: None new — `System.Text.Regex`.

**Storage**: N/A — stateless.

**Testing**: xUnit, mirroring the existing Phase 2 rule test files' structure, adapted for this rule's different evaluation shape (no count-based diffing to test; instead: Modified-only, content-actually-changed, extension-or-marker-matched).

**Target Platform**: Same deployed `agentguard-api` service.

**Project Type**: Extension of `AgentGuard.Core`. No `AgentGuard.Api` contract changes — existing "8 checks" assertions become 9.

**Performance Goals**: No new target.

**Constraints**: MUST NOT change any of the eight existing rules' behavior. MUST NOT implement a general build-artifact/codegen-manifest analyzer (FR-010). A structural note worth recording: because this rule only evaluates `ChangeType.Modified` files, and every file this feature's own PR *adds* (spec/research/data-model/quickstart/tasks/new source/new test files) is `ChangeType.Added` rather than `Modified` when analyzed against `main`, this rule cannot self-trip on its own newly-added documentation regardless of content — only the small number of *existing* files this PR modifies (`RuleCatalog.cs`, `AgentGuardAnalyzer.cs`, the endpoint test file) need the standard self-tripping-pattern check (research.md §1 in `008`).

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
specs/009-generated-file-contamination/
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
│       ├── GeneratedFileSignals.cs        # new: extension pattern + content-marker pattern
│       ├── GeneratedFileModifiedRule.cs   # new: Evaluate(changeSet) -> findings
│       └── RuleCatalog.cs                 # changed: + GeneratedFileModified entry
├── AgentGuard.Core/
│   └── AgentGuardAnalyzer.cs              # changed: wire new rule in
└── AgentGuard.Core.Tests/
    └── Rules/
        └── GeneratedFileModifiedRuleTests.cs  # new

backend/AgentGuard.Api.Tests/
└── (existing endpoint test files)         # "HaveCount(8)" -> 9
```

**Structure Decision**: Purely additive within `AgentGuard.Core`. This rule's `Evaluate` is structurally distinct from `OverlyPermissiveAccessRule`/`DisabledTestRule`/`SwallowedExceptionRule` (no count-based diffing loop) but follows the same fixed-pattern-list philosophy.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
