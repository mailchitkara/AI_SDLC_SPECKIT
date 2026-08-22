# Implementation Plan: GitHub PR Import for AgentGuard

**Branch**: `003-github-pr-import` | **Date**: 2026-08-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-github-pr-import/spec.md`

## Summary

Add a new endpoint, `POST /api/pr-risk-analysis/from-reference`, to the existing `AgentGuard.Api` project. It accepts a GitHub PR reference (URL, or owner/repository/PR-number) plus an optional credential, retrieves that PR's changed files from GitHub's REST API, maps them onto the existing `PullRequestChangeSet` shape, and runs them through the same `AgentGuardAnalyzer` the manual `/api/pr-risk-analysis` endpoint already uses — so this feature adds a new *input path* into unchanged analysis logic, never a new analysis behavior. GitHub retrieval lives entirely in the API layer via a small typed `HttpClient`, keeping `AgentGuard.Core` free of network I/O and non-determinism.

## Technical Context

**Language/Version**: C# 12 / .NET 8 — same as the existing `AgentGuard.Api`/`AgentGuard.Core` projects; no new language or major version introduced.

**Primary Dependencies**: Built-in `System.Net.Http` (`IHttpClientFactory` + a typed `GitHubPullRequestClient`) and `System.Text.Json`, both already implicit in the ASP.NET Core SDK — no new NuGet package. A dedicated GitHub SDK (Octokit.NET) was considered and rejected; see `research.md` §1.

**Storage**: N/A — no new persistence; each request remains synchronous and stateless, per the spec's Assumptions.

**Testing**: xUnit, matching the existing `AgentGuard.Api.Tests`/`AgentGuard.Core.Tests` projects. GitHub retrieval is abstracted behind an `IGitHubPullRequestClient` interface so endpoint tests substitute a fake client rather than calling the real GitHub API — see `research.md` §2.

**Target Platform**: The existing deployed `agentguard-api` Render service (see `backend/Dockerfile`, `render.yaml`) — this feature is an addition to that same service, not a new deployable.

**Project Type**: Web API extension — new endpoint, contracts, and a GitHub client added to the existing `backend/AgentGuard.Api` project. `backend/AgentGuard.Core` is unmodified.

**Performance Goals**: SC-002's 15-second budget for a typical PR (under 50 files) — bounded primarily by GitHub API round-trip time (2 calls per file plus 2 metadata calls), not by AgentGuard's own analysis, which is already fast and CPU-bound.

**Constraints**: No new persistence; no new AgentGuard user authentication (the optional credential is a GitHub-scoped pass-through only, per FR-006/FR-007); the credential MUST NOT appear in logs — request/response logging middleware (if any is added later) MUST redact the `credential` field; GitHub API calls MUST include a `User-Agent` header (GitHub rejects requests without one).

**Scale/Scope**: One new endpoint, one new GitHub client, extended response/error contracts. No changes to the five existing rules, `RiskEngine`, or the manually-submitted `/api/pr-risk-analysis` endpoint's existing behavior.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The constitution's architecture is `React UI → REST → ASP.NET Core API → AgentGuard.Core`, with Core "responsible for deterministic analysis, findings and risk calculation" and UI/orchestration concerns kept out of Core.

- This feature adds no UI surface (the spec has no UI-facing functional requirements, unlike `001-pr-risk-analysis-v1`'s FR-022–FR-027) — not applicable here.
- **Separation of Concerns, extended to this feature's own I/O**: GitHub retrieval is inherently non-deterministic, network-dependent orchestration — the same category of concern the constitution already keeps out of `AgentGuard.Core` for the UI layer. This plan places the entire `GitHubPullRequestClient` in `AgentGuard.Api`, and `AgentGuardAnalyzer`/`AgentGuard.Core` remain called exactly as they are today, unaware that their input originated from GitHub rather than a manual request body. This is a direct extension of the constitution's existing separation principle, not a new one.
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged: `AgentGuard.Core` gains zero new files or dependencies; all new code is additive within `AgentGuard.Api`.*

## Project Structure

### Documentation (this feature)

```text
specs/003-github-pr-import/
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
├── AgentGuard.Api/
│   ├── Contracts/
│   │   ├── PrReferenceAnalysisRequest.cs        # new: PR reference + optional credential
│   │   ├── PrReferenceAnalysisRequestValidator.cs  # new
│   │   ├── ImportErrorResponse.cs               # new: 400/404/429 error body shape
│   │   └── RiskAnalysisResultResponse.cs        # extended: + PartiallyEvaluatedFiles
│   ├── GitHub/
│   │   ├── IGitHubPullRequestClient.cs          # new
│   │   ├── GitHubPullRequestClient.cs           # new: HttpClient-based implementation
│   │   ├── GitHubPullRequestClientResult.cs     # new: success/not-found/rate-limited outcome type
│   │   └── GitHubFileStatusMapping.cs           # new: GitHub status -> existing ChangeType
│   ├── Endpoints/
│   │   └── PrReferenceAnalysisEndpoint.cs       # new: POST /api/pr-risk-analysis/from-reference
│   └── Program.cs                               # updated: register HttpClient + map new endpoint
└── AgentGuard.Api.Tests/
    └── PrReferenceAnalysisEndpointTests.cs      # new: uses a fake IGitHubPullRequestClient
```

**Structure Decision**: Purely additive within the existing `backend/AgentGuard.Api` project — no new project, no changes to `AgentGuard.Core`, `frontend/`, or `render.yaml`/deployment. Mirrors the existing `Contracts/`/`Endpoints/` split already established by `PrRiskAnalysisEndpoint`; `GitHub/` is a new sibling folder for the one piece of genuinely new capability (an outbound API client), kept separate from `Contracts/` so DTOs and I/O logic don't blur together.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
