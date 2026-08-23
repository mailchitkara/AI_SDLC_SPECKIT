# Implementation Plan: Risk Engine Foundation

**Branch**: `feature/risk-engine-foundation` | **Date**: 2026-08-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-risk-engine-foundation/spec.md`

## Summary

Extend `AgentGuard.Core`'s finding and scoring model so every finding carries a risk dimension, a confidence level, a deterministic-vs-contextual classification, and an optional per-finding mandatory-override flag — without changing what the five existing V1 rules detect or how they score. Make the classification score-bands configurable per-request (never server-side state). Thread all of this through both existing analysis endpoints and the frontend's display of results. This is data-model and evaluation-logic work only; no new detection rule is added.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (unchanged) — same `AgentGuard.Core`/`AgentGuard.Api` projects; TypeScript/React (unchanged) for the frontend additions.

**Primary Dependencies**: None new. Pure additions to existing records/enums; no new NuGet or npm packages.

**Storage**: N/A — threshold configuration is per-request only (per Clarifications), never persisted; no new database or config store introduced, consistent with AgentGuard's existing no-database constraint.

**Testing**: xUnit for `AgentGuard.Core.Tests`/`AgentGuard.Api.Tests` (unchanged framework) — every existing test must keep passing unchanged (FR-013's regression guarantee is directly enforced by not touching existing test expectations for score/classification/recommendation); new tests added for dimension/confidence/kind defaults, threshold-band configuration and validation, and mandatory-override behavior. Vitest for the frontend's new badge rendering.

**Target Platform**: Same deployed `agentguard-api` (Render) and `agentguard-frontend` (Render Static Site) services — this is an extension of existing services, not a new deployable.

**Project Type**: Extension of the existing `AgentGuard.Core` data model, `AgentGuard.Api` contracts/endpoints, and `frontend` result-display components.

**Performance Goals**: No new performance target — this is in-memory record/enum work added to an already-fast, CPU-bound pure-function pipeline (`RiskEngine.Evaluate`); no measurable latency impact expected.

**Constraints**: MUST NOT change the score, classification, or recommendation the five existing V1 rules produce for the same input when no threshold override or mandatory-override finding is present (FR-013) — this is the single hardest constraint on this plan and drives the "additive fields, unchanged arithmetic by default" design below. MUST NOT introduce any new detection rule (FR-014). MUST NOT require server-side persistence for threshold configuration (per Clarifications).

**Scale/Scope**: Additive changes across ~14 existing files in `AgentGuard.Core`/`AgentGuard.Api`/`frontend`, plus ~6 new small files (new enums, one new configuration record, one new validator). No new project, no new service.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Separation of Concerns**: All new evaluation logic (dimension assignment per rule, confidence defaults, threshold banding, mandatory-override resolution) stays entirely inside `AgentGuard.Core`. `AgentGuard.Api` only exposes the richer result shape; `frontend` only displays it — neither computes anything new. This is a direct extension of the constitution's existing "Core remains responsible for deterministic analysis, findings and risk calculation" principle, not a new pattern.
- **UI Contract & Accessibility**: The constitution requires the UI to "clearly distinguish BLOCKER, HIGH, MEDIUM, LOW and INFO findings" and "make every risk score explainable." Adding dimension/confidence/kind badges and a visible mandatory-override indicator directly reinforces this requirement rather than introducing a new one.
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged: still additive-only within Core's existing responsibility; no new service, no new persistence, no UI business logic.*

## Project Structure

### Documentation (this feature)

```text
specs/005-risk-engine-foundation/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── AgentGuard.Core/
│   ├── Rules/
│   │   ├── RuleId.cs                     # changed: enum -> stable string-backed identity (FR-001)
│   │   ├── RuleCatalog.cs                # changed: each Rule gains a DefaultDimension
│   │   ├── LargeChangeSizeRule.cs        # changed: findings set Dimension/Confidence/Kind
│   │   ├── MissingRelatedTestsRule.cs    # changed: same
│   │   ├── ApiContractBreakingChangeRule.cs  # changed: same
│   │   ├── ArchitectureViolationRule.cs  # changed: same
│   │   └── SecretDetectedRule.cs         # changed: same (MandatoryOverride left false — see research.md §4)
│   ├── RiskEngine/
│   │   ├── RiskDimension.cs              # new: the 8-value dimension enum
│   │   ├── Confidence.cs                 # new: Certain/High/Medium/Low enum
│   │   ├── ThresholdConfiguration.cs     # new: score-band record + Default + validation
│   │   ├── RiskEngine.cs                 # changed: accepts thresholds, resolves mandatory override
│   │   └── Severity.cs                   # unchanged
│   ├── Findings/
│   │   ├── Finding.cs                    # changed: + Dimension, Confidence, Kind, MandatoryOverride
│   │   └── FindingKind.cs                # new: Deterministic/Contextual enum
│   └── AgentGuardAnalyzer.cs             # changed: accepts optional ThresholdConfiguration
├── AgentGuard.Api/
│   ├── Contracts/
│   │   ├── ThresholdConfigurationRequest.cs      # new + validator
│   │   ├── PullRequestChangeSetRequest.cs        # changed: + optional Thresholds
│   │   ├── PullRequestChangeSetValidator.cs      # changed: + threshold validation
│   │   ├── PrReferenceAnalysisRequest.cs         # changed: + optional Thresholds
│   │   ├── PrReferenceAnalysisRequestValidator.cs # changed: + threshold validation
│   │   ├── RiskAnalysisResultResponse.cs         # changed: + new Finding fields, + RecommendationForcedByOverride
│   │   └── EnumMappings.cs                       # changed: + ToApiString for the 3 new enums
│   └── Endpoints/
│       ├── PrRiskAnalysisEndpoint.cs             # changed: pass thresholds through
│       └── PrReferenceAnalysisEndpoint.cs        # changed: pass thresholds through
├── AgentGuard.Core.Tests/                # existing tests unchanged; new test files for dimension/confidence/threshold/override behavior
└── AgentGuard.Api.Tests/                 # existing tests unchanged; new assertions for the richer response shape

frontend/
├── src/types/riskAnalysis.ts             # changed: + Dimension/Confidence/Kind types, + new Finding/Result fields
├── src/components/FindingsList.tsx       # changed: render dimension/confidence/kind badges
├── src/components/FindingsList.module.css # changed: badge styles for the new fields
├── src/components/RiskSummary.tsx        # changed: show mandatory-override indicator
└── src/components/RiskSummary.module.css # changed: override-indicator style
```

**Structure Decision**: Purely additive within the three existing projects (`AgentGuard.Core`, `AgentGuard.Api`, `frontend`) — no new project, no new service, no change to `render.yaml` or deployment. Mirrors the existing `Rules/`/`RiskEngine/`/`Findings/` split in Core and the existing `Contracts/`/`Endpoints/` split in Api.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
