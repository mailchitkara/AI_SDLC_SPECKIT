# Quickstart: Validating Policy-as-Code Configuration Loading

## Prerequisites

`backend/AgentGuard.Api` runnable locally (`dotnet run` from `backend/AgentGuard.Api`).

## Scenario 1 — a well-formed policy file configures both rules (US1 Acceptance Scenario 1)

```bash
cat > /tmp/agentguard-policy.json << 'EOF'
{
  "forbiddenDependencies": [{ "from": "src/Ui/", "to": "MyApp.Data.*" }],
  "businessCriticalPaths": [{ "pathPattern": "payments/*", "label": "Payment Processing" }]
}
EOF
AGENTGUARD_POLICY_FILE_PATH=/tmp/agentguard-policy.json dotnet run --project backend/AgentGuard.Api --urls http://localhost:5081 &
sleep 8
curl -s -X POST http://localhost:5081/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":100,"prTitle":"Touch payments and add a bad import",
    "changedFiles":[
      {"path":"payments/Gateway.cs","changeType":"MODIFIED","oldContent":"x","newContent":"y","linesAdded":1,"linesDeleted":1},
      {"path":"src/Ui/Component.cs","changeType":"ADDED","newContent":"using MyApp.Data.Repository;\nclass C {}","linesAdded":2,"linesDeleted":0}
    ]
  }'
```

**Expect**: Both `BUSINESS_CRITICAL_PATH_TOUCHED` (evidencing "Payment Processing") and `ARCHITECTURE_VIOLATION` findings appear.

## Scenario 2 — unset environment variable behaves exactly as today (US1 Acceptance Scenario 2 / regression)

```bash
dotnet run --project backend/AgentGuard.Api --urls http://localhost:5082 &
sleep 8
curl -s -X POST http://localhost:5082/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":101,"prTitle":"Touch payments",
    "changedFiles":[{"path":"payments/Gateway.cs","changeType":"MODIFIED","oldContent":"x","newContent":"y","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: No `BUSINESS_CRITICAL_PATH_TOUCHED` finding — identical to every prior test in this repo's history before this feature existed.

## Scenario 3 — a missing file path behaves the same as unset (US1 Acceptance Scenario 3)

```bash
AGENTGUARD_POLICY_FILE_PATH=/tmp/does-not-exist.json dotnet run --project backend/AgentGuard.Api --urls http://localhost:5083 &
sleep 8
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5083/health
```

**Expect**: `200` — the service starts successfully despite the missing file, per FR-003.

## Scenario 4 — a malformed policy file fails startup loudly (US1 Acceptance Scenario 4)

```bash
echo '{ "forbiddenDependencies": "this should be an array, not a string" }' > /tmp/bad-policy.json
AGENTGUARD_POLICY_FILE_PATH=/tmp/bad-policy.json timeout 5 dotnet run --project backend/AgentGuard.Api --urls http://localhost:5084
echo "Exit code: $?"
```

**Expect**: The process exits with a non-zero code and a clear error message identifying the parse failure, rather than starting successfully.
