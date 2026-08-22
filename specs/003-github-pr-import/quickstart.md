# Quickstart: Validating GitHub PR Import

Proves the feature end-to-end once implemented, against the real GitHub API and a locally-running (or deployed) `agentguard-api`.

## Prerequisites

- `backend/AgentGuard.Api` running locally (`dotnet run` from `backend/AgentGuard.Api`) or the deployed instance.
- `curl` and (optionally) a GitHub PAT with `Contents: Read` + `Pull requests: Read` for the credential-retry scenarios.

## Scenario 1 — analyze a real public PR by URL (US1)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis/from-reference \
  -H "Content-Type: application/json" \
  -d '{"prUrl": "https://github.com/chalk/chalk/pull/688"}'
```

**Expect**: `200 OK`, `score: 0`, `classification: LOW`, `recommendation: SAFE_TO_REVIEW`, `partiallyEvaluatedFiles: []` — matches the result already confirmed manually for this PR during ad hoc testing.

**Validates**: FR-001, FR-002, FR-004, FR-005, SC-001.

## Scenario 2 — same PR by owner/repo/number form (US1, contract equivalence)

```bash
curl -s -X POST http://localhost:5080/api/pr-risk-analysis/from-reference \
  -H "Content-Type: application/json" \
  -d '{"owner": "chalk", "repository": "chalk", "prNumber": 688}'
```

**Expect**: Identical response to Scenario 1.

**Validates**: FR-001, contract test "equivalent request forms."

## Scenario 3 — determinism (US1 Acceptance Scenario 3)

Run Scenario 1 twice in a row.

**Expect**: Byte-identical `score`/`classification`/`recommendation`/`findings`.

**Validates**: FR-011, SC-005.

## Scenario 4 — a PR with a file that can't be fully evaluated (US2)

Find (or construct, via a fixture repo) a small PR that adds a binary file (e.g., an image).

**Expect**: `200 OK`, that file's path appears in `partiallyEvaluatedFiles` with `reason: "not_retrievable"`; the rest of the analysis (e.g., `LargeChangeSize` still counting its lines) proceeds normally.

**Validates**: FR-009, SC-003.

## Scenario 5 — invalid reference (US3 Acceptance Scenario 1)

```bash
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5080/api/pr-risk-analysis/from-reference \
  -H "Content-Type: application/json" \
  -d '{"prUrl": "not a url"}'
```

**Expect**: `400`, `errorType: invalid_reference`, `retryableWithCredential: false`.

**Validates**: FR-010.

## Scenario 6 — not-found-or-no-access, then recover with a credential (US3 Acceptance Scenarios 2–3)

```bash
# Attempt 1: a private repo PR, no credential
curl -s -X POST http://localhost:5080/api/pr-risk-analysis/from-reference \
  -H "Content-Type: application/json" \
  -d '{"prUrl": "https://github.com/{your-org}/{private-repo}/pull/1"}'
# Expect: 404, errorType: not_found_or_no_access, retryableWithCredential: true

# Attempt 2: same reference, with a credential that has access
curl -s -X POST http://localhost:5080/api/pr-risk-analysis/from-reference \
  -H "Content-Type: application/json" \
  -d '{"prUrl": "https://github.com/{your-org}/{private-repo}/pull/1", "credential": "'"$GITHUB_TOKEN"'"}'
# Expect: 200, full analysis
```

**Validates**: FR-006, FR-010a, SC-004.

## Scenario 7 — retry with a credential that still lacks access (US3 Acceptance Scenario 4)

Repeat Scenario 6's second call with a token that does **not** have access to that repository.

**Expect**: `404` again, same `not_found_or_no_access` shape — not a different error.

**Validates**: FR-010a's "same outcome again" requirement.

## Scenario 8 — rate limiting

Exhaust GitHub's unauthenticated rate limit (60 requests in an hour from one IP — easiest to trigger by scripting Scenario 1 in a loop without a credential) and issue one more request.

**Expect**: `429`, `errorType: rate_limited`.

**Validates**: FR-010.
