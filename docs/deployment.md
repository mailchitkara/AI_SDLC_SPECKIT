# Deploying AgentGuard to Render

Spec: [specs/002-render-deployment/spec.md](../specs/002-render-deployment/spec.md) · Plan: [specs/002-render-deployment/plan.md](../specs/002-render-deployment/plan.md)

## How it works

- `render.yaml` at the repo root is a Render **Blueprint** — infrastructure-as-code describing two services: the `agentguard-api` Web Service (Docker, `backend/Dockerfile`) and the `agentguard-frontend` Static Site (`npm run build` in `frontend/`, serving `frontend/dist`). Both have `autoDeploy: false` — Render does **not** watch `main` on its own for either.
- The two services are cross-wired via Blueprint env vars: the frontend's `VITE_API_BASE_URL` points at the backend's live URL (baked in at build time, since Vite inlines env vars), and the backend's `FRONTEND_ORIGIN` uses `fromService: { name: agentguard-frontend, property: host }` to pick up the frontend's hostname for CORS — see `backend/AgentGuard.Api/Program.cs`, which prefixes it with `https://` before adding it to the allowed-origins list.
- `.github/workflows/ci.yml` has a `deploy` job: `needs: [backend, frontend]`, and `if: github.event_name == 'push' && github.ref == 'refs/heads/main'`. Only when both hold — the push was to `main`, *and* that commit's build/test jobs succeeded — does it call each service's Render **Deploy Hook**, a per-service webhook URL that tells Render "build and deploy now." Render then does the actual build and deploy itself. The frontend step is a no-op until `RENDER_FRONTEND_DEPLOY_HOOK_URL` is added (see below), so merging the Blueprint change doesn't break the existing backend-only deploy.
- Pull requests run the existing `backend`/`frontend` CI jobs only; the `deploy` job's `if` condition structurally excludes `pull_request` events, so no PR can ever trigger it.
- `healthCheckPath: /health` tells Render how to confirm a new backend revision is actually serving before routing traffic to it.

**CI vs CD, concretely**:

| | Triggered by | Does | Knows about Render? |
|---|---|---|---|
| **CI** (`backend`/`frontend` jobs) | PRs → `main`, pushes to `main`/`Feature/AgentGuard` | `dotnet build`/`dotnet test`, `npm run build`/`npm test` | No |
| **CD** (`deploy` job, same workflow) | Push to `main`, only after CI succeeds for that commit | One `curl` call to the Render Deploy Hook | Only the hook URL (a secret) — no other coupling |

Same workflow file, but CD structurally depends on CI (`needs: [backend, frontend]`) rather than running independently — a failing build/test run cannot reach the deploy step.

## One-time setup (manual, outside version control)

Neither the Render↔GitHub connection nor the deploy hooks can be created from the repository itself:

1. Sign in to [Render](https://render.com) and connect your GitHub account.
2. **New → Blueprint**, select this repository. Render will detect `render.yaml` at the root and show both services (`agentguard-api` Web Service, `agentguard-frontend` Static Site).
3. Approve the plan. Render creates both services and performs an initial deploy of each.
4. For **each** service: open it → **Settings → Deploy Hook** → copy the generated URL. (Note: this is different from the Blueprint's own **Sync Hook**, which re-reads `render.yaml` for infra changes — e.g. adding/removing a service — rather than redeploying an existing one.)
5. In this GitHub repository (**Settings → Secrets and variables → Actions → New repository secret**), add both:
   - `RENDER_DEPLOY_HOOK_URL` — the backend's hook.
   - `RENDER_FRONTEND_DEPLOY_HOOK_URL` — the frontend's hook.
6. From then on, every push to `main` that passes CI triggers a deploy of both services automatically — no further manual steps.

**If `render.yaml` itself changes later** (new service, new env var, etc.), a plain deploy hook isn't enough — Render needs to re-read the file. Trigger the Blueprint's **Sync Hook** (from the Blueprint's own Settings page, not an individual service's) once after merging such a change, or click **Sync** in the Render dashboard.

## Local validation

From the repository root:

```bash
# Build the image exactly as Render will (context = backend/, per render.yaml)
docker build -f backend/Dockerfile backend

# Run it, simulating Render's dynamic port assignment
docker run -e PORT=8080 -p 8080:8080 <image-id>

# In another terminal:
curl http://localhost:8080/health
curl -X POST http://localhost:8080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{"repositoryName":"demo","prNumber":1,"prTitle":"test","changedFiles":[]}'
```

## Rollback

No custom rollback tooling exists for this service. If a bad `main` commit fails to build or fails its health check, Render keeps the previous revision live automatically. If a deploy passes health checks but is broken in some other way, use Render's dashboard to redeploy a previous successful deploy.
