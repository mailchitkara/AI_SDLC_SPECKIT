# Implementation Plan: AgentGuard API - Render Deployment

**Branch**: `002-render-deployment` (git branch: `feature/render-deployment`) | **Date**: 2026-08-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-render-deployment/spec.md`

## Summary

Containerize `AgentGuard.Api` (with its `AgentGuard.Core` dependency) via a multi-stage Docker build producing a minimal ASP.NET Core runtime image, add a dependency-free `/health` endpoint, and deploy it to Render as a Web Service defined by a committed `render.yaml` Blueprint. **Revised 2026-08-22**: deployment is now explicitly triggered by GitHub Actions — a new `deploy` job in the existing `ci.yml` calls a Render Deploy Hook, but only after the `backend`/`frontend` build/test jobs succeed on a push to `main`, and never for pull requests. Render still performs the actual Docker build and hosts the service; Render's own auto-deploy-on-push is disabled (FR-013) so this job is the sole trigger. CI (build/test quality gate) and CD (this new deploy job) live in the same workflow file but remain conceptually distinct: CI answers "is this code correct," CD answers "should Render build and serve it" — the latter now explicitly depends on the former's result via `needs`.

## Technical Context (Deployment Context)

**Containerization**: Docker multi-stage build — `mcr.microsoft.com/dotnet/sdk:8.0` build stage (restore + `dotnet publish`), `mcr.microsoft.com/dotnet/aspnet:8.0` runtime stage (published output only, non-root user)

**Build context**: `backend/` (so the build stage can `COPY` both `AgentGuard.Core/` and `AgentGuard.Api/` as siblings; test projects and everything outside `backend/` excluded via `.dockerignore`)

**Dockerfile location**: `backend/Dockerfile` — co-located with `AgentGuard.sln`, since this is backend-only; the frontend is out of scope for this iteration (spec Assumptions)

**Port binding**: Render injects a `PORT` environment variable at container runtime (not build time). `Program.cs` reads `PORT` (falling back to `8080` for local `docker run`) and calls `builder.WebHost.UseUrls($"http://+:{port}")` — this is the standard pattern for platforms (Render, Heroku-style) that assign the port dynamically; `ASPNETCORE_URLS` alone can't reference a value only known at container start.

**Health check**: ASP.NET Core's built-in health checks middleware (`Microsoft.Extensions.Diagnostics.HealthChecks`, part of the `Microsoft.NET.Sdk.Web` shared framework already referenced — no new package). `builder.Services.AddHealthChecks()` + `app.MapHealthChecks("/health")`, mapped independently of `AgentGuardAnalyzer`/the rules pipeline, satisfying FR-003's "does not depend on the business logic."

**Render configuration**: `render.yaml` Blueprint at repo root — `runtime: docker`, `dockerfilePath: ./backend/Dockerfile`, `dockerContext: ./backend`, `branch: main`, `autoDeploy: false` (FR-013 — deploys are triggered by GitHub Actions, not by Render watching the branch itself), `plan: free`, `healthCheckPath: /health`. Linking this Blueprint to a Render account, then generating a Deploy Hook URL for the service and storing it as the `RENDER_DEPLOY_HOOK_URL` GitHub Actions secret, are one-time manual actions outside repository version control (spec Assumptions).

**CI vs CD responsibilities**:
- **CI** (`backend`/`frontend` jobs in `.github/workflows/ci.yml`, unchanged): PR → build → automated tests → quality gate. Runs on every PR to `main` and every push to `main`/`Feature/AgentGuard`. Has zero knowledge of Render — its job is "is this code correct," full stop.
- **CD** (new `deploy` job, same workflow file): `needs: [backend, frontend]` and `if: github.event_name == 'push' && github.ref == 'refs/heads/main'` — runs only when both prerequisites hold: the event was a push to `main`, *and* the build/test jobs for that exact commit succeeded. Its one step calls the Render Deploy Hook (`curl -fsS -X POST "$RENDER_DEPLOY_HOOK_URL"`); Render then builds the Docker image itself and deploys, with its own health check (`healthCheckPath`) gating whether the new revision receives traffic. Pull request events never satisfy the `if` condition, so the job — and therefore any deploy — never runs for a PR (FR-007, FR-008).

This keeps CI and CD in one workflow file (simple, one place to read the whole pipeline) while making the dependency explicit: a red build/test run structurally cannot reach the deploy step, which is the entire reason GitHub Actions is now in this loop instead of Render blindly watching `main`.

**Branch/deployment policy**: Only a push to `main` can lead to a deploy, and only via the `deploy` job's explicit trigger (FR-006). `feature/render-deployment`, any future feature branches, and all pull requests only ever run the `backend`/`frontend` CI jobs — never `deploy`.

**Rollback considerations**: No custom rollback tooling is built (spec Assumptions). Two platform-native safety nets already cover this:
1. If a new deploy's container fails to build or fails its health check during rollout, Render keeps the previously running revision live — a bad `main` commit doesn't take the service down by itself.
2. If a deploy succeeds and passes health checks but is later found broken in some other way, a maintainer can use Render's dashboard "redeploy a previous successful deploy" action — a platform feature, not something this repository needs to implement.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The constitution (`.specify/memory/constitution.md`) currently covers only UI principles and the React → REST → ASP.NET Core API → AgentGuard.Core layering. It does not mention deployment, hosting, or CI/CD — so this feature is new territory rather than something to be checked against an existing deployment gate.

| Constitution concern | Status | Notes |
|---|---|---|
| React UI MUST use React + TypeScript + Vite | N/A | This feature does not touch the frontend at all (spec Assumptions) |
| Architecture: React UI → REST → ASP.NET Core API → AgentGuard.Core | PASS | Unchanged. Containerizing `AgentGuard.Api` packages the existing layering as-is; no new layer, no logic moved between `Api` and `Core` |
| UI-specific requirements MUST NOT leak into AgentGuard.Core | PASS | The health endpoint lives in `AgentGuard.Api` (a hosting concern), not `AgentGuard.Core` — `Core` remains framework-agnostic |
| Frontend MUST NOT implement risk-analysis business rules | N/A | Not touched by this feature |
| Future capabilities MUST NOT be implemented until separately specified | PASS | This confirms the *pattern* this feature already follows — deployment is being introduced as its own spec (`002-render-deployment`), exactly as the constitution expects new capabilities to arrive |

**No violations.** One gap worth surfacing explicitly (not a violation, since nothing prohibits it): the constitution has no "Deployment & Operations" section at all. That's a reasonable follow-up for `/speckit-constitution` once this deploys successfully, but is out of scope to add speculatively here.

## Project Structure

### Documentation (this feature)

```text
specs/002-render-deployment/
├── plan.md              # This file (/speckit-plan command output)
├── tasks.md             # Phase 2 output (/speckit-tasks command)
└── (no research.md/data-model.md/contracts/quickstart.md — see note below)
```

Note: `research.md`, `data-model.md`, and `contracts/` are omitted for this feature — there are no unresolved technical unknowns to research (the spec + this plan already settled every decision) and no data entities or REST contracts are introduced (the existing `POST /api/pr-risk-analysis` contract is unchanged; `/health` is a one-line platform convention, not a contract worth a separate document). A `quickstart.md`-equivalent (local Docker build/run validation) is folded into the Tasks phase instead, since it's a handful of commands, not a multi-step guide.

### Source Code (repository root)

```text
render.yaml                        # NEW — Render Blueprint (Infrastructure-as-Code)

backend/
├── Dockerfile                     # NEW — multi-stage build (sdk:8.0 → aspnet:8.0)
├── .dockerignore                  # NEW — excludes bin/, obj/, test projects, .git, etc.
├── AgentGuard.Api/
│   └── Program.cs                 # MODIFIED — PORT env var binding + /health endpoint
├── AgentGuard.Core/                (untouched)
├── AgentGuard.Core.Tests/          (untouched)
└── AgentGuard.Api.Tests/           (MODIFIED — one new test asserting /health returns success)

.gitignore                          # MODIFIED — add Docker-related local artifact patterns, if any beyond what already exists

.github/workflows/ci.yml            # MODIFIED — new `deploy` job added; existing `backend`/`frontend` jobs untouched (FR-008)
```

**Structure Decision**: New deployment-only files live at the repository root (`render.yaml`, Render's required Blueprint location) and inside `backend/` (`Dockerfile`, `.dockerignore`, co-located with `AgentGuard.sln` since only the backend is containerized). Application code changes are limited to `Program.cs` (port binding + health endpoint) — everything else in `AgentGuard.Core` and the rules/scoring pipeline is untouched. `ci.yml` gains one new job but its existing `backend`/`frontend` jobs, their triggers, and their behavior are unmodified, keeping this a deployment change, not a behavior change.

## Complexity Tracking

*No violations — table intentionally omitted.* This plan does add one GitHub Actions job (`deploy`) that the original design deliberately avoided — a direct consequence of the explicit decision to make CI gate CD rather than have Render watch `main` independently. No managed database, no container registry, and no Render service beyond the one Web Service are introduced, per FR-012 and the spec's "keep it deliberately simple" requirement — this is the minimum addition needed to make CI-gated deployment true, not scope creep beyond it.
