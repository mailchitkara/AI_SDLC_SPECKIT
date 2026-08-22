---

description: "Task list for AgentGuard API - Render Deployment"
---

# Tasks: AgentGuard API - Render Deployment

**Input**: Design documents from `/specs/002-render-deployment/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md) (no research.md/data-model.md/contracts — see plan.md's note on why)

**Tests**: One integration test is included (`/health` returns success independent of the analysis pipeline) — everything else in this feature is infrastructure configuration validated by direct build/run/deploy checks, not unit-testable application logic.

**Organization**: Because `render.yaml`'s `healthCheckPath` references the `/health` endpoint, the endpoint itself is a shared prerequisite (Foundational), not part of User Story 2's own phase — US2's phase is validation of that already-built endpoint against the deployed service. This mirrors how small this feature actually is: three user stories, one shared piece of code, and configuration/validation around it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

## Path Conventions

Per plan.md: `render.yaml` at repo root; `backend/Dockerfile`, `backend/.dockerignore`; changes inside `backend/AgentGuard.Api/`; new `deploy` job in `.github/workflows/ci.yml`.

**Revision (2026-08-22)**: T009, T010, and T017 (new) were updated/added to reflect a changed decision — deployment is now triggered by a GitHub Actions job calling a Render Deploy Hook (gated on the existing `backend`/`frontend` jobs succeeding), rather than Render watching `main` directly. See spec.md's Revision note and FR-006/FR-008/FR-013.

---

## Phase 1: Setup

- [X] T001 [P] Create `backend/.dockerignore` excluding `bin/`, `obj/`, `AgentGuard.Core.Tests/`, `AgentGuard.Api.Tests/`, `.git`, `*.user`, `*.suo`
- [X] T002 [P] Review root `.gitignore` for any Docker-local artifact patterns needed beyond the existing `bin/`/`obj/` coverage — confirmed no gap; Docker's own build cache/layers live outside the repo tree, nothing new to ignore

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Get a correctly-listening, health-checkable container image building locally — required before any user story can be meaningfully deployed or validated

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Modify `backend/AgentGuard.Api/Program.cs`: read the `PORT` environment variable (only overriding the listen URL when it's set, so local `dotnet run`/launchSettings.json behavior is unaffected) and bind Kestrel via `builder.WebHost.UseUrls($"http://+:{port}")` (FR-002)
- [X] T004 Modify `backend/AgentGuard.Api/Program.cs`: add `builder.Services.AddHealthChecks()` and `app.MapHealthChecks("/health")`, mapped independently of `AgentGuardAnalyzer`/the rules pipeline (FR-003)
- [X] T005 [P] Add integration test asserting `GET /health` returns a success status without requiring any PR-analysis request first, in `backend/AgentGuard.Api.Tests/HealthEndpointTests.cs`
- [X] T006 Create `backend/Dockerfile`: build stage (`mcr.microsoft.com/dotnet/sdk:8.0`, restore + `dotnet publish` for `AgentGuard.Api`, correctly resolving the `AgentGuard.Core` project reference) and runtime stage (`mcr.microsoft.com/dotnet/aspnet:8.0`, copy only the publish output, create and switch to a non-root user, `ENTRYPOINT ["dotnet", "AgentGuard.Api.dll"]`) (FR-001, FR-011)
- [ ] T007 Local validation: `docker build -f backend/Dockerfile backend` — **BLOCKED**: Docker is not installed in this environment (`docker: command not found`); could not run. Dockerfile was written and reviewed but not build-verified locally — see risks in final summary.
- [ ] T008 Local validation: `docker run` + curl `/health` and `/api/pr-risk-analysis` — **BLOCKED**: same reason as T007

**Checkpoint**: A correct, minimal, health-checkable container builds and runs locally — every user story below is now deployable/verifiable.

---

## Phase 3: User Story 1 - Automatic Deployment on Merge to Main (Priority: P1) 🎯 MVP

**Goal**: A push to `main` results in the updated API being live on Render with no manual deployment step.

**Independent Test**: Merge a trivial, observable change into `main` and confirm the public Render URL reflects it without any manual action beyond the merge.

### Implementation for User Story 1

- [X] T009 [US1] Create `render.yaml` Blueprint at the repository root: `runtime: docker`, `dockerfilePath: ./backend/Dockerfile`, `dockerContext: ./backend`, `branch: main`, `autoDeploy: false` (deploys are now triggered by GitHub Actions, not by Render watching the branch — FR-013), `plan: free`, `healthCheckPath: /health`, `envVars: [{ key: ASPNETCORE_ENVIRONMENT, value: Production }]` (FR-004, FR-005, FR-006, FR-012)
- [X] T010 [US1] Document the one-time manual steps (connecting this repository's `render.yaml` Blueprint to a Render account/dashboard; generating a Deploy Hook URL for the service and adding it as the `RENDER_DEPLOY_HOOK_URL` GitHub Actions secret) — done in `docs/deployment.md` (spec Assumptions)
- [X] T017 [US1] Add a `deploy` job to `.github/workflows/ci.yml`: `needs: [backend, frontend]`, `if: github.event_name == 'push' && github.ref == 'refs/heads/main'`, one step calling `curl -fsS -X POST "${{ secrets.RENDER_DEPLOY_HOOK_URL }}"` — the existing `backend`/`frontend` jobs and their triggers are otherwise untouched (FR-006, FR-008, FR-009)
- [ ] T011 [US1] Deployment validation — **PENDING** (requires the repository owner to connect the Render Blueprint and add the `RENDER_DEPLOY_HOOK_URL` secret per T010, and requires a push to `main`, which is outside this agent's authority per the Git workflow instructions for this task)

**Checkpoint**: Pushing to `main` alone is sufficient to update the live service.

---

## Phase 4: User Story 2 - Health Verification (Priority: P2)

**Goal**: Render (and any operator) can confirm the deployed API is actually serving requests via a dedicated health endpoint.

**Independent Test**: Call the health endpoint directly against the deployed service and confirm a fast, successful response that doesn't depend on the analysis pipeline.

### Implementation for User Story 2

- [ ] T012 [US2] Deployment validation — **PENDING**, same reason as T011. Locally, the equivalent is covered: `HealthEndpointTests` (T005) passes, confirming `/health` responds successfully independent of the analysis pipeline.

**Checkpoint**: Render's health check — and any human checking service status — has a real, business-logic-independent signal to rely on.

---

## Phase 5: User Story 3 - Pull Requests Never Deploy (Priority: P3)

**Goal**: Opening or updating a pull request against `main` never triggers a Render deployment; existing CI still runs unchanged.

**Independent Test**: Open a pull request with a trivial change, confirm CI runs build+test as before, and confirm Render's deploy history shows no deploy triggered by that branch.

### Implementation for User Story 3

- [ ] T013 [US3] Validation — **PENDING**, same reason as T011 (requires an actual PR and a connected Render account to observe deploy history). What's already verified: the existing `backend`/`frontend` CI jobs and their `pull_request`/`push` triggers in `.github/workflows/ci.yml` are byte-for-byte unchanged (confirmed by diff — only the new `deploy` job was added), and that job's `if` condition structurally excludes `pull_request` events, so a PR cannot satisfy it regardless of what else happens in the run.

**Checkpoint**: All three user stories are independently confirmed working together.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T014 [P] Add a short "Deployment" section documenting the `render.yaml`-based auto-deploy flow and local Docker validation commands — done in `docs/deployment.md` (combined with T010, same content)
- [X] T015 [P] Review `render.yaml`, `backend/Dockerfile`, `backend/.dockerignore`, `docs/deployment.md`, and the `Program.cs` diff for secrets — grep-scanned, clean (FR-009, SC-004)
- [X] T016 Run the full validation suite: `dotnet restore` ✓, `dotnet build --configuration Release` ✓ (0 warnings/errors), `dotnet test` ✓ (37/37: 33 Core + 4 Api including the new health test), `npm run build` ✓, `npm test -- --run` ✓ (6/6, no regression). `docker build` — **not run**, Docker unavailable in this environment (see T007)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories (a correctly-listening, health-checkable image is a precondition for any of the three stories to be verifiable)
- **User Stories (Phase 3-5)**: All depend on Foundational. US1 (`render.yaml`) is what actually enables deployment; US2 and US3 are validations that depend on US1's `render.yaml` existing and being connected to Render, so in practice these run in priority order (P1 → P2 → P3) rather than in parallel, even though each has its own independent test.
- **Polish (Phase 6)**: Depends on all three user stories being validated

### Parallel Opportunities

- T001 and T002 (Setup) in parallel
- T004 and T005 (health endpoint + its test) can be done together; T003 (port binding) is independent of both and can run in parallel
- T014 and T015 (Polish) in parallel

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — port binding, health endpoint, Dockerfile, local build/run validation)
3. Complete Phase 3: User Story 1 (`render.yaml` + one-time Render connection + first automatic deploy)
4. **STOP and VALIDATE**: confirm the public Render URL serves the API correctly
5. This alone is a usable, demoable deployment — US2 and US3 add verification/safety confirmation on top of it, not new capability

### Incremental Delivery

1. Setup + Foundational → a correct container image, ready to deploy
2. Add User Story 1 → validate → the service is live (MVP)
3. Add User Story 2 → validate → health checks confirmed working
4. Add User Story 3 → validate → PR safety boundary confirmed
5. Polish → documentation + secret review + full validation suite

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Commit after each task or logical group
- Do not weaken or remove existing tests to make `dotnet test`/`npm test` pass
