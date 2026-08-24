# Quickstart: Validating the Vulnerable Dependency Adapter

## Prerequisites

`backend/AgentGuard.Api` running locally (`dotnet run` from `backend/AgentGuard.Api`).

## Scenario 1 — one vulnerable dependency entry produces one finding (US1 Acceptance Scenario 1)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":70,"prTitle":"Bump dependencies",
    "changedFiles":[],
    "vulnerableDependencies":[{"packageName":"left-pad","version":"1.3.0","severity":"HIGH","advisoryId":"GHSA-xxxx-xxxx-xxxx","advisoryUrl":"https://github.com/advisories/GHSA-xxxx-xxxx-xxxx"}]
  }'
```

**Expect**: A finding with `ruleId: "VULNERABLE_DEPENDENCY_DETECTED"`, `dimension: "DEPENDENCIES"`, `severity: "HIGH"`, evidence naming `left-pad@1.3.0` and the advisory id.

## Scenario 2 — multiple entries each produce their own finding (US1 Acceptance Scenario 2)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":71,"prTitle":"Bump dependencies",
    "changedFiles":[],
    "vulnerableDependencies":[
      {"packageName":"left-pad","version":"1.3.0","severity":"LOW"},
      {"packageName":"event-stream","version":"3.3.6","severity":"CRITICAL"}
    ]
  }'
```

**Expect**: Two findings. The `LOW` entry maps to `severity: "LOW"`; the `CRITICAL` entry maps to `severity: "HIGH"` (never `"BLOCKER"` — research.md §3), so the `recommendation` is not `BLOCK_MERGE` from this rule alone.

## Scenario 3 — omitting the field entirely is identical to before this feature (US1 Acceptance Scenario 3 / regression)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{"repositoryName":"agentguard-demo","prNumber":1,"prTitle":"Update README","changedFiles":[]}'
```

**Expect**: `score: 0`, `classification: "LOW"`, `recommendation: "SAFE_TO_REVIEW"`, and now **12** checks (not 11) — the new rule appears as passed, since it produced no finding.

## Scenario 4 — an unrecognized severity value is rejected (US1 Acceptance Scenario 4)

```bash
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":72,"prTitle":"Bump dependencies",
    "changedFiles":[],
    "vulnerableDependencies":[{"packageName":"left-pad","version":"1.3.0","severity":"SEVERE"}]
  }'
```

**Expect**: `400` (severity is not one of `LOW`/`MODERATE`/`HIGH`/`CRITICAL`).

## Scenario 5 — a missing required field is rejected (US1 Acceptance Scenario 5)

```bash
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":73,"prTitle":"Bump dependencies",
    "changedFiles":[],
    "vulnerableDependencies":[{"version":"1.3.0","severity":"HIGH"}]
  }'
```

**Expect**: `400` (`packageName` is required).

## Scenario 6 — the from-reference endpoint accepts the same field

```bash
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5080/api/pr-risk-analysis/from-reference \
  -H "Content-Type: application/json" \
  -d '{"prUrl":"https://github.com/chalk/chalk/pull/688","vulnerableDependencies":[{"packageName":"left-pad","version":"1.3.0","severity":"HIGH"}]}'
```

**Expect**: `200` (or a normal GitHub-lookup outcome) — confirms the field is accepted and passed through on this endpoint too, per `data-model.md`'s note that both DTOs gain the field identically.
