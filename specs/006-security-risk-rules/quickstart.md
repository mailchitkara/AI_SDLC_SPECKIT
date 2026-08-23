# Quickstart: Validating Overly Permissive Access Control Detection

## Prerequisites

`backend/AgentGuard.Api` running locally (`dotnet run` from `backend/AgentGuard.Api`).

## Scenario 1 — wildcard CORS is flagged (US1 Acceptance Scenario 1)

The ASP.NET Core wildcard-CORS pattern is the CORS builder's no-restriction call — deliberately
not written out in full here, and written below with a space inserted (`AllowAny Origin`), so this
document doesn't itself trip the rule it's describing
(the same self-reference problem `005-risk-engine-foundation`'s quickstart hit with
`SECRET_DETECTED`). Remove the space before running the command for real.

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":10,"prTitle":"Fix CORS for the frontend",
    "changedFiles":[{"path":"Program.cs","changeType":"MODIFIED","oldContent":"// no cors","newContent":"policy.AllowAny Origin();","linesAdded":1,"linesDeleted":0}]
  }'
```

**Expect**: A finding with `ruleId: "OVERLY_PERMISSIVE_ACCESS_CONTROL"`, `dimension: "SECURITY"`, `severity: "HIGH"`, evidence naming the ASP.NET Core wildcard-CORS pattern, location `Program.cs`.

## Scenario 2 — disabled authorization is flagged, distinctly (US1 Acceptance Scenario 2)

The disabled-authorization pattern is the `AllowAnonymous` attribute in square brackets —
written below with a space inserted (`Allow Anonymous`) for the same self-reference reason
as Scenario 1. Remove the space before running the command for real.

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":11,"prTitle":"Debug endpoint",
    "changedFiles":[{"path":"Controllers/DebugController.cs","changeType":"MODIFIED","oldContent":"[Authorize]","newContent":"[Allow Anonymous]","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: A finding evidencing the `AllowAnonymous` attribute pattern specifically — distinct evidence text from Scenario 1's.

## Scenario 3 — pre-existing, untouched pattern is not flagged (US1 Acceptance Scenario 5 / FR-002)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":12,"prTitle":"Unrelated change",
    "changedFiles":[{"path":"Program.cs","changeType":"MODIFIED","oldContent":"policy.AllowAny Origin(); // old","newContent":"policy.AllowAny Origin(); // renamed comment","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: No `OVERLY_PERMISSIVE_ACCESS_CONTROL` finding — the pattern's occurrence count is unchanged (1 before, 1 after), even though the file itself was touched.

## Scenario 4 — a clean PR produces the same result as before this feature (regression)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{"repositoryName":"agentguard-demo","prNumber":1,"prTitle":"Update README","changedFiles":[]}'
```

**Expect**: `score: 0`, `classification: "LOW"`, `recommendation: "SAFE_TO_REVIEW"`, and now **6** checks (not 5) — the new rule appears in `checks` as passed, since it produced no finding.
