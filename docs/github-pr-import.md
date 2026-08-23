# Analyzing a real GitHub PR with AgentGuard

Spec: [specs/003-github-pr-import/spec.md](../specs/003-github-pr-import/spec.md) · Plan: [specs/003-github-pr-import/plan.md](../specs/003-github-pr-import/plan.md) · Contract: [specs/003-github-pr-import/contracts/pr-reference-analysis-endpoint.md](../specs/003-github-pr-import/contracts/pr-reference-analysis-endpoint.md)

## What this is

`POST /api/pr-risk-analysis/from-reference` runs AgentGuard's existing risk analysis (the five deterministic rules — see [specs/001-pr-risk-analysis-v1](../specs/001-pr-risk-analysis-v1/spec.md)) against a **real GitHub pull request**, instead of requiring the caller to manually assemble the changed-files JSON that `/api/pr-risk-analysis` expects. It fetches the PR's metadata and file contents from GitHub itself, then feeds the result through the same unchanged analyzer.

## Request

Either a PR URL:

```bash
curl -X POST https://agentguard-api-ifb3.onrender.com/api/pr-risk-analysis/from-reference \
  -H "Content-Type: application/json" \
  -d '{"prUrl": "https://github.com/{owner}/{repo}/pull/{number}"}'
```

or the owner/repository/PR-number form:

```bash
curl -X POST https://agentguard-api-ifb3.onrender.com/api/pr-risk-analysis/from-reference \
  -H "Content-Type: application/json" \
  -d '{"owner": "{owner}", "repository": "{repo}", "prNumber": 123}'
```

Both are equivalent — use whichever is more convenient for the caller. Provide exactly one form, not both.

### Private repositories / rate limits

Public repositories work with no credential, subject to GitHub's unauthenticated rate limit (60 requests/hour, shared across every caller from the same IP). For a private repository, or to raise that limit, add a `credential`:

```bash
curl -X POST https://agentguard-api-ifb3.onrender.com/api/pr-risk-analysis/from-reference \
  -H "Content-Type: application/json" \
  -d '{"prUrl": "https://github.com/{owner}/{repo}/pull/{number}", "credential": "'"$GITHUB_TOKEN"'"}'
```

A fine-grained GitHub PAT with `Contents: Read` + `Pull requests: Read` on the target repository is enough. The credential is used only for that one request and is never stored or logged.

## Response

On success, the same shape as `/api/pr-risk-analysis` (`score`, `classification`, `recommendation`, `checks`, `findings`), plus `partiallyEvaluatedFiles` — any file GitHub couldn't serve inline (binary, or over ~1MB), listed so a clean result never silently hides an unreadable file.

On failure, an `ImportErrorResponse` with the HTTP status doing the signaling:

| Status | `errorType` | Meaning | Retry with a credential? |
|---|---|---|---|
| `400` | `invalid_reference` | Malformed URL, or neither/both reference forms given | No — fix the request |
| `404` | `not_found_or_no_access` | The PR doesn't exist, **or** it's private and this request can't see it — GitHub itself doesn't distinguish these for an unauthenticated caller, so neither do we | Yes |
| `429` | `rate_limited` | GitHub's rate limit is exhausted (a `Retry-After` header is included when GitHub supplies one) | No — wait, or add a credential for future requests |

### Richer findings and configurable thresholds (005-risk-engine-foundation)

Both this endpoint and `/api/pr-risk-analysis` also accept an optional `thresholds` request field (`{lowMax, mediumMax, highMax}`, defaulting to V1's fixed 24/49/74 bands) and return each finding with a `dimension`, `confidence`, `kind`, and `mandatoryOverride`, plus a top-level `recommendationForcedByOverride`. See [specs/005-risk-engine-foundation/contracts/risk-analysis-response-extensions.md](../specs/005-risk-engine-foundation/contracts/risk-analysis-response-extensions.md) for the full shape.

## Local validation

See [specs/003-github-pr-import/quickstart.md](../specs/003-github-pr-import/quickstart.md) for the full set of runnable scenarios (clean PR, secret-tripping PR, invalid/not-found/rate-limited references, credential retry).
