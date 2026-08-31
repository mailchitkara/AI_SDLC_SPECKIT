# AgentGuard

Deterministic pull-request risk analysis, purpose-built to catch the mistakes AI coding agents
make before merge — not a general linter, not a reimplementation of SAST or dependency-scanning
tools.

- **Live app**: <https://agentguard-frontend-grar.onrender.com>
- **Live API**: <https://agentguard-api-ifb3.onrender.com>
- **Full guide**: [`docs/HELP.md`](./docs/HELP.md) — what it checks, why each check matters for
  agentic code, the roadmap, and how to use it from the web UI, the REST API, or directly from
  .NET code.

## Quick facts

- 14 deterministic rules across 9 risk dimensions, plus 2 operator-configurable governance
  policies.
- No LLM, no non-determinism in the current release — identical input always produces identical
  output (`.specify/memory/constitution.md` makes this a constitutional requirement, not just a
  convention).
- React + TypeScript frontend, ASP.NET Core API, C# analysis engine (`backend/AgentGuard.Core`).
- Built increment by increment with the spec-kit workflow — every feature's spec, plan, research,
  and task breakdown lives under `specs/`.

## Running locally

```bash
# Backend
dotnet test backend/AgentGuard.sln
dotnet run --project backend/AgentGuard.Api

# Frontend (separate terminal)
cd frontend
npm install
npm run dev
```

See [`docs/HELP.md`](./docs/HELP.md) for API request shapes, optional fields, policy
configuration, and everything else needed to actually use it.
