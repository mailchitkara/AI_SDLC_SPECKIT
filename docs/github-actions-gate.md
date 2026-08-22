# Gating pull requests with the AgentGuard Actions Gate

Spec: [specs/004-github-actions-pr-gate/spec.md](../specs/004-github-actions-pr-gate/spec.md) · Plan: [specs/004-github-actions-pr-gate/plan.md](../specs/004-github-actions-pr-gate/plan.md) · Contracts: [action-interface.md](../specs/004-github-actions-pr-gate/contracts/action-interface.md), [analyze-by-reference.md](../specs/004-github-actions-pr-gate/contracts/analyze-by-reference.md)

## What this is

A reusable composite GitHub Action, `.github/actions/agentguard-pr-gate`, that automatically analyzes the pull request a workflow is running on — no URL, no manual data entry — via [GitHub PR Import](./github-pr-import.md), applies a configurable risk policy, and publishes the result directly on the PR as a Check Run.

## Adding it to a workflow

```yaml
name: AgentGuard Gate

on:
  pull_request:
    branches: [main]

permissions:
  contents: read
  checks: write        # required to publish the Check Run
  pull-requests: write  # required only for the forked-PR comment fallback

jobs:
  agentguard:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: ./.github/actions/agentguard-pr-gate
        with:
          api-url: https://agentguard-api-ifb3.onrender.com
```

That's the whole integration — the action reads the triggering PR from the workflow's own context.

## Inputs

| Input | Required | Default | Notes |
|---|---|---|---|
| `api-url` | yes | — | Base URL of the AgentGuard.Api instance exposing the [GitHub PR Import](./github-pr-import.md) endpoint. |
| `github-token` | no | `${{ github.token }}` | Used both to fetch the PR (as AgentGuard's optional credential) and to publish the Check Run/comment. |
| `block-on` | no | `CRITICAL` | Comma-separated classifications that fail the step, e.g. `HIGH,CRITICAL`. |
| `fail-on-unavailable` | no | `false` | `true` fails the step when analysis itself can't complete, instead of warning and succeeding (fail-open default — see below). |
| `timeout-seconds` | no | `60` | How long to wait for the analysis call. |

Full reference: [contracts/action-interface.md](../specs/004-github-actions-pr-gate/contracts/action-interface.md).

## Outputs

`status`, `score`, `classification`, `recommendation`, `pass` — usable by a later step, e.g.:

```yaml
      - uses: ./.github/actions/agentguard-pr-gate
        id: gate
        with:
          api-url: https://agentguard-api-ifb3.onrender.com
      - run: echo "Risk was ${{ steps.gate.outputs.classification }}"
```

## Making it a required check

The action itself only produces and publishes a pass/fail outcome — it does not configure branch protection. To actually block merges on it:

1. Merge a workflow using this action at least once, so GitHub has seen a run named **AgentGuard PR Risk Gate**.
2. Repo **Settings → Branches → Branch protection rules** → edit (or add) the rule for your default branch.
3. Enable **Require status checks to pass before merging**, and select **AgentGuard PR Risk Gate** from the list.

## Fail-open vs. fail-closed

If AgentGuard itself can't be reached (an outage, rate limiting, a timeout), the step **succeeds with a warning by default** (`fail-on-unavailable: false`) — a transient AgentGuard issue shouldn't block unrelated, otherwise-safe PRs. Set `fail-on-unavailable: true` for a repository where merges must wait for the gate to actually run successfully.

## Forked pull requests

`pull_request` events from a fork run with a reduced `GITHUB_TOKEN` that typically can't write a Check Run. When that happens, the action falls back to posting the same summary as a PR comment instead, and still resolves the workflow step's own pass/fail correctly — only the *visible* mechanism changes, not the gating behavior.

## Local validation

See [specs/004-github-actions-pr-gate/quickstart.md](../specs/004-github-actions-pr-gate/quickstart.md) for the full set of scenarios this was validated against, and `.github/workflows/agentguard-gate-self-test.yml` for the automated self-test that runs on every PR to this repo.

## Scenarios not automated in the self-test workflow

A few scenarios need infrastructure the self-test workflow doesn't have (a dedicated fixture PR, or a real external fork) and are verified manually instead:

- **A PR that actually trips `CRITICAL`/`HIGH`, to confirm `block-on` fails the step**: the self-test workflow's own PRs aren't guaranteed to trip any particular classification. To check by hand: open a throwaway PR containing a string matching AWS's access-key-ID shape (the literal prefix `AKIA` followed by 16 uppercase letters/digits — deliberately not written out in full here, since an actual matching example would itself trip AgentGuard's `SECRET_DETECTED` rule on *this* document, which is exactly what happened the first time this line was drafted), run the action against it, and confirm the step fails with `block-on: CRITICAL` (the default) and succeeds with `block-on: LOW` (nothing at that threshold or below should ever block, since the fixture always resolves to `CRITICAL`).
- **The forked-PR comment fallback**: needs a `pull_request` event from an actual external fork, which a same-repo self-test workflow can't generate on demand. To check by hand: have an external contributor open a PR from their fork against a repo running this action, and confirm a PR comment (not a Check Run) appears when the workflow's `GITHUB_TOKEN` lacks `checks: write` for that event.
