# Quickstart: Validating the Risk Engine Foundation

## Prerequisites

`backend/AgentGuard.Api` running locally (`dotnet run` from `backend/AgentGuard.Api`).

## Scenario 1 — existing behavior is unchanged (FR-013)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{"repositoryName":"agentguard-demo","prNumber":1,"prTitle":"Update README","changedFiles":[]}'
```

**Expect**: `score: 0`, `classification: "LOW"`, `recommendation: "SAFE_TO_REVIEW"` — identical to before this feature — plus `recommendationForcedByOverride: false` and an empty `findings` array (no dimension/confidence to check on an empty result).

## Scenario 2 — a finding now carries dimension/confidence/kind (US1)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":7,"prTitle":"Add debug logging",
    "changedFiles":[{"path":"src/config/aws.ts","changeType":"ADDED","newContent":"const key = '"'"'AKIAABCDEFGHIJKLMNOP'"'"';","linesAdded":1,"linesDeleted":0}]
  }'
```

**Expect**: Same `score: 100` / `CRITICAL` / `BLOCK_MERGE` as before this feature. The `SECRET_DETECTED` finding now additionally shows `"dimension":"SECURITY"`, `"confidence":"CERTAIN"`, `"kind":"DETERMINISTIC"`, `"mandatoryOverride":false`. `recommendationForcedByOverride: false` (this reached BLOCK_MERGE via score, not override — research.md §3).

## Scenario 3 — configurable thresholds change classification (US2)

```bash
# Score 30 normally classifies MEDIUM under the default bands (25-49).
# A custom band where MEDIUM only goes up to 20 should push it to HIGH instead.
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":42,"prTitle":"Refactor pricing engine",
    "changedFiles":[{"path":"src/pricing/PricingEngine.cs","changeType":"MODIFIED","oldContent":"x","newContent":"y","linesAdded":300,"linesDeleted":250}],
    "thresholds": {"lowMax": 10, "mediumMax": 20, "highMax": 74}
  }'
```

**Expect**: `score: 30` (unchanged arithmetic), but `classification: "HIGH"` instead of the default bands' `"MEDIUM"`.

## Scenario 4 — invalid thresholds are rejected (FR-008)

```bash
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{"repositoryName":"a","prNumber":1,"prTitle":"t","changedFiles":[],"thresholds":{"lowMax":50,"mediumMax":20,"highMax":74}}'
```

**Expect**: `400` (bands out of order).

## Scenario 5 — mandatory override forces BLOCK_MERGE (US3)

Not reachable via the two existing endpoints in this phase, since none of the five existing rules sets `MandatoryOverride: true` (research.md §3) — this is validated at the `AgentGuard.Core` unit-test level instead: construct a `Finding` with `Severity: Low` and `MandatoryOverride: true`, run it through `RiskEngine.Evaluate`, and confirm `Recommendation.BlockMerge` and `RecommendationForcedByOverride: true` even though a Low-severity-only score would never reach Critical under any threshold configuration.

## Scenario 6 — frontend renders the new fields

Open the frontend, run Scenario 2's payload through the "Paste JSON" tab, and confirm the findings card shows the dimension and confidence alongside the existing severity badge.
