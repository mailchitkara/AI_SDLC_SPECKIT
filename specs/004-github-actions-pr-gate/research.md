# Phase 0 Research: GitHub Actions PR Gate

No `[NEEDS CLARIFICATION]` markers remain in the Technical Context — the open product-level question (fail-open vs. fail-closed default) was already resolved in the spec itself (FR-004a/FR-009). This file records the technology-choice research behind that Technical Context.

## 1. Action implementation type: composite vs. JavaScript vs. Docker

**Decision**: GitHub composite action (`using: "composite"` in `action.yml`, plain shell steps).

**Rationale**: The action only needs to call an HTTP endpoint, parse JSON, and call the GitHub API — no logic complex enough to need a real language runtime. `bash`, `curl`, `jq`, and `gh` are all preinstalled on GitHub-hosted runners, so a composite action needs zero setup/build step and stays consistent with this repo's own CI (`ci.yml` already runs plain shell steps on `ubuntu-latest`). It also avoids standing up a fourth build/publish pipeline (a JS action needs `npm run build` + committed `dist/`; a Docker action needs an image build/pull on every run) for a genuinely thin client.

**Alternatives considered**:
- *JavaScript/TypeScript action* (`@actions/core`, `@actions/github`) — better DX for anything with real branching logic or that the team wants to publish to the Marketplace with strong typing, but adds an npm build/publish step for what is currently three linear HTTP calls. Reasonable to revisit if the gate grows materially more complex.
- *Docker action* — most isolated, but adds container build/pull latency to every PR's CI run and reintroduces a Docker dependency that AgentGuard's own V1 spec explicitly avoided for the core analysis (FR-021 in `001-pr-risk-analysis-v1`). Rejected for that consistency reason as much as for simplicity.

## 2. How the result gets published on the PR

**Decision**: A GitHub **Check Run** (Checks API, `gh api repos/{owner}/{repo}/check-runs`), with `conclusion` set from the Gate Policy decision and a markdown `output.summary` containing score, classification, recommendation, and the top findings. Falls back to a `gh pr comment` when the ambient token lacks `checks: write` (the forked-PR case from the spec's edge cases).

**Rationale**: Check Runs are the modern mechanism branch protection's "Require status checks to pass" expects, support a rich markdown summary (needed for FR-006's "score, classification, recommendation" and US3's "enough detail... without opening logs"), and are what a required-status-check reviewer expects to see next to other CI checks (build, test) rather than buried in a comment thread. A plain PR comment is kept only as the degrade-gracefully fallback, not the primary mechanism, since comments cannot be made "required" by branch protection.

**Alternatives considered**:
- *Commit Status API* (`gh api repos/{owner}/{repo}/statuses/{sha}`) — simpler, also usable as a required check, but limited to a short plain-text description (no markdown summary), which would fail US3's "see findings without opening logs" requirement on anything but the simplest PRs.
- *PR comment only* — visible and rich, but not natively wireable into branch protection's required-checks list, so it can't satisfy SC-004 (a required check blocking merge) on its own.

## 3. Analysis call timeout budget

**Decision**: 60-second default, exposed as a configurable `timeout-seconds` action input.

**Rationale**: `003-github-pr-import`'s own SC-002 target is a complete analysis under 15 seconds for a typical PR (under 50 files); 60 seconds leaves generous headroom for GitHub API round-trips and larger PRs while still failing fast enough that a CI job doesn't stall a developer's feedback loop. Making it configurable lets a maintainer raise it for repositories with routinely larger PRs without a code change.

**Alternatives considered**: A fixed, non-configurable timeout was rejected — different repositories have different "typical" PR sizes, and FR-010 only requires *a* bounded budget with a distinct timeout outcome, not one universal number.

## 4. Dependency on `003-github-pr-import`

**Decision**: This action calls `003`'s analyze-by-reference HTTP endpoint as a black box; it does not re-implement GitHub file retrieval. Since `003` has not yet been planned/implemented, this plan documents the *expected* request/response shape it depends on (see `contracts/analyze-by-reference.md`) as a forward contract, to be reconciled with `003`'s own contract when that feature is planned.

**Rationale**: Matches the spec's explicit Assumption ("does not duplicate or replace that retrieval logic") and keeps this feature's scope to CI orchestration, not PR-data-fetching — the two features stay independently testable per the spec-kit user-story model.

**Alternatives considered**: Having the action call the GitHub API directly and duplicate 003's fetch logic in shell was rejected — it would duplicate the not-fully-evaluated-file handling, credential handling, and error-shape logic that 003 already has to define carefully (see 003's spec edge cases), doubling the maintenance surface for behavior that should be identical either way.
