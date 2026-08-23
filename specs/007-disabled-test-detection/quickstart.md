# Quickstart: Validating Newly Disabled Test Detection

## Prerequisites

`backend/AgentGuard.Api` running locally (`dotnet run` from `backend/AgentGuard.Api`).

**Note on examples below**: every example payload in this document is deliberately written with a
space inserted into the skip construct's name (e.g. `Skip = "..."` written as `Sk ip = "..."`) so
this document doesn't itself trip the rule it's describing — the same self-reference problem this
session's `005` and `006` quickstarts hit with `SECRET_DETECTED` and `OVERLY_PERMISSIVE_ACCESS_CONTROL`
respectively. Remove the inserted space before running a command for real.

## Scenario 1 — a newly-skipped xUnit test is flagged (US1 Acceptance Scenario 1)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":20,"prTitle":"Temporarily disable flaky test",
    "changedFiles":[{"path":"PaymentTests.cs","changeType":"MODIFIED","oldContent":"[Fact]\npublic void Charges_card() { }","newContent":"[Fact(Sk ip = \"flaky\")]\npublic void Charges_card() { }","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: A finding with `ruleId: "DISABLED_TEST_INTRODUCED"`, `dimension: "TESTING"`, `severity: "HIGH"`, evidence naming the xUnit skip-parameter pattern, location `PaymentTests.cs`.

## Scenario 2 — a newly-skipped Jest test is flagged, distinctly (US1 Acceptance Scenario 2)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":21,"prTitle":"Skip broken test",
    "changedFiles":[{"path":"payment.test.js","changeType":"MODIFIED","oldContent":"it(\"charges the card\", () => {});","newContent":"it.sk ip(\"charges the card\", () => {});","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: A finding evidencing the JS/TS test-skip-modifier pattern specifically — distinct evidence text from Scenario 1's.

## Scenario 3 — a newly-skipped pytest test is flagged (US1 Acceptance Scenario 3)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":22,"prTitle":"Skip broken test",
    "changedFiles":[{"path":"test_payment.py","changeType":"MODIFIED","oldContent":"def test_charges_card(): pass","newContent":"@pytest.mark.sk ip\ndef test_charges_card(): pass","linesAdded":1,"linesDeleted":0}]
  }'
```

**Expect**: A finding evidencing the pytest skip-decorator pattern.

## Scenario 4 — a newly-skipped Go test is flagged (US1 Acceptance Scenario 4)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":23,"prTitle":"Skip broken test",
    "changedFiles":[{"path":"payment_test.go","changeType":"MODIFIED","oldContent":"func TestChargesCard(t *testing.T) { }","newContent":"func TestChargesCard(t *testing.T) { t.Sk ip(\"flaky\") }","linesAdded":1,"linesDeleted":0}]
  }'
```

**Expect**: A finding evidencing the Go early-skip-call pattern.

## Scenario 5 — pre-existing, untouched skip marker is not flagged (US1 Acceptance Scenario 6 / FR-002)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":24,"prTitle":"Unrelated change",
    "changedFiles":[{"path":"PaymentTests.cs","changeType":"MODIFIED","oldContent":"[Fact(Sk ip = \"old reason\")]\npublic void Charges_card() { }","newContent":"[Fact(Sk ip = \"updated reason\")]\npublic void Charges_card() { }","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: No `DISABLED_TEST_INTRODUCED` finding — the pattern's occurrence count is unchanged (1 before, 1 after), even though the file itself was touched.

## Scenario 6 — a re-enabled test is not flagged (Edge Case)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":25,"prTitle":"Re-enable fixed test",
    "changedFiles":[{"path":"PaymentTests.cs","changeType":"MODIFIED","oldContent":"[Fact(Sk ip = \"was flaky\")]\npublic void Charges_card() { }","newContent":"[Fact]\npublic void Charges_card() { }","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: No `DISABLED_TEST_INTRODUCED` finding — the skip-marker count decreased, and this rule only flags newly *introduced* occurrences.

## Scenario 7 — a clean PR produces the same result as before this feature (regression)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{"repositoryName":"agentguard-demo","prNumber":1,"prTitle":"Update README","changedFiles":[]}'
```

**Expect**: `score: 0`, `classification: "LOW"`, `recommendation: "SAFE_TO_REVIEW"`, and now **7** checks (not 6) — the new rule appears in `checks` as passed, since it produced no finding.
