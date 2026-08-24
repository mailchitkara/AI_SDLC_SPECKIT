# Quickstart: Validating Newly Introduced TODO/Stub Detection

## Prerequisites

`backend/AgentGuard.Api` running locally (`dotnet run` from `backend/AgentGuard.Api`).

**Note on examples below**: every example payload below is deliberately written with a space
inserted into the marker/keyword (e.g. `TODO` written as `TO DO`) so this document doesn't itself
trip the rule it's describing. Remove the inserted space before running a command for real.

## Scenario 1 — a newly-added TODO comment is flagged (US1 Acceptance Scenario 1)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":50,"prTitle":"Add pricing tier",
    "changedFiles":[{"path":"PricingService.cs","changeType":"MODIFIED","oldContent":"Price = base;","newContent":"Price = base; // TO DO: apply discount rules","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: A finding with `ruleId: "TODO_STUB_INTRODUCED"`, `dimension: "CHANGE_MANAGEMENT"`, `severity: "MEDIUM"`, evidence naming the comment-marker pattern, location `PricingService.cs`.

## Scenario 2 — a newly-added C# not-implemented stub is flagged, distinctly (US1 Acceptance Scenario 2)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":51,"prTitle":"Scaffold refund flow",
    "changedFiles":[{"path":"RefundService.cs","changeType":"ADDED","newContent":"public void Refund() { throw new Not ImplementedException(); }","linesAdded":1,"linesDeleted":0}]
  }'
```

**Expect**: A finding evidencing the C# not-implemented-stub pattern specifically.

## Scenario 3 — a newly-added Python not-implemented stub is flagged (US1 Acceptance Scenario 3)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":52,"prTitle":"Scaffold refund flow",
    "changedFiles":[{"path":"refund_service.py","changeType":"ADDED","newContent":"def refund():\n    raise Not ImplementedError","linesAdded":2,"linesDeleted":0}]
  }'
```

**Expect**: A finding evidencing the Python not-implemented-stub pattern.

## Scenario 4 — an unrelated word containing HACK is not flagged (Edge Case)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":53,"prTitle":"Add event",
    "changedFiles":[{"path":"Events.cs","changeType":"ADDED","newContent":"string eventName = \"Hackathon2026\";","linesAdded":1,"linesDeleted":0}]
  }'
```

**Expect**: No `TODO_STUB_INTRODUCED` finding — "Hackathon2026" is not a standalone "HACK" marker.

## Scenario 5 — pre-existing, untouched marker is not flagged (US1 Acceptance Scenario 5 / FR-002)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":54,"prTitle":"Unrelated change",
    "changedFiles":[{"path":"PricingService.cs","changeType":"MODIFIED","oldContent":"Price = base; // TO DO: old note","newContent":"Price = base; // TO DO: updated note","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: No `TODO_STUB_INTRODUCED` finding — the pattern's occurrence count is unchanged.

## Scenario 6 — a clean PR produces the same result as before this feature (regression)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{"repositoryName":"agentguard-demo","prNumber":1,"prTitle":"Update README","changedFiles":[]}'
```

**Expect**: `score: 0`, `classification: "LOW"`, `recommendation: "SAFE_TO_REVIEW"`, and now **10** checks (not 9).
