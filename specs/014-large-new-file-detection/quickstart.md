# Quickstart: Validating Large New File Detection

## Prerequisites

`backend/AgentGuard.Api` running locally (`dotnet run` from `backend/AgentGuard.Api`).

## Scenario 1 — a large new file is flagged (US1 Acceptance Scenario 1)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":90,"prTitle":"Add pricing engine",
    "changedFiles":[{"path":"PricingEngine.cs","changeType":"ADDED","newContent":"class PricingEngine {}","linesAdded":250,"linesDeleted":0}]
  }'
```

**Expect**: A finding with `ruleId: "LARGE_NEW_FILE_INTRODUCED"`, `dimension: "CHANGE_MANAGEMENT"`, `severity: "MEDIUM"`, evidence noting 250 lines, location `PricingEngine.cs`.

## Scenario 2 — a small new file is not flagged (US1 Acceptance Scenario 2)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":91,"prTitle":"Add a small DTO",
    "changedFiles":[{"path":"PriceDto.cs","changeType":"ADDED","newContent":"record PriceDto(decimal Amount);","linesAdded":1,"linesDeleted":0}]
  }'
```

**Expect**: No `LARGE_NEW_FILE_INTRODUCED` finding.

## Scenario 3 — a large modification to an existing file is not flagged (US1 Acceptance Scenario 3)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":92,"prTitle":"Rewrite pricing logic",
    "changedFiles":[{"path":"PricingEngine.cs","changeType":"MODIFIED","oldContent":"x","newContent":"y","linesAdded":250,"linesDeleted":240}]
  }'
```

**Expect**: No `LARGE_NEW_FILE_INTRODUCED` finding — the file is `MODIFIED`, not `ADDED` (this scenario will still show `LARGE_CHANGE_SIZE` if the PR total crosses that rule's own separate PR-wide threshold — unaffected by this feature).

## Scenario 4 — multiple large new files each produce their own finding (US1 Acceptance Scenario 4)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":93,"prTitle":"Add two new modules",
    "changedFiles":[
      {"path":"PricingEngine.cs","changeType":"ADDED","newContent":"x","linesAdded":250,"linesDeleted":0},
      {"path":"DiscountEngine.cs","changeType":"ADDED","newContent":"x","linesAdded":300,"linesDeleted":0}
    ]
  }'
```

**Expect**: Two `LARGE_NEW_FILE_INTRODUCED` findings, one per file.

## Scenario 5 — a clean PR produces the same result as before this feature (regression)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{"repositoryName":"agentguard-demo","prNumber":1,"prTitle":"Update README","changedFiles":[]}'
```

**Expect**: `score: 0`, `classification: "LOW"`, `recommendation: "SAFE_TO_REVIEW"`, and now **14** checks (not 13).
