# Deploying AgentGuard.Api to Render

Spec: [specs/002-render-deployment/spec.md](../specs/002-render-deployment/spec.md) · Plan: [specs/002-render-deployment/plan.md](../specs/002-render-deployment/plan.md)

## How it works

- `render.yaml` at the repo root is a Render **Blueprint** — infrastructure-as-code describing one Web Service (`agentguard-api`), built from `backend/Dockerfile`. `autoDeploy: false` — Render does **not** watch `main` on its own.
- Instead, `.github/workflows/ci.yml` has a `deploy` job: `needs: [backend, frontend]`, and `if: github.event_name == 'push' && github.ref == 'refs/heads/main'`. Only when both hold — the push was to `main`, *and* that commit's build/test jobs succeeded — does it call the Render **Deploy Hook**, a per-service webhook URL that tells Render "build and deploy now." Render then does the actual Docker build and deploy itself.
- Pull requests run the existing `backend`/`frontend` CI jobs only; the `deploy` job's `if` condition structurally excludes `pull_request` events, so no PR can ever trigger it.
- `healthCheckPath: /health` tells Render how to confirm a new revision is actually serving before routing traffic to it.

**CI vs CD, concretely**:

| | Triggered by | Does | Knows about Render? |
|---|---|---|---|
| **CI** (`backend`/`frontend` jobs) | PRs → `main`, pushes to `main`/`Feature/AgentGuard` | `dotnet build`/`dotnet test`, `npm run build`/`npm test` | No |
| **CD** (`deploy` job, same workflow) | Push to `main`, only after CI succeeds for that commit | One `curl` call to the Render Deploy Hook | Only the hook URL (a secret) — no other coupling |

Same workflow file, but CD structurally depends on CI (`needs: [backend, frontend]`) rather than running independently — a failing build/test run cannot reach the deploy step.

## One-time setup (manual, outside version control)

Neither the Render↔GitHub connection nor the deploy hook can be created from the repository itself:

1. Sign in to [Render](https://render.com) and connect your GitHub account.
2. **New → Blueprint**, select this repository. Render will detect `render.yaml` at the root.
3. Approve the plan Render shows (one Web Service, Free plan, Docker runtime, `autoDeploy` off). Render creates the service and performs an initial deploy.
4. In the service's **Settings → Deploy Hook**, copy the generated URL.
5. In this GitHub repository: **Settings → Secrets and variables → Actions → New repository secret**, name it `RENDER_DEPLOY_HOOK_URL`, paste the value.
6. From then on, every push to `main` that passes CI triggers a deploy automatically — no further manual steps.

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
