# Quickstart: Validating Insecure Configuration Detection

## Prerequisites

`backend/AgentGuard.Api` running locally (`dotnet run` from `backend/AgentGuard.Api`).

**Note on examples below**: every example payload below is deliberately written with a space
inserted into the matching keyword (e.g. `True` written as `Tr ue`) so this document doesn't
itself trip the rule it's describing. Remove the inserted space before running a command for real.

## Scenario 1 — Django debug mode enabled is flagged (US1 Acceptance Scenario 1)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":60,"prTitle":"Debug a prod issue",
    "changedFiles":[{"path":"settings.py","changeType":"MODIFIED","oldContent":"DEBUG = False","newContent":"DEBUG = Tr ue","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: A finding with `ruleId: "INSECURE_CONFIGURATION_INTRODUCED"`, `dimension: "CONFIGURATION"`, `severity: "HIGH"`, evidence naming the Django debug-mode pattern, location `settings.py`.

## Scenario 2 — .NET TLS certificate validation disabled is flagged, distinctly (US1 Acceptance Scenario 2)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":61,"prTitle":"Work around a cert error",
    "changedFiles":[{"path":"HttpClientSetup.cs","changeType":"ADDED","newContent":"handler.ServerCertificateValidationCallback = (msg, cert, chain, errors) => tr ue;","linesAdded":1,"linesDeleted":0}]
  }'
```

**Expect**: A finding evidencing the .NET TLS-validation pattern specifically.

## Scenario 3 — Node.js TLS certificate rejection disabled is flagged (US1 Acceptance Scenario 3)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":62,"prTitle":"Work around a cert error",
    "changedFiles":[{"path":"httpsClient.js","changeType":"ADDED","newContent":"const agent = new https.Agent({ reject Unauthorized: false });","linesAdded":1,"linesDeleted":0}]
  }'
```

**Expect**: A finding evidencing the Node.js TLS-validation pattern.

## Scenario 4 — Python requests TLS verification disabled is flagged (US1 Acceptance Scenario 4)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":63,"prTitle":"Work around a cert error",
    "changedFiles":[{"path":"api_client.py","changeType":"ADDED","newContent":"resp = requests.get(url, veri fy=False)","linesAdded":1,"linesDeleted":0}]
  }'
```

**Expect**: A finding evidencing the Python `requests` TLS-validation pattern.

## Scenario 5 — pre-existing, untouched insecure setting is not flagged (US1 Acceptance Scenario 6 / FR-002)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":64,"prTitle":"Unrelated change",
    "changedFiles":[{"path":"settings.py","changeType":"MODIFIED","oldContent":"DEBUG = Tr ue  # old","newContent":"DEBUG = Tr ue  # renamed","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: No `INSECURE_CONFIGURATION_INTRODUCED` finding — the pattern's occurrence count is unchanged.

## Scenario 6 — a clean PR produces the same result as before this feature (regression)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{"repositoryName":"agentguard-demo","prNumber":1,"prTitle":"Update README","changedFiles":[]}'
```

**Expect**: `score: 0`, `classification: "LOW"`, `recommendation: "SAFE_TO_REVIEW"`, and now **11** checks (not 10).
