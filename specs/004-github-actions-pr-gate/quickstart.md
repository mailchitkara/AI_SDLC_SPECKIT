# Quickstart: Validating the GitHub Actions PR Gate

Proves the feature end-to-end once implemented. Assumes `003-github-pr-import`'s analyze-by-reference endpoint (see `contracts/analyze-by-reference.md`) is deployed and reachable at `api-url`.

## Prerequisites

- This repository, with `.github/actions/agentguard-pr-gate/action.yml` implemented.
- An AgentGuard.Api instance reachable at a known URL, exposing the endpoint from `contracts/analyze-by-reference.md`.
- A test workflow wired to run on `pull_request` (see `.github/workflows/agentguard-gate-self-test.yml`).

## Scenario 1 — clean PR passes (US1 + US2 default policy)

1. Open a PR in the test repository that changes only, e.g., a README line (mirrors the "clean PR" fixture used in `003`'s own testing).
2. Confirm the gate step's workflow run: step succeeds, `pass` output is `true`.
3. Confirm a Check Run named `AgentGuard PR Risk Gate` appears on the PR with `conclusion: success` and a summary showing `LOW`/`SAFE_TO_REVIEW`.

**Validates**: FR-001, FR-002, FR-004, FR-006, SC-001, SC-003.

## Scenario 2 — CRITICAL PR blocks (US2 non-default + default policy)

1. Open a PR that introduces a hardcoded secret (same fixture pattern as `001-pr-risk-analysis-v1`'s `SECRET_DETECTED` example).
2. Confirm the gate step fails (default `block-on: CRITICAL` catches this).
3. Confirm the Check Run shows `conclusion: failure` with the score, `CRITICAL` classification, `BLOCK_MERGE` recommendation, and finding severity counts in its summary.
4. If a branch protection rule requires this check, confirm the PR's merge button is disabled.

**Validates**: FR-003, FR-005, FR-006, SC-003, SC-004.

## Scenario 3 — same PR re-run is deterministic (US1 Acceptance Scenario 3)

1. Re-run the gate step from Scenario 1 or 2 without pushing new commits.
2. Confirm `score`, `classification`, `recommendation`, and `pass` are identical to the first run.

**Validates**: FR-001 determinism note, SC-002.

## Scenario 4 — analysis unavailable, fail-open default (Edge Case 1, FR-009)

1. Point `api-url` at an unreachable/invalid host for one run (or otherwise force a timeout/rate-limit condition).
2. Confirm the step still succeeds (`pass: true`) by default.
3. Confirm the Check Run shows `conclusion: neutral` (not `success`) with a summary explaining the gate could not complete and why.

**Validates**: FR-008, FR-009 (default), SC-005.

## Scenario 5 — analysis unavailable, fail-closed configured (FR-004a)

1. Repeat Scenario 4 with `fail-on-unavailable: true` set on the step.
2. Confirm the step now fails (`pass: false`).

**Validates**: FR-004a, FR-009 (configured).

## Scenario 6 — updated PR supersedes the prior result (FR-007, US3 Acceptance Scenario 3)

1. After Scenario 2's failing result is published, push a new commit to the same PR that removes the secret.
2. Re-run the gate step.
3. Confirm the same Check Run (`AgentGuard PR Risk Gate`) now shows `conclusion: success` — updated in place, not a second separate check.

**Validates**: FR-007, US3 Acceptance Scenario 3.

## Scenario 7 — forked-repo PR degrades gracefully (Edge Case 2)

1. Open a PR from a fork of the test repository (ambient token has reduced permissions under the default `pull_request` event).
2. Confirm the gate step still completes and resolves a `pass`/`fail` decision.
3. Confirm the published result mechanism falls back to a PR comment (`mechanism: pr_comment`) if the Check Run write is rejected, rather than the whole workflow step erroring out.

**Validates**: forked-PR degrade-gracefully assumption, FR-006 (best-effort).
