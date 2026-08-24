# Quickstart: Validating Newly Swallowed Exception Detection

## Prerequisites

`backend/AgentGuard.Api` running locally (`dotnet run` from `backend/AgentGuard.Api`).

**Note on examples below**: every example payload below that needs a genuinely empty block is
deliberately written with a space inserted into a keyword (e.g. `catch` written as `ca tch`) so
this document doesn't itself trip the rule it's describing — the same technique `007`'s quickstart
used. Remove the inserted space before running a command for real.

## Scenario 1 — a newly-added empty catch block is flagged (US1 Acceptance Scenario 1)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":30,"prTitle":"Handle the timeout",
    "changedFiles":[{"path":"PaymentService.cs","changeType":"MODIFIED","oldContent":"Charge();","newContent":"try { Charge(); } ca tch (Exception) { }","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: A finding with `ruleId: "SWALLOWED_EXCEPTION_INTRODUCED"`, `dimension: "RELIABILITY"`, `severity: "HIGH"`, evidence naming the empty-catch-block pattern, location `PaymentService.cs`.

## Scenario 2 — a newly-added Python bare except is flagged, distinctly (US1 Acceptance Scenario 2)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":31,"prTitle":"Handle the timeout",
    "changedFiles":[{"path":"payment_service.py","changeType":"MODIFIED","oldContent":"charge()","newContent":"try:\n    charge()\nexcept:\n    pa ss","linesAdded":2,"linesDeleted":0}]
  }'
```

**Expect**: A finding evidencing the bare-except-with-pass pattern specifically. Remove the inserted space in `pa ss` before running for real.

## Scenario 3 — a newly-added Go ignored-error-check is flagged (US1 Acceptance Scenario 3)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":32,"prTitle":"Handle the timeout",
    "changedFiles":[{"path":"payment_service.go","changeType":"MODIFIED","oldContent":"Charge()","newContent":"if err := Charge(); err != n il {\n}","linesAdded":2,"linesDeleted":1}]
  }'
```

**Expect**: A finding evidencing the ignored-Go-error-check pattern. Remove the inserted space in `n il` before running for real.

## Scenario 4 — a catch block that actually handles the error is not flagged (US1 Acceptance Scenario 4)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":33,"prTitle":"Handle the timeout properly",
    "changedFiles":[{"path":"PaymentService.cs","changeType":"MODIFIED","oldContent":"Charge();","newContent":"try { Charge(); } catch (Exception ex) { _logger.LogError(ex, \"charge failed\"); }","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: No `SWALLOWED_EXCEPTION_INTRODUCED` finding — the catch block has a non-empty body.

## Scenario 5 — pre-existing, untouched swallowed error is not flagged (US1 Acceptance Scenario 5 / FR-002)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":34,"prTitle":"Unrelated change",
    "changedFiles":[{"path":"PaymentService.cs","changeType":"MODIFIED","oldContent":"try { Charge(); } ca tch (Exception) { } // old","newContent":"try { Charge(); } ca tch (Exception) { } // renamed","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: No `SWALLOWED_EXCEPTION_INTRODUCED` finding — the pattern's occurrence count is unchanged.

## Scenario 6 — a clean PR produces the same result as before this feature (regression)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{"repositoryName":"agentguard-demo","prNumber":1,"prTitle":"Update README","changedFiles":[]}'
```

**Expect**: `score: 0`, `classification: "LOW"`, `recommendation: "SAFE_TO_REVIEW"`, and now **8** checks (not 7).
