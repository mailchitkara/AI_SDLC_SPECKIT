# Quickstart: AgentGuard V1 - PR Risk Analysis

Validates the feature end-to-end: backend analysis + API contract + UI rendering. See [data-model.md](./data-model.md) for field definitions and [contracts/openapi.yaml](./contracts/openapi.yaml) for the full request/response schema.

## Prerequisites

- .NET 8 SDK
- Node.js 20+ (for the Vite/React frontend)
- No database, no Docker, no external network access required (per FR-018, FR-021)

## 1. Run the backend

```bash
cd backend
dotnet run --project AgentGuard.Api
```

The API listens on its configured local port (e.g. `http://localhost:5080`) with a single endpoint: `POST /api/pr-risk-analysis`.

## 2. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

Open the printed local URL. The one screen (`PrRiskAnalysisPage`) is shown directly — no login (FR-019).

## 3. Validate: a clean PR is SAFE TO REVIEW

Submit a PR with no changed files:

```bash
curl -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName": "agentguard-demo",
    "prNumber": 1,
    "prTitle": "Update README",
    "changedFiles": []
  }'
```

**Expected** (per Edge Cases in spec.md and data-model.md invariants): `score: 0`, `classification: "LOW"`, `recommendation: "SAFE_TO_REVIEW"`, all 5 entries in `checks` have `passed: true`, `findings: []`. Confirms User Story 1 and 3's baseline case.

## 4. Validate: a secret always forces BLOCK MERGE

Submit a PR whose changed content matches a recognized secret pattern (e.g., an AWS-style access key literal in `newContent`) — see research.md §6 for the pattern set used by `SECRET_DETECTED`.

**Expected**: `score: 100`, `classification: "CRITICAL"`, `recommendation: "BLOCK_MERGE"` (FR-014, FR-017, SC-006), and the matching finding's `evidence` field contains a masked value, never the raw secret (FR-010, SC-007) — inspect the raw HTTP response body to confirm the literal secret string does not appear anywhere in it.

## 5. Validate: findings are explorable and filterable in the UI

Submit a PR that trips at least two rules (e.g., over 20 changed files with no test files touched, to trip both `LARGE_CHANGE_SIZE` and `MISSING_RELATED_TESTS`). In the UI:

- Confirm the checks summary shows those two rules failed and the other three passed (User Story 3).
- Confirm each finding lists rule id, rule name, severity, explanation, evidence, remediation, and — for file-scoped findings — a location (User Story 2, FR-008/FR-009).
- Use the severity filter/group control and confirm the list narrows to only the selected severity (User Story 2, FR-025, SC-004).

## 6. Validate: determinism

Submit the exact same request body from step 3 or 4 twice in a row. **Expected**: byte-for-byte identical `score`, `classification`, `recommendation`, and `findings` ordering both times (FR-013, SC-002) — confirms `RiskEngine` is a pure function of its input (research.md §7).

## 7. Automated checks

- `dotnet test backend/AgentGuard.Core.Tests` — one test class per rule (FR-003..FR-007) plus the `RiskEngine` weight/cap/classification/recommendation matrix.
- `dotnet test backend/AgentGuard.Api.Tests` — end-to-end through the real endpoint (`WebApplicationFactory`), including the `400` validation-error case (FR-002).
- `npm test` (in `frontend/`) — component tests for `RiskSummary`, `ChecksSummary`, and `FindingsList` (severity filter behavior).
