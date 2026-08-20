# Implementation Plan: AgentGuard V1 - PR Risk Analysis

**Branch**: `001-pr-risk-analysis-v1` | **Date**: 2026-08-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-pr-risk-analysis-v1/spec.md`

## Summary

Deliver a single-screen PR Risk Analysis experience: a developer supplies one pull request's change data and receives a deterministic risk score (0-100), classification (LOW/MEDIUM/HIGH/CRITICAL), an overall recommendation, and a list of findings from five fixed deterministic rules (large change size, missing related tests, API contract breaking change, architecture/dependency violation, potential secret). Technical approach: a framework-agnostic `AgentGuard.Core` C# class library performs all rule evaluation, weighted scoring (INFO=0/LOW=10/MEDIUM=20/HIGH=35/BLOCKER=100, capped at 100), classification, and recommendation derivation; a thin ASP.NET Core .NET 8 Web API exposes one synchronous REST endpoint over it; a React + TypeScript + Vite single-page UI calls that endpoint and only renders the result. No database, no authentication, no LLM/external AI call, no Docker requirement, no cloud dependency for core analysis — matching both the spec's platform constraints and the project constitution's mandated layering (React UI → ASP.NET Core API → AgentGuard.Core).

## Technical Context

**Language/Version**: C# / .NET 8 (backend: AgentGuard.Core class library + ASP.NET Core Web API); TypeScript 5.x with React 18 (frontend, built with Vite)

**Primary Dependencies**: ASP.NET Core Minimal APIs (.NET 8); React 18 + Vite for the UI; no ORM, no auth middleware, no message bus, no LLM/AI SDK — the feature has no dependency beyond the base web frameworks

**Storage**: N/A — analysis is computed synchronously per request and not persisted (per FR-018); no database of any kind

**Testing**: xUnit + FluentAssertions for `AgentGuard.Core` rule/scoring unit tests; `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) for API integration tests; Vitest + React Testing Library for frontend component/UI tests

**Target Platform**: Self-hosted cross-platform web app (Kestrel, runs on Windows/Linux/macOS without Docker); UI targets modern desktop and tablet browsers (per spec Assumptions)

**Project Type**: Web application — React frontend + ASP.NET Core backend, with a separate framework-agnostic core library (`AgentGuard.Core`) between them, per the constitution's mandated architecture

**Performance Goals**: Complete analysis returned and rendered in under 5 seconds for a PR with under 50 changed files (SC-005); a developer can read the outcome within 10 seconds of the screen loading (SC-001, a UI-clarity goal, not a raw throughput target)

**Constraints**: Deterministic output — identical input MUST always yield an identical score/classification/recommendation (FR-013); no persistence beyond the single request/response (FR-018); no authentication (FR-019); no LLM or external AI call of any kind (FR-020); must run without Docker and without any cloud dependency for core analysis (FR-021); secret evidence MUST be masked everywhere it surfaces — API, UI, logs (FR-010)

**Scale/Scope**: Exactly one PR analyzed per request; five fixed rules; one UI screen (PR Risk Analysis) with a findings list filterable/groupable by severity; typical PR under 50 changed files, "large" defined as >500 changed lines or >20 changed files (FR-003)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate (from `.specify/memory/constitution.md`) | Status | How the plan satisfies it |
|---|---|---|
| UI MUST use React + TypeScript + Vite | PASS | Frontend project is React 18 + TypeScript, built with Vite (Technical Context, Project Structure) |
| Architecture MUST be React UI → REST → ASP.NET Core API → AgentGuard.Core (Rules / Findings / Risk Engine / Policy Engine) | PASS | `AgentGuard.Core` is a standalone class library with `Rules/`, `Findings/`, `RiskEngine/`, `PolicyEngine/` folders; `AgentGuard.Api` is a thin ASP.NET Core .NET 8 host that only translates HTTP ⇄ Core calls; frontend only calls the REST endpoint (see Project Structure) |
| React frontend MUST NOT implement risk-analysis business rules | PASS | All rule evaluation, weighting, classification, and recommendation logic lives in `AgentGuard.Core`; frontend only renders the `RiskAnalysisResult` DTO returned by the API (data-model.md, contracts/) |
| UI-specific requirements MUST NOT leak into AgentGuard.Core | PASS | `AgentGuard.Core` has no reference to ASP.NET Core, HTTP types, or any UI concept; it exposes plain C# types and a single `Analyze(...)` entry point |
| Any displayed risk score MUST include the findings that contributed to it | PASS | `RiskAnalysisResult` always carries its full `Findings[]` alongside `Score`; UI renders both together, and the score is defined as the sum of exactly those findings' weights (FR-013) |
| The React UI MUST consume AgentGuard through documented REST API contracts | PASS | Single documented endpoint in `contracts/openapi.yaml`; frontend's `services/` layer is the only place that calls it |
| V1 UI SHOULD support keyboard navigation, accessible labels, sufficient contrast, responsive layout for desktop/tablet | PASS (SHOULD, planned) | Noted as a frontend design constraint in Project Structure; addressed with semantic HTML, ARIA labels on severity filters, and a responsive layout in the component design (tasks phase will include an accessibility pass) |
| Future capabilities (PR history, policy configuration UI, rule management, spec-to-code compliance, AI vs. human analysis, leadership dashboards) MUST NOT be implemented until separately specified | PASS | None of these are in scope; `PolicyEngine` in V1 is limited to loading the static forbidden-dependency configuration used by FR-006 — no policy *authoring* UI or API is built |

No violations. Complexity Tracking is not needed.

## Project Structure

### Documentation (this feature)

```text
specs/001-pr-risk-analysis-v1/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── openapi.yaml
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── AgentGuard.Core/                   # Framework-agnostic analysis library (no ASP.NET reference)
│   ├── Rules/                         # One evaluator per FR-003..FR-007 (LargeChangeSize, MissingRelatedTests,
│   │                                   # ApiContractBreakingChange, ArchitectureViolation, SecretDetected)
│   ├── Findings/                      # Finding model + evidence masking (FR-008..FR-010)
│   ├── RiskEngine/                    # Severity weight table, score summation/cap, classification bands (FR-012..FR-015)
│   ├── PolicyEngine/                  # Loads the static forbidden-dependency configuration used by FR-006
│   └── Recommendation/                # Classification -> recommendation mapping (FR-016..FR-017)
│
├── AgentGuard.Api/                    # ASP.NET Core .NET 8 host
│   ├── Endpoints/                     # POST /api/pr-risk-analysis (maps request DTO -> Core call -> response DTO)
│   ├── Contracts/                     # Request/response DTOs matching data-model.md and contracts/openapi.yaml
│   └── Program.cs
│
├── AgentGuard.Core.Tests/             # xUnit unit tests: one rule/scoring/classification behavior per FR
└── AgentGuard.Api.Tests/              # WebApplicationFactory integration tests against the real endpoint

frontend/
├── src/
│   ├── pages/
│   │   └── PrRiskAnalysisPage.tsx     # The one V1 screen (User Stories 1-3)
│   ├── components/
│   │   ├── RiskSummary.tsx            # Repo/PR header, score, classification, recommendation (US1)
│   │   ├── ChecksSummary.tsx          # Passed/failed per rule (US3)
│   │   └── FindingsList.tsx           # Findings + severity filter/group (US2)
│   ├── services/
│   │   └── riskAnalysisClient.ts      # Only place that calls the REST API
│   └── types/
│       └── riskAnalysis.ts            # TypeScript types mirroring contracts/openapi.yaml
└── tests/                             # Vitest + React Testing Library
```

**Structure Decision**: Web application split into three code units, matching the constitution's mandated layering rather than the generic two-folder web-app template: `frontend/` (React + TS + Vite, UI only), `backend/AgentGuard.Api/` (thin ASP.NET Core .NET 8 REST host), and `backend/AgentGuard.Core/` (framework-agnostic class library holding Rules, Findings, Risk Engine, and Policy Engine, referenced by the API but with zero HTTP/UI awareness). This keeps risk-analysis logic testable in isolation (`AgentGuard.Core.Tests`) and keeps the API a pure translation layer, satisfying the "UI-specific requirements MUST NOT leak into AgentGuard.Core" constitution gate.

## Complexity Tracking

*No violations — table intentionally omitted.*
