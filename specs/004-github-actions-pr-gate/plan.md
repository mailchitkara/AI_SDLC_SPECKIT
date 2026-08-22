# Implementation Plan: GitHub Actions PR Gate

**Branch**: `004-github-actions-pr-gate` | **Date**: 2026-08-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-github-actions-pr-gate/spec.md`

## Summary

Ship a reusable GitHub composite Action that a workflow can drop into a `pull_request` job with no manual input. The action resolves the triggering PR's context from the Actions environment, calls AgentGuard's PR-by-reference analysis capability (the "GitHub PR Import" feature, `003-github-pr-import`) over HTTP, applies a per-repository Gate Policy (blocking threshold + fail-open/closed) to derive a pass/fail decision, publishes a GitHub Check Run with the score/classification/recommendation/finding summary on the PR, and exits with a status that fails the workflow step when the policy says to block.

## Technical Context

**Language/Version**: POSIX shell (bash) steps inside a GitHub composite action — matches the shell already used by `.github/workflows/ci.yml`'s `ubuntu-latest` jobs; no new language runtime introduced.

**Primary Dependencies**: `curl` (call the AgentGuard.Api analyze-by-reference endpoint from `003-github-pr-import`), `jq` (parse the JSON response and build the Check Run payload), GitHub CLI `gh` (create/update the Check Run and, as a forked-PR fallback, a PR comment) — all three are preinstalled on GitHub-hosted `ubuntu-latest` runners, so the action has zero install step.

**Storage**: N/A — stateless per run; no new persistence, consistent with AgentGuard's existing no-database constraint.

**Testing**: A self-test workflow (`.github/workflows/agentguard-gate-self-test.yml`) in this repo that runs the action against fixture `pull_request` events (a clean PR, a PR trippable to BLOCK_MERGE, a PR against an inaccessible repo) and asserts the resulting step outcome and Check Run content; shell assertions via `jq`, no new test framework.

**Target Platform**: GitHub-hosted Actions runners, `ubuntu-latest` primary target (bash/curl/jq/gh all present by default); documented as likely compatible with other GitHub-hosted OSes since all three dependencies ship cross-platform, but only `ubuntu-latest` is validated by the self-test workflow for V1.

**Project Type**: GitHub composite Action (new artifact category in this repo, alongside the existing `backend/` and `frontend/` projects) plus its usage documentation.

**Performance Goals**: Analysis call budget of 60 seconds by default (configurable action input `timeout-seconds`), chosen to comfortably cover `003-github-pr-import`'s own SC-002 target (complete analysis under 15s for a typical PR) plus GitHub API round-trips, while still failing fast within a CI job rather than hanging.

**Constraints**: Must function using only the default `GITHUB_TOKEN` permissions on same-repo PRs (`pull-requests: read`, `checks: write`); must not hard-fail the whole workflow when a forked-PR's reduced token can't write a Check Run (FR degrade-gracefully edge case) — falls back to a PR comment via `issue_comment`-equivalent `gh pr comment`, which fork PRs typically retain read/comment ability for depending on the triggering event; must not require Docker (composite action, no container build/pull step).

**Scale/Scope**: One action definition (`action.yml` + shell scripts) + one example/self-test workflow + one usage doc. Gate Policy is configured via action inputs (blocking threshold list, fail-open/closed flag) set directly in the consuming workflow's YAML — no separate config file format to design for V1, keeping the surface area small.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution's principles are scoped to AgentGuard's UI (React/TypeScript/Vite) and the UI → API → Core separation of concerns. This feature does not touch the UI, does not implement or duplicate risk-analysis business rules, and does not require any change to AgentGuard.Core:

- **Separation of Concerns**: The action is a thin client — it calls the existing (003-introduced) HTTP contract for analysis and never computes scores, classifications, or findings itself. This directly follows the constitution's "React frontend MUST NOT implement risk-analysis business rules" principle, applied here to a second, non-UI client of the same API.
- **UI Contract**: N/A — this feature adds no UI surface. `docs/deployment.md`-style documentation is the closest existing precedent for how this feature's usage doc should read.
- No violations identified. Complexity Tracking table is not needed.

*Re-checked after Phase 1 design below — unchanged: the composite action + docs stay a thin client of the 003 HTTP contract; no new violations introduced by the data model or contracts.*

## Project Structure

### Documentation (this feature)

```text
specs/004-github-actions-pr-gate/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
.github/
├── actions/
│   └── agentguard-pr-gate/
│       ├── action.yml              # composite action: inputs, outputs, step sequence
│       └── scripts/
│           ├── analyze.sh          # resolve PR context, call the 003 analyze-by-reference endpoint
│           ├── apply-policy.sh     # derive pass/fail from Gate Policy inputs + analysis result
│           └── publish-result.sh   # create/update a Check Run (or fall back to a PR comment)
└── workflows/
    └── agentguard-gate-self-test.yml   # exercises the action against fixture PRs in this repo's own CI

docs/
└── github-actions-gate.md          # usage doc: adding the action to a workflow, Gate Policy inputs reference
```

**Structure Decision**: Follows this repo's existing "web application" layout (`backend/`, `frontend/`) by adding a third, independent artifact category under `.github/actions/` — the conventional location GitHub itself expects for a repository-local composite action — rather than a top-level `action/` folder that would imply a standalone publishable package. Usage documentation follows the precedent already set by `docs/deployment.md`.

## Complexity Tracking

*No Constitution Check violations — this section is intentionally empty.*
