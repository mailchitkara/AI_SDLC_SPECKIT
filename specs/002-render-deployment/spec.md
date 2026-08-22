# Feature Specification: AgentGuard API - Render Deployment

**Feature Branch**: `feature/render-deployment`

**Created**: 2026-08-21

**Status**: Draft

**Input**: User description: "Deploy the AgentGuard .NET API to Render as a containerised application. Containerise AgentGuard.Api with a multi-stage Docker build, keep the image minimal, correctly reference AgentGuard.Core, bind to the port Render expects, run as a Render Web Service, deploy from main only, preserve existing CI, ensure PRs validate but never deploy, provide a health endpoint for Render health checks, never commit secrets, ignore generated build artifacts, and keep the implementation simple. Deployment trigger: GitHub Actions, after the existing build/test jobs succeed on a push to `main`, calls a Render Deploy Hook to trigger the deploy — Render still builds the Docker image itself server-side (no container registry involved). Plan: Render Free tier."

**Revision (2026-08-22)**: The deployment trigger mechanism changed from Render-native auto-deploy (Render watching `main` directly, no GitHub Actions involvement) to a GitHub Actions-triggered deploy (a new job calls a Render Deploy Hook only after the `backend`/`frontend` CI jobs succeed on a push to `main`). Requested explicitly so that a deploy is contingent on that specific commit's CI run passing, not merely on the commit landing on `main`. Render's role (building the Docker image, running the health check, serving traffic) is unchanged — only *what triggers* the build changed.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automatic Deployment on Merge to Main (Priority: P1)

A maintainer merges an approved pull request into `main`. Without performing any manual deployment step, the updated AgentGuard API becomes reachable at its public Render URL shortly afterward.

**Why this priority**: This is the entire point of the capability — without it, "deployment" is just a container that has to be pushed by hand, which delivers no real workflow value over running it locally.

**Independent Test**: Merge a trivial, verifiable change into `main` (e.g., a change observable through an existing endpoint's response) and confirm the public Render URL reflects it without any manual action beyond the merge itself.

**Acceptance Scenarios**:

1. **Given** a change is merged into `main`, **When** the GitHub Actions build/test jobs for that commit succeed, **Then** a deploy job triggers a Render Deploy Hook, and Render builds a new container image and deploys it automatically.
2. **Given** a deploy has completed, **When** a client sends a request to the public Render URL's existing `POST /api/pr-risk-analysis` endpoint, **Then** it responds exactly as it does when run locally.
3. **Given** a change is merged into `main`, **When** the GitHub Actions build/test jobs for that commit fail, **Then** the deploy job MUST NOT run and no deployment is triggered.

---

### User Story 2 - Health Verification (Priority: P2)

Render (and any operator checking service status) can confirm the deployed API is actually able to serve requests via a dedicated, lightweight health endpoint, independent of the PR-risk-analysis business logic.

**Why this priority**: Without a real health signal, a broken deploy (crashed process, unhandled startup exception) looks identical to a healthy one from the platform's point of view until a real user hits it. This is what lets Render's own health checking do its job.

**Independent Test**: Call the health endpoint directly against the deployed service and confirm a fast, successful response; separately, confirm the endpoint requires no request body, business input, or the rest of the analysis pipeline to succeed.

**Acceptance Scenarios**:

1. **Given** the deployed service is running normally, **When** the health endpoint is called, **Then** it returns a success response.
2. **Given** the health endpoint is called, **When** the response is inspected, **Then** it does not depend on or exercise the PR-risk-analysis rules/scoring pipeline.

---

### User Story 3 - Pull Requests Never Deploy (Priority: P3)

A maintainer opens a pull request against `main`. The existing CI build/test gate runs as before, but at no point does a container get built and deployed to the public Render URL from that PR.

**Why this priority**: This is a safety boundary rather than new capability — it prevents untrusted or in-progress code from ever reaching the public deployment, but the deployment mechanism (P1) has to exist first for this boundary to mean anything.

**Independent Test**: Open a pull request containing a trivial change, observe existing CI run (build + test), and confirm no deploy job runs for that branch and Render's deploy history shows no new deploy triggered by it.

**Acceptance Scenarios**:

1. **Given** a pull request is opened against `main`, **When** CI runs, **Then** it performs the existing build-and-test validation only, and the deploy job does not run (it is scoped to `push` events on `main`, not `pull_request` events).
2. **Given** a pull request is open, **When** its branch receives new commits, **Then** no deployment to Render occurs as a result.

---

### Edge Cases

- What happens when the container image fails to build on Render? The deploy attempt fails and the previously running revision continues serving traffic — a broken `main` never take the service offline by itself.
- What happens when the health endpoint fails immediately after a new deploy? The platform's health check is the signal that the new revision isn't ready; the previous revision remains the one serving traffic until a healthy revision is confirmed.
- What happens when a pull request branch is pushed to directly (not merged)? Existing CI (build + test) still runs per the current workflow's trigger rules; the deploy job does not run for `pull_request` events, so no deploy occurs.
- What happens when the build/test jobs fail on a push to `main` itself (e.g., a flaky test, or a direct push that bypassed PR review)? The deploy job's dependency on those jobs succeeding means it does not run — a failing `main` commit is never deployed, even though it reached `main`.
- What happens if Render's free-tier instance has spun down due to inactivity? The next request incurs a cold-start delay before the health endpoint (and all other endpoints) respond — this is an accepted platform trade-off for the free tier, not a failure.
- What happens if the Render Deploy Hook call itself fails (network error, invalid/rotated hook URL)? The GitHub Actions job fails visibly (non-zero exit from the triggering step) rather than silently succeeding without having deployed anything.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The API MUST be packaged as a container image built via a multi-stage Docker build that compiles `AgentGuard.Api` and its `AgentGuard.Core` dependency in a build stage and produces a minimal runtime image containing only the published output and the ASP.NET Core runtime (no SDK, no source, no test projects).
- **FR-002**: The containerized API MUST bind to the network port supplied by the platform at startup (via the `PORT` environment variable) rather than a hardcoded port.
- **FR-003**: The API MUST expose a dedicated health endpoint that returns a success response when the service is able to serve requests, and that does not depend on the PR-risk-analysis rules/scoring pipeline.
- **FR-004**: The service MUST be deployed as a Render Web Service backed by the container image described in FR-001.
- **FR-005**: The Render service's configuration MUST be defined as version-controlled Infrastructure-as-Code (a Render Blueprint, `render.yaml`) committed to the repository, rather than existing only as manual dashboard configuration.
- **FR-006**: A deployment to Render MUST only be triggerable from a commit on the `main` branch, and only after that commit's automated build-and-test checks have succeeded; no other branch MUST be able to trigger a Render deployment.
- **FR-007**: Pull request validation MUST continue to run only the existing build-and-automated-test checks; opening or updating a pull request MUST NOT trigger a deployment to Render.
- **FR-008**: The existing GitHub Actions CI workflow (backend build+test, frontend build+test) MUST continue to run unchanged as the PR/push validation gate. A new job in that same workflow MUST trigger the Render deployment by calling a Render Deploy Hook, and that job MUST run only when the triggering event is a push to `main` and only after the existing build/test jobs for that commit have succeeded — it MUST NOT run for pull request events.
- **FR-009**: No secret, credential, or connection string MUST be committed to the repository at any point. The Render Deploy Hook URL used by FR-008's deploy job MUST be stored as a GitHub Actions secret, never as plaintext in the workflow file or elsewhere in the repository; any runtime configuration the deployed service itself needs MUST be supplied through Render's own environment variable configuration.
- **FR-010**: Generated Docker/build artifacts MUST be excluded from version control via `.dockerignore` (build context) and `.gitignore` (any local Docker output), consistent with the existing ignore-file conventions in the repository.
- **FR-011**: The container image build MUST correctly resolve `AgentGuard.Api`'s project reference to `AgentGuard.Core`, producing a complete, independently runnable image — not an image that only contains `AgentGuard.Api` in isolation.
- **FR-012**: The deployment MUST NOT introduce any additional Render-managed service (no managed database, no cache, no background worker), consistent with AgentGuard V1's no-database, no-external-dependency design.
- **FR-013**: Render's own automatic deploy-on-push behavior MUST be disabled for this service, so that the GitHub Actions deploy job (FR-008) is the sole trigger for a deployment — a push to `main` MUST NOT independently cause Render to deploy outside of that job.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After a change is merged to `main`, the API is reachable at its public Render URL, reflecting that change, without any manual deployment step, within Render's normal build-and-deploy time for a service of this size.
- **SC-002**: The health endpoint responds successfully in under 5 seconds under normal (non-cold-start) conditions.
- **SC-003**: Across the deploy history, zero deployments are attributable to a pull request branch — every deployment originates from a `main`-branch commit.
- **SC-004**: A review of the repository's committed files and git history for this feature shows zero secrets, credentials, or connection strings.
- **SC-005**: The container image builds successfully from a clean checkout of the repository using only the Dockerfile and repository contents, with no manual local setup steps beyond having Docker itself installed.

## Assumptions

- A Render account, with this GitHub repository connected/authorized, is provisioned by the user outside of this repository's version control — Spec Kit artifacts describe and configure the deployment, but cannot create the Render account itself.
- Generating the Render Deploy Hook URL (in the Render dashboard, once the service exists) and adding it to this repository's GitHub Actions secrets (as `RENDER_DEPLOY_HOOK_URL`) is a one-time manual step the user performs outside version control, for the same reason.
- Only `AgentGuard.Api` (and its `AgentGuard.Core` dependency) is in scope for this deployment. The React frontend is explicitly out of scope for this iteration — the user's requirement names only "the AgentGuard .NET API."
- The Render Free tier's spin-down/cold-start behavior after inactivity is an accepted trade-off for this learning exercise, not a defect to engineer around.
- No custom domain is required; the default Render-provided `*.onrender.com` URL is sufficient for this iteration.
- No authentication/authorization is added as part of this deployment — the deployed API remains exactly as unauthenticated as it is when run locally, matching AgentGuard V1's explicit "no authentication" boundary (spec `001-pr-risk-analysis-v1`, FR-019).
- Rollback, if ever needed, is handled through Render's own built-in "redeploy a previous revision" capability; no custom rollback tooling is built as part of this feature.
