# Contract: Analyze-by-Reference Endpoint (reconciled to `003-github-pr-import`)

`004-github-actions-pr-gate` depends on this endpoint but does not own it — it is delivered by `003-github-pr-import`. This file previously forward-declared an expected shape; `003` has since been planned, so this is now reconciled to its authoritative contract at [`../../003-github-pr-import/contracts/pr-reference-analysis-endpoint.md`](../../003-github-pr-import/contracts/pr-reference-analysis-endpoint.md). Read that file for the full request/response/error contract. This file records only the two things `003`'s planning changed from what `004` originally assumed, and how `004`'s own `Gate Outcome` mapping (see `../data-model.md`) adapts to the real contract.

## What changed from the original forward-declaration

1. **Request field name**: `003` uses `prUrl` (or the `owner`/`repository`/`prNumber` trio), not a generic `prReference` string. `004`'s `analyze.sh` always has a full PR URL available from Actions context (`github.repository` + the event's PR number), so it uses the `prUrl` form exclusively — the trio form exists in `003`'s contract for other callers, not needed here.
2. **Error representation**: `003` uses HTTP status codes (`400`/`404`/`429`) with a small typed error body, not a `200` envelope with an `"outcome"` discriminator as originally assumed. `analyze.sh` branches on HTTP status, not a body field.

## `004`'s `Gate Outcome` mapping, updated for the real contract

| `003` HTTP response | `004` `Gate Outcome.status` | `004` `unavailable_reason` |
|---|---|---|
| `200 OK` | `completed` | — |
| `429 Too Many Requests` (`rate_limited`) | `unavailable` | `rate_limited` |
| `404 Not Found` (`not_found_or_no_access`) | `unavailable` | `unreachable` |
| `400 Bad Request` (`invalid_reference`) | `unavailable` | `unreachable` (should not occur in practice, since `004` constructs `prUrl` itself from valid Actions context — a defensive case, not an expected path) |
| No response within `timeout-seconds` | `unavailable` | `timed_out` |

`004` does not implement `003`'s credential-retry flow (`003` US3) — a single request is made per gate run, using the ambient `GITHUB_TOKEN` once via the `credential` field. If that lacks access (`404`), the run resolves as `unavailable` under the Gate Policy rather than prompting interactively (there is no interactive user to prompt inside a CI run).

`003`'s `200` response's new `partiallyEvaluatedFiles` field is surfaced in `004`'s Check Run summary as an additional note (not modeled as a separate `Gate Outcome` field — see `../data-model.md`'s `finding_summary`, which this appends to when non-empty).
