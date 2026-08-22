# Feature Specification: GitHub Actions PR Gate

**Feature Branch**: `004-github-actions-pr-gate`

**Created**: 2026-08-22

**Status**: Draft

**Input**: User description: "Add a capability so people can call AgentGuard's PR risk analysis from GitHub Actions, and use the results to gate (block or allow) a pull request's merge, building on the GitHub PR Import capability (spec 003)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automatically Analyze the PR a Workflow Is Running On (Priority: P1)

A GitHub Actions workflow, triggered on a pull request event, invokes AgentGuard for that exact PR without any manual input — using the repository, PR number, and commit context the workflow already has — and receives a risk result.

**Why this priority**: This is the entry point. Without an automated, context-driven invocation, there is nothing for a CI gate to act on; a maintainer would be back to manually pasting a URL, which defeats the point of automation.

**Independent Test**: Can be fully tested by triggering a workflow run against a real pull request and verifying it produces a complete risk result for that exact PR, using only the ambient workflow context — no URL or manual input required.

**Acceptance Scenarios**:

1. **Given** a workflow running on a pull-request-related event, **When** the gate step executes, **Then** it analyzes the exact PR that triggered the workflow using its already-available repository and PR context, without requiring a URL or manual input.
2. **Given** the workflow's own ambient credential has access to the repository, **When** the gate step executes, **Then** no additional credential needs to be configured for that repository to be analyzed.
3. **Given** the same PR with no new commits between two workflow runs, **When** the gate step executes twice, **Then** both runs produce the same risk result.

---

### User Story 2 - Block or Warn Based on a Configurable Policy (Priority: P2)

A repository maintainer configures which risk outcomes fail the workflow (blocking the PR from merging, when paired with a required status check) versus which merely surface a warning, so the gate reflects their team's own risk tolerance rather than one fixed threshold imposed on everyone.

**Why this priority**: Without configurability, every team is forced to accept a single hard-coded threshold, which will be wrong for many teams and lead to the gate being disabled entirely rather than trusted. This is what makes the gate adoptable, not just technically present.

**Independent Test**: Can be fully tested by configuring the gate with an explicit policy (e.g., "fail only on BLOCK_MERGE" vs. "fail on HUMAN_REVIEW_REQUIRED and above") and verifying the workflow step fails only when the analyzed PR's outcome meets or exceeds the configured threshold, and succeeds otherwise.

**Acceptance Scenarios**:

1. **Given** a policy configured to fail only on a BLOCK_MERGE recommendation, **When** the analyzed PR's recommendation is HUMAN_REVIEW_REQUIRED, **Then** the workflow step completes successfully.
2. **Given** a policy configured to fail only on a BLOCK_MERGE recommendation, **When** the analyzed PR's recommendation is BLOCK_MERGE, **Then** the workflow step fails.
3. **Given** no explicit policy is configured, **When** the gate step executes, **Then** a documented default policy is applied consistently.

---

### User Story 3 - See the Risk Result Directly on the PR (Priority: P3)

A developer looking at a pull request can see AgentGuard's risk score, classification, and recommendation directly on the PR — without leaving GitHub or opening the workflow's raw logs.

**Why this priority**: This makes the gate's reasoning visible and actionable exactly where a developer is already looking, rather than leaving an opaque pass/fail buried in a build log. It depends on Stories 1 and 2 already producing a decision, so it is valuable but not the foundation.

**Independent Test**: Can be fully tested by running the gate on a real PR that trips at least one finding, then verifying the PR itself shows an outcome with enough detail (score, classification, and finding summary) to understand the result without opening workflow logs.

**Acceptance Scenarios**:

1. **Given** the gate step completes for a PR, **When** a developer views that PR on GitHub, **Then** a result reflecting the analysis outcome (pass/fail per the configured policy, plus the underlying score and classification) is visible on the PR itself.
2. **Given** the analyzed PR has one or more findings, **When** a developer views that result, **Then** they can see enough summary detail (score, classification, and count/severity of findings) to understand the outcome without opening the workflow run's raw logs.
3. **Given** the gate re-runs after new commits are pushed to the PR, **When** a developer views the PR, **Then** the visible result reflects the latest run, not a stale prior one.

---

### Edge Cases

- What happens when the gate itself cannot complete (e.g., the source PR is unreachable, the source provider is rate-limiting, or evaluation exceeds its time budget)? By default the workflow step succeeds with a visible warning (fail-open), so a transient outage does not block unrelated, otherwise-safe PRs; a repository maintainer may flip this to fail-closed for a given repository via the same Gate Policy configuration used in User Story 2, for teams whose risk tolerance requires blocking merges until the gate can run successfully.
- What happens on a pull request from a forked repository, where the workflow's ambient credential typically has reduced permissions? The gate should still be able to analyze the PR's file contents; if the reduced credential prevents publishing a visible result on the PR, the gate degrades gracefully (e.g., still produces a pass/fail decision for the workflow itself) rather than failing the entire workflow outright.
- What happens when a PR is updated with new commits after the gate already published a result? The next run for the updated commits produces and publishes a fresh result that supersedes the prior one, consistent with how any other required status check behaves.
- What happens when the underlying analysis reports one or more files it could not fully evaluate (per the GitHub PR Import capability)? That limitation is reflected in the published result rather than silently treated as a clean pass.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a way to invoke AgentGuard's PR risk analysis from within a GitHub Actions workflow using only the PR context already available to that workflow run (repository, PR number, commit references) — no manual URL entry required.
- **FR-002**: System MUST use the GitHub Actions workflow's own ambient credential to retrieve PR data for repositories that credential has access to, without requiring a separately configured credential for that case.
- **FR-003**: System MUST allow a repository maintainer to configure, per repository, a policy defining which risk classification(s) or recommendation(s) cause the gate to fail the workflow run, and which are treated as non-blocking.
- **FR-004**: System MUST apply a documented default policy when no explicit policy is configured.
- **FR-004a**: The Gate Policy MUST include whether the gate fails-closed or fails-open when analysis itself cannot complete (per FR-009), defaulting to fail-open when not explicitly configured.
- **FR-005**: System MUST cause the workflow step to fail when the analyzed PR's outcome meets or exceeds the configured (or default) blocking threshold, and to succeed otherwise.
- **FR-006**: System MUST publish the analysis outcome (at minimum: score, classification, recommendation) directly on the pull request, visible without opening the workflow's raw run logs.
- **FR-007**: System MUST ensure a subsequent gate run on updated PR commits produces and publishes a fresh outcome that supersedes any prior published outcome for that PR.
- **FR-008**: System MUST distinguish, in what it reports, between "the PR was analyzed and here is its risk outcome" and "the gate itself could not complete," so a developer cannot mistake an unavailable gate for a clean result.
- **FR-009**: When the gate itself cannot complete, system MUST resolve the workflow step's pass/fail outcome according to the Gate Policy's configured fail-open/fail-closed setting (FR-004a): fail-open (succeed with a visible warning) by default, or fail-closed (fail the run) when so configured for that repository.
- **FR-010**: System MUST complete within a bounded time budget appropriate for a CI workflow step, and MUST fail with a distinct timeout outcome rather than run indefinitely if that budget is exceeded.
- **FR-011**: System MUST NOT require any change to AgentGuard's existing risk rules, scoring model, or classification/recommendation mapping to support this capability.

### Key Entities

- **Gate Policy**: The per-repository configuration defining which risk classifications or recommendations cause the workflow to fail versus merely warn, plus whether the gate fails open or closed when analysis itself cannot complete, plus the documented defaults applied when unconfigured.
- **Gate Outcome**: The result of one gate invocation — either the underlying risk analysis result, or a distinct not-completed reason (rate-limited, timed out, PR unreachable) — together with the pass/fail decision derived by applying the Gate Policy.
- **Published Result**: The representation of a Gate Outcome made visible directly on the pull request, superseded by each later run for the same PR.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A repository maintainer can add automatic PR risk gating to a workflow using only the PR context GitHub Actions already provides, without writing any custom code to fetch PR data manually.
- **SC-002**: 100% of gate runs against an unchanged PR commit produce the same pass/fail decision.
- **SC-003**: A developer can see the gate's score, classification, and recommendation on the PR itself, in the same place they would see any other required check, for 100% of completed gate runs — without opening workflow logs.
- **SC-004**: 100% of PRs whose analysis meets the configured blocking threshold produce a failing check result, so that a required-status-check branch protection rule can rely on it to prevent merge.
- **SC-005**: 100% of gate runs distinguish an "analysis could not complete" outcome from a completed risk result, so a developer never mistakes an unavailable gate for a clean PR.

## Assumptions

- GitHub Actions is the only CI/CD platform in scope for this feature; other CI systems (GitLab CI, Jenkins, CircleCI, etc.) are out of scope.
- This feature builds on the GitHub PR Import capability (see the "GitHub PR Import for AgentGuard" specification) to retrieve PR data; it does not duplicate or replace that retrieval logic.
- Actually enforcing the gate's result as a hard merge requirement (via required status checks / branch protection rules) is configuration the repository maintainer applies in GitHub's own settings; this feature is responsible for producing and publishing an accurate, policy-driven pass/fail outcome, not for configuring branch protection itself.
- The gate policy is configured per repository (e.g., via workflow input or a configuration file in that repository), not centrally managed across multiple repositories, for this version.
- Forked-repository pull requests may run with a reduced ambient credential under GitHub's own security model; this feature degrades gracefully (e.g., still produces a workflow pass/fail decision even if it cannot publish a visible result on the PR) rather than failing the entire workflow outright in that case.
