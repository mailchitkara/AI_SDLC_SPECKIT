# Implementation Plan: Newly Disabled Test Detection

**Branch**: `feature/testing-risk-rules` | **Date**: 2026-08-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/007-disabled-test-detection/spec.md`

## Summary

Add one new deterministic rule, `DisabledTestRule`, under the Testing risk dimension established in `005-risk-engine-foundation`. It scans each changed file's content for a fixed set of test-skip/ignore patterns (xUnit's `Skip` parameter, a Jest/Mocha skip modifier or skip-prefixed test function, pytest's skip decorators, a Go test's early-skip call) across a few common test frameworks, flagging only patterns whose occurrence *count* increased between the file's old and new content — never patterns that were already present and untouched. No new API contract, no new UI work: the finding flows through the exact response shape `005` already established. Architecturally this rule is a sibling of `006-security-risk-rules`'s `OverlyPermissiveAccessRule` — same count-based diffing shape, same fixed-pattern-list approach, different pattern family and dimension.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged).

**Primary Dependencies**: None new — `System.Text.Regex`, already used by `SecretPatterns`/`PermissivePatterns`.

**Storage**: N/A — stateless, same as every existing rule.

**Testing**: xUnit, mirroring `OverlyPermissiveAccessRuleTests.cs`'s structure exactly (this rule is architecturally its sibling).

**Target Platform**: Same deployed `agentguard-api` service — this is an additive rule, not a new deployable.

**Project Type**: Extension of `AgentGuard.Core` (new rule + pattern set) and its wiring into `AgentGuardAnalyzer`/`RuleCatalog`. No `AgentGuard.Api` contract changes needed — existing tests that hardcode "6 checks" do need updating, since the check *count* genuinely changes from 6 to 7 (expected, correct maintenance, not a regression — `006`'s guarantee only covers the first six rules' own behavior staying unchanged).

**Performance Goals**: No new target — a handful of additional regex passes per changed file, same cost class as the existing `OverlyPermissiveAccessRule`.

**Constraints**: MUST NOT change any of the six existing rules' behavior (only additive: a 7th rule joins the existing six). MUST NOT implement a general test-coverage or test-quality analyzer (FR-008) — a fixed, reviewable pattern list only, matching `PermissivePatterns`' proven shape exactly. Given this session's established precedent (self-tripping `SECRET_DETECTED` on PRs #10/#14, and this same class of bug caught and fixed proactively on PR #15), every new spec/doc/test string introduced by this feature MUST be checked against the new rule's own patterns before the PR is pushed — literal matching text in this feature's own artifacts must be rewritten (prose description, or compile-time-constant string concatenation for test fixtures) rather than left to trip the gate on this PR.

**Scale/Scope**: One new rule file, one new pattern-definitions file, one `RuleCatalog` entry, one `AgentGuardAnalyzer` wiring change, test updates for the "6 checks" assumption. No new project, no new endpoint.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Separation of Concerns**: The new rule lives entirely in `AgentGuard.Core`, exactly like every existing rule — no logic in `AgentGuard.Api` or `frontend`. Direct continuation of the constitution's existing architecture, not a new pattern.
- **UI Contract**: No UI change needed — `005`'s existing dimension/severity/confidence badges already render any rule's findings generically. This rule's findings will display through that same, already-built path.
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged.*

## Project Structure

### Documentation (this feature)

```text
specs/007-disabled-test-detection/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

No `contracts/` directory — no API request/response shape changes.

### Source Code (repository root)

```text
backend/
├── AgentGuard.Core/
│   └── Rules/
│       ├── DisabledTestPatterns.cs  # new: fixed set of (name, regex, remediation) entries
│       ├── DisabledTestRule.cs      # new: Evaluate(changeSet) -> findings, count-based diff
│       └── RuleCatalog.cs           # changed: + DisabledTest entry
├── AgentGuard.Core/
│   └── AgentGuardAnalyzer.cs        # changed: wire the new rule into the fixed pipeline
└── AgentGuard.Core.Tests/
    ├── Rules/
    │   └── DisabledTestRuleTests.cs # new
    └── (existing test files)         # "6 checks"-style assertions updated to 7

backend/AgentGuard.Api.Tests/
└── (existing endpoint test files)   # "HaveCount(6)"-style assertions updated to 7
```

**Structure Decision**: Purely additive within `AgentGuard.Core`, mirroring the file layout of the existing `OverlyPermissiveAccessRule`/`PermissivePatterns` pair exactly. No `AgentGuard.Api` or `frontend` source changes — only test-assertion updates where they hardcoded the old rule count.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
