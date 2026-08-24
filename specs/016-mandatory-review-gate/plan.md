# Implementation Plan: Mandatory Review Gate by Risk Dimension

**Branch**: `feature/mandatory-review-gate` | **Date**: 2026-08-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/016-mandatory-review-gate/spec.md`

## Summary

Add a new operator-configured policy — a set of risk dimensions for which any finding forces the recommendation to at least `HUMAN_REVIEW_REQUIRED` — applied as a post-processing floor in `RiskEngine.Evaluate`, after score/classification/`MandatoryOverride` are already computed. Configured through `015-policy-as-code`'s existing JSON policy file, extended with a third section. New `RecommendationForcedByGovernancePolicy` response field distinguishes this mechanism from the existing `RecommendationForcedByOverride`.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged).

**Primary Dependencies**: None new — extends `015`'s existing `System.Text.Json`-based loader.

**Storage**: N/A — stateless, loaded once at startup, matching every other config in this codebase.

**Testing**: xUnit. `RiskEngine` gets new direct test cases (it already has `RiskEngineTests.cs`); `PolicyFileLoader` gets new cases for the third section; `AgentGuardAnalyzer`/endpoint tests get one end-to-end case.

**Target Platform**: Same deployed `agentguard-api` service.

**Project Type**: Extension of `AgentGuard.Core` (`RiskEngine.Evaluate` gains a parameter and a response field; a new `RiskGovernancePolicy` type) and `AgentGuard.Api` (`PolicyFileLoader` gains a third section; `EnumMappings` gains dimension-string parsing; `RiskAnalysisResultResponse` gains one field; `Program.cs` wires the new config). No `frontend` changes — the existing recommendation display already shows whatever recommendation the backend computes; a future increment could add a "why" badge distinguishing override vs. governance-policy, but isn't required for this response field to be correct.

**Performance Goals**: No new target — a single `Any()` check over already-computed findings.

**Constraints**: MUST NOT change any of the fourteen existing rules' evaluation logic, `MandatoryOverride` semantics, or score arithmetic (FR-007). MUST default to an empty dimension set, producing byte-for-byte identical results to today (FR-003, SC-002, SC-004). MUST fail loudly on an unrecognized dimension name in the policy file (FR-006), matching `015`'s established malformed-content philosophy.

**Scale/Scope**: One new Core type (`RiskGovernancePolicy`), a `RiskEngine.Evaluate` signature/logic change (additive parameter, additive floor logic), a `ScoredRisk`/`RiskAnalysisResult` field addition, one `AgentGuardAnalyzer` constructor parameter, one `PolicyFileLoader` section, one `EnumMappings` addition, one `RiskAnalysisResultResponse` field, a `Program.cs` wiring change. No new endpoint, no new request field.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Separation of Concerns**: The policy type and floor logic live in `AgentGuard.Core` (`RiskEngine`), exactly where `MandatoryOverride`'s own floor/ceiling logic already lives. JSON parsing of the new policy-file section stays in `AgentGuard.Api`'s `PolicyFileLoader`, matching `015`'s precedent exactly.
- **UI Contract**: No change required — the existing recommendation display already renders whatever recommendation value the backend produces; this feature changes which value that is under specific configured conditions, not the display contract itself.
- **Deterministic/Contextual track**: This is a Deterministic-track concern only — a policy floor over already-Deterministic findings' dimensions. It does not touch, and is not constrained by, the Contextual track's constitutional rules (those govern individual Contextual findings' own behavior, not this cross-cutting policy layer). Worth noting for future Phase 3 work: once Contextual findings exist, this policy will apply to them too if their dimension is configured — consistent with the constitution's own statement that Contextual findings must never *themselves* force `BLOCK_MERGE`, since this policy's ceiling is `HUMAN_REVIEW_REQUIRED`, strictly below that limit.
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged.*

## Project Structure

### Documentation (this feature)

```text
specs/016-mandatory-review-gate/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── contracts/           # Phase 1 output — this feature changes the API response shape
│   └── governance-policy-response-field.md
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
backend/
├── AgentGuard.Core/
│   ├── RiskEngine/
│   │   ├── RiskGovernancePolicy.cs   # new
│   │   ├── RiskEngine.cs             # changed: + governancePolicy param, floor logic
│   │   └── RiskAnalysisResult.cs     # changed: ScoredRisk/RiskAnalysisResult + RecommendationForcedByGovernancePolicy
│   └── AgentGuardAnalyzer.cs         # changed: constructor gains 3rd config param, passes through
├── AgentGuard.Api/
│   ├── Configuration/
│   │   └── PolicyFileLoader.cs       # changed: + mandatoryReviewDimensions section
│   ├── Contracts/
│   │   ├── EnumMappings.cs           # changed: + TryParseRiskDimension
│   │   └── RiskAnalysisResultResponse.cs  # changed: + RecommendationForcedByGovernancePolicy field
│   └── Program.cs                    # changed: register RiskGovernancePolicy
└── AgentGuard.Core.Tests/
    └── RiskEngineTests.cs            # changed: + governance-floor test cases

backend/AgentGuard.Api.Tests/
├── Configuration/
│   └── PolicyFileLoaderTests.cs      # changed: + mandatoryReviewDimensions cases
└── PrRiskAnalysisEndpointTests.cs    # changed: + one end-to-end case
```

**Structure Decision**: Touches shared scoring infrastructure (`RiskEngine.cs`) for the first time since `005-risk-engine-foundation` itself — every rule PR since then has been purely additive at the rule level. This is expected and unavoidable for a cross-cutting policy layer; the change itself is additive (a new optional parameter, a new field, both defaulting to no-op), not a rewrite of existing logic.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
