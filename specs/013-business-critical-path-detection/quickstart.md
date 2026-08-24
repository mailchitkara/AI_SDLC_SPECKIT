# Quickstart: Validating Business-Critical Path Detection

## Prerequisites

`backend/AgentGuard.Api` running locally (`dotnet run` from `backend/AgentGuard.Api`).

**Note**: `AgentGuard.Api`'s dependency-injection registration does not supply a
`BusinessCriticalPathConfig` in this increment (mirroring how `ForbiddenDependencyConfig` also
isn't wired to anything by default) — so every scenario below that expects a finding is validated
at the `AgentGuard.Core` unit-test level (`BusinessCriticalPathRuleTests.cs`), not via a live HTTP
call, since the deployed API has no critical paths configured to match against. Scenario 3 (the
no-config case) is the one behavior directly observable through the live API.

## Scenario 1 — a matching file produces a finding (US1 Acceptance Scenario 1)

Not reachable via the live API in this phase (see note above) — validated directly at the
`AgentGuard.Core` unit-test level: construct a `BusinessCriticalPathConfig` with one pattern,
run a matching `ChangedFile` through `BusinessCriticalPathRule.Evaluate`, and confirm a finding
with the pattern's label in evidence, `dimension: BusinessCriticality`, `severity: Medium`.

## Scenario 2 — a file matching two patterns produces two findings (US1 Acceptance Scenario 2)

Also validated at the unit-test level (as above) — `BusinessCriticalPathRuleTests.Produces_one_finding_per_matched_pattern_when_a_file_matches_multiple_patterns`.

## Scenario 3 — no configuration supplied produces zero findings (US1 Acceptance Scenario 3 / regression)

This one *is* directly observable through the live, deployed API, since it has no critical-path
configuration wired up:

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName":"agentguard-demo","prNumber":80,"prTitle":"Touch a payments file",
    "changedFiles":[{"path":"payments/Gateway.cs","changeType":"MODIFIED","oldContent":"x","newContent":"y","linesAdded":1,"linesDeleted":1}]
  }'
```

**Expect**: No `BUSINESS_CRITICAL_PATH_TOUCHED` finding, regardless of the file's path — confirming the default-empty configuration behaves identically to before this feature existed.

## Scenario 4 — the check appears in `checks`, always, even unconfigured

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{"repositoryName":"agentguard-demo","prNumber":1,"prTitle":"Update README","changedFiles":[]}'
```

**Expect**: `score: 0`, `classification: "LOW"`, `recommendation: "SAFE_TO_REVIEW"`, and now **13** checks (not 12) — `BUSINESS_CRITICAL_PATH_TOUCHED` appears as passed, matching every other rule's check-registration behavior regardless of whether it can currently fire.
