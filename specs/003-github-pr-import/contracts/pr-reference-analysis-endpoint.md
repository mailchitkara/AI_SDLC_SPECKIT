# Contract: `POST /api/pr-risk-analysis/from-reference`

This is the authoritative contract for this feature's endpoint. `004-github-actions-pr-gate` depends on it (see that feature's `contracts/analyze-by-reference.md`, which should be read as reconciled to this file, not the other way around).

## Request

```json
{
  "prUrl": "https://github.com/{owner}/{repo}/pull/{number}",
  "credential": "optional GitHub token"
}
```

or, equivalently:

```json
{
  "owner": "{owner}",
  "repository": "{repo}",
  "prNumber": 123,
  "credential": "optional GitHub token"
}
```

Exactly one of `prUrl` or the `owner`+`repository`+`prNumber` trio must be present (see `data-model.md`'s PR Reference validation rule). `credential`, when present, is forwarded to GitHub only and is never echoed back or logged.

## Responses

### `200 OK` — analysis completed

Same shape as the existing `POST /api/pr-risk-analysis` response, plus `partiallyEvaluatedFiles`:

```json
{
  "repositoryName": "chalk",
  "prNumber": 688,
  "prTitle": "Fix: Treat a numeric FORCE_COLOR as an exact level",
  "score": 0,
  "classification": "LOW",
  "recommendation": "SAFE_TO_REVIEW",
  "checks": [ { "ruleId": "...", "ruleName": "...", "passed": true } ],
  "findings": [],
  "partiallyEvaluatedFiles": []
}
```

### `400 Bad Request` — invalid reference

```json
{ "errorType": "invalid_reference", "message": "...", "retryableWithCredential": false }
```

Returned before any GitHub call — malformed URL, or neither/both reference forms supplied.

### `404 Not Found` — not found or no access

```json
{
  "errorType": "not_found_or_no_access",
  "message": "The PR could not be found, or you may not have access to it. If this is a private repository, retry with a credential that has access.",
  "retryableWithCredential": true
}
```

Returned whenever GitHub itself returns `404` for the PR/repo/owner lookup — deliberately ambiguous between "doesn't exist" and "exists but needs access," per spec User Story 3.

### `429 Too Many Requests` — rate-limited

```json
{ "errorType": "rate_limited", "message": "...", "retryableWithCredential": false }
```

Returned when GitHub signals its rate limit is exhausted (`403` with `X-RateLimit-Remaining: 0`, or a genuine `429`). A `Retry-After` response header is included when GitHub itself supplies one.

## Contract test coverage (for `tasks.md` to enumerate)

- Valid `prUrl` → `200`, response shape matches existing `RiskAnalysisResultResponse` + `partiallyEvaluatedFiles`.
- Valid `owner`/`repository`/`prNumber` trio → `200`, identical result to the equivalent `prUrl` call (US1 Acceptance Scenario 1's "equivalent to manual submission" also implies these two request forms are equivalent to each other).
- Both `prUrl` and the trio supplied → `400 invalid_reference`.
- Neither supplied → `400 invalid_reference`.
- Malformed `prUrl` (not a GitHub PR URL) → `400 invalid_reference`.
- PR reference resolves to GitHub `404` → `404 not_found_or_no_access`, `retryableWithCredential: true`.
- Same request retried with a valid `credential` that has access → `200` (US3 Acceptance Scenario 3).
- Same request retried with a `credential` that still lacks access → `404 not_found_or_no_access` again (US3 Acceptance Scenario 4).
- GitHub rate-limit response → `429 rate_limited`.
- PR containing a file GitHub can't serve inline (oversized/binary) → `200`, that file listed in `partiallyEvaluatedFiles`, rest of analysis unaffected (US2).
- Same PR analyzed twice, unchanged → identical `200` response both times (US1 Acceptance Scenario 3 / FR-011).
