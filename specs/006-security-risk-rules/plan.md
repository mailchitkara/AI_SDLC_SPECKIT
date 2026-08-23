# Implementation Plan: Overly Permissive Access Control Detection

**Branch**: `feature/security-risk-rules` | **Date**: 2026-08-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/006-security-risk-rules/spec.md`

## Summary

Add one new deterministic rule, `OverlyPermissiveAccessRule`, under the Security risk dimension established in `005-risk-engine-foundation`. It scans each changed file's content for a fixed set of overly-permissive access-control patterns (wildcard CORS, disabled authorization, wildcard allowed-hosts) across a few common stacks, flagging only patterns whose occurrence *count* increased between the file's old and new content — never patterns that were already present and untouched. No new API contract, no new UI work: the finding flows through the exact response shape `005` already established.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged).

**Primary Dependencies**: None new — `System.Text.Regex`, already used by the existing `SecretPatterns`/`ArchitectureViolationRule`.

**Storage**: N/A — stateless, same as every existing rule.

**Testing**: xUnit, mirroring `SecretDetectedRuleTests.cs`'s structure exactly (this rule is architecturally its sibling).

**Target Platform**: Same deployed `agentguard-api` service — this is an additive rule, not a new deployable.

**Project Type**: Extension of `AgentGuard.Core` (new rule + pattern set) and its wiring into `AgentGuardAnalyzer`/`RuleCatalog`. No `AgentGuard.Api` contract changes needed (research.md §3) — existing tests that hardcode "5 checks" do need updating, since the check *count* genuinely changes from 5 to 6 (this is expected, correct maintenance, not a regression of `005`'s guarantee, which only promised the original five rules' own behavior stays unchanged).

**Performance Goals**: No new target — a handful of additional regex passes per changed file, same cost class as the existing `SecretDetectedRule`/`ArchitectureViolationRule`.

**Constraints**: MUST NOT change any of the five V1 rules' or `005`'s new fields' existing behavior (only additive: a 6th rule joins the existing five). MUST NOT reimplement a general SAST engine (FR-008) — a fixed, reviewable pattern list only, matching `SecretPatterns`' proven shape exactly.

**Scale/Scope**: One new rule file, one new pattern-definitions file, one `RuleCatalog` entry, one `AgentGuardAnalyzer` wiring change, test updates for the "5 checks" assumption. No new project, no new endpoint.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Separation of Concerns**: The new rule lives entirely in `AgentGuard.Core`, exactly like every existing rule — no logic in `AgentGuard.Api` or `frontend`. Direct continuation of the constitution's existing architecture, not a new pattern.
- **UI Contract**: No UI change needed — `005`'s existing dimension/severity/confidence badges already render any rule's findings generically. This rule's findings will display through that same, already-built path.
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged.*

## Project Structure

### Documentation (this feature)

```text
specs/006-security-risk-rules/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

No `contracts/` directory — no API request/response shape changes (research.md §3).

### Source Code (repository root)

```text
backend/
├── AgentGuard.Core/
│   └── Rules/
│       ├── PermissivePatterns.cs        # new: fixed set of (name, regex, remediation) entries
│       ├── OverlyPermissiveAccessRule.cs # new: Evaluate(changeSet) -> findings, count-based diff
│       └── RuleCatalog.cs                # changed: + OverlyPermissiveAccess entry
├── AgentGuard.Core/
│   └── AgentGuardAnalyzer.cs             # changed: wire the new rule into the fixed pipeline
└── AgentGuard.Core.Tests/
    ├── Rules/
    │   └── OverlyPermissiveAccessRuleTests.cs  # new
    └── (existing test files)                    # "5 checks"-style assertions updated to 6

backend/AgentGuard.Api.Tests/
└── (existing endpoint test files)          # "HaveCount(5)"-style assertions updated to 6
```

**Structure Decision**: Purely additive within `AgentGuard.Core`, mirroring the file layout of the existing `SecretDetectedRule`/`SecretPatterns` pair exactly. No `AgentGuard.Api` or `frontend` source changes — only test-assertion updates where they hardcoded the old rule count.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
