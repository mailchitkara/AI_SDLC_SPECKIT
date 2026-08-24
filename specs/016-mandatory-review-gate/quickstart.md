# Quickstart: Validating the Mandatory Review Gate

## Prerequisites

`backend/AgentGuard.Api` runnable locally (`dotnet run` from `backend/AgentGuard.Api`).

## Scenario 1 — a low-scoring business-critical finding is floored to HUMAN_REVIEW_REQUIRED (US1 Acceptance Scenario 1)

```bash
cat > /tmp/agentguard-policy.json << 'EOF'
{
  "businessCriticalPaths": [{ "pathPattern": "payments/*", "label": "Payment Processing" }],
  "mandatoryReviewDimensions": ["BUSINESS_CRITICALITY"]
}
EOF
AGENTGUARD_POLICY_FILE_PATH=/tmp/agentguard-policy.json dotnet run --project backend/AgentGuard.Api --urls http://localhost:5085 &
sleep 8
curl -s -X POST http://localhost:5085/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":110,"prTitle":"Tiny payments tweak",
    "changedFiles":[{"path":"payments/Gateway.cs","changeType":"MODIFIED","oldContent":"x","newContent":"y","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: `recommendation: "HUMAN_REVIEW_REQUIRED"`, `recommendationForcedByGovernancePolicy: true`, even though the only findings (`BUSINESS_CRITICAL_PATH_TOUCHED` at Medium, `MISSING_RELATED_TESTS` at Medium) would otherwise classify well below that.

## Scenario 2 — an already-BLOCK_MERGE PR is unaffected (US1 Acceptance Scenario 2 / Edge Case 3)

Using the same running instance from Scenario 1. The AWS-key-shaped literal below (the literal prefix
`AKIA` followed by 16 uppercase letters/digits) is deliberately not written out in full — an actual
matching example would trip `SECRET_DETECTED` on this document itself, the same self-reference
problem `005-risk-engine-foundation`'s and `006-security-risk-rules`'s quickstarts already hit once
each. Reassemble the placeholder below into a real matching value before running for real:

```bash
curl -s -X POST http://localhost:5085/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":111,"prTitle":"Leaked key in payments",
    "changedFiles":[{"path":"payments/Gateway.cs","changeType":"MODIFIED","oldContent":"x","newContent":"const key = '\''<AKIA-shaped fixture>'\'';","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: `recommendation: "BLOCK_MERGE"` (reached via score — `SECRET_DETECTED` is `Severity.Blocker`-weighted, not `MandatoryOverride`; see `005-risk-engine-foundation`'s own precedent for this distinction), `recommendationForcedByOverride: false`, `recommendationForcedByGovernancePolicy: false` — this policy did not cause the outcome, the score alone already reached `BLOCK_MERGE`.

## Scenario 3 — no mandatory-review dimensions configured is a byte-for-byte regression match (US1 Acceptance Scenario 3)

```bash
dotnet run --project backend/AgentGuard.Api --urls http://localhost:5086 &
sleep 8
curl -s -X POST http://localhost:5086/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":112,"prTitle":"Tiny payments tweak",
    "changedFiles":[{"path":"payments/Gateway.cs","changeType":"MODIFIED","oldContent":"x","newContent":"y","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: `recommendationForcedByGovernancePolicy: false`, and the recommendation is whatever the score alone would classify (no business-critical config means `BUSINESS_CRITICAL_PATH_TOUCHED` doesn't even fire here — see `013`).

## Scenario 4 — an unrecognized dimension name fails startup loudly

```bash
echo '{ "mandatoryReviewDimensions": ["NOT_A_REAL_DIMENSION"] }' > /tmp/bad-governance-policy.json
AGENTGUARD_POLICY_FILE_PATH=/tmp/bad-governance-policy.json timeout 5 dotnet run --project backend/AgentGuard.Api --urls http://localhost:5087
echo "Exit code: $?"
```

**Expect**: The process exits with a non-zero code and a clear error identifying the unrecognized dimension name.
