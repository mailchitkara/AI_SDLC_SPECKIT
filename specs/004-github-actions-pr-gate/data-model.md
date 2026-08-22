# Phase 1 Data Model: GitHub Actions PR Gate

Entities as used by the action's shell steps and its contracts. None of this is persisted — every field lives only for the duration of one workflow step invocation, consistent with the spec's no-new-persistence assumption.

## Gate Policy

The action's own configuration, supplied entirely as `action.yml` inputs on the consuming workflow's step (no separate config file for V1 — see `research.md` §"Scale/Scope").

| Field | Type | Default | Notes |
|---|---|---|---|
| `block-on` | list of `LOW \| MEDIUM \| HIGH \| CRITICAL` | `CRITICAL` | Classifications at or above which the step fails. Matches spec US2: "fail only on BLOCK_MERGE" maps to `block-on: CRITICAL`, since AgentGuard's CRITICAL classification is what always carries the BLOCK_MERGE recommendation (per `001-pr-risk-analysis-v1` FR-017). |
| `fail-on-unavailable` | boolean | `false` (fail-open) | Per FR-004a/FR-009 — `true` makes the step fail when analysis itself cannot complete, instead of warning and succeeding. |
| `timeout-seconds` | integer | `60` | Analysis call budget — see `research.md` §3. |
| `api-url` | string | *(required, no default)* | Base URL of the AgentGuard.Api instance exposing the `003` analyze-by-reference endpoint. Required rather than defaulted, since a consuming repository may run its own AgentGuard deployment rather than a shared one. |
| `github-token` | string | `${{ github.token }}` | Ambient credential passed through to the analyze call and used to publish the Check Run / fallback comment. |
| `check-name` | string | `AgentGuard PR Risk Gate` | Name of the Published Result's Check Run — see `contracts/action-interface.md` for why this exists (discovered live: without it, multiple invocations against the same PR/SHA overwrite each other's real result, per the update-in-place behavior in FR-007). |

## Gate Outcome

The in-memory result of one invocation, produced by `analyze.sh` + `apply-policy.sh` and consumed by `publish-result.sh`.

| Field | Type | Notes |
|---|---|---|
| `status` | `completed \| unavailable` | `unavailable` covers rate-limited, PR-unreachable, and timed-out per the spec's Edge Cases — always reported distinctly from a completed analysis (FR-008). |
| `score` | integer 0–100, present only when `status = completed` | Passed through unchanged from the 003 analysis result. |
| `classification` | `LOW \| MEDIUM \| HIGH \| CRITICAL`, present only when `status = completed` | Passed through unchanged. |
| `recommendation` | `SAFE_TO_REVIEW \| REVIEW_RECOMMENDED \| HUMAN_REVIEW_REQUIRED \| BLOCK_MERGE`, present only when `status = completed` | Passed through unchanged. |
| `finding_summary` | list of `{severity, count}`, present only when `status = completed` | Aggregated from the analysis result's findings for the Check Run summary (US3 Acceptance Scenario 2). |
| `unavailable_reason` | `rate_limited \| unreachable \| timed_out`, present only when `status = unavailable` | Drives the Check Run's summary text when analysis didn't complete. |
| `pass` | boolean | Derived by `apply-policy.sh`: for `status = completed`, `classification` is compared against `block-on`; for `status = unavailable`, the inverse of `fail-on-unavailable`. |

## Published Result

What `publish-result.sh` writes back to GitHub. One row per workflow run; each new run for the same PR head SHA supersedes (updates, not duplicates) the prior Check Run for this check name, per FR-007.

| Field | Type | Notes |
|---|---|---|
| `mechanism` | `check_run \| pr_comment` | `pr_comment` only when the ambient token lacks `checks: write` (forked-PR fallback — see `research.md` §2). |
| `check_name` | string, fixed value `AgentGuard PR Risk Gate` | Stable name so GitHub treats successive runs on the same PR/SHA as updates to one check, and so branch protection can require it by name. |
| `conclusion` | `success \| failure \| neutral` | `neutral` used for a `status = unavailable` outcome that passed under fail-open, so it's visually distinct from a clean `success` (supports FR-008/SC-005: never mistake "gate didn't run" for "gate ran and found nothing"). |
| `summary_markdown` | string | Score/classification/recommendation/finding counts (completed) or the unavailable reason (not completed); this is the text a developer reads without opening logs (US3). |
