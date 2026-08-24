# Phase 0 Research: Business-Critical Path Detection

No `[NEEDS CLARIFICATION]` markers. This records the technology/design decisions.

## 1. Configuration shape: reuse `ForbiddenDependencyConfig`'s exact pattern-matching semantics

**Decision**: `BusinessCriticalPathConfig` is a direct structural mirror of `ForbiddenDependencyConfig` — a list of `(PathPattern, Label)` entries, matched via the same "trailing `*` = prefix match, otherwise case-insensitive substring containment" rule `ForbiddenDependency.Matches` already implements, with an `Empty` static default.

**Rationale**: `ArchitectureViolationRule` already proved this exact shape works for a consuming-team-supplied, DI-injected configuration with a safe empty default. Reusing it exactly (rather than inventing glob syntax, regex, or a different matching rule) keeps this new rule reviewable in isolation and avoids introducing a second, subtly-different pattern language into the same codebase.

**Alternatives considered**: A richer glob/regex pattern language — rejected as unnecessary complexity for a first increment; the existing simple semantics already cover the realistic case (a directory prefix like `payments/*`, or a substring like `PaymentGateway`).

## 2. A new `RiskDimension` value, not reuse of an existing one

**Decision**: `RiskDimension.BusinessCriticality`, a 9th value alongside the original eight from `005-risk-engine-foundation`.

**Rationale**: None of the eight existing dimensions represent "this code area matters more to the business" — Architecture is about structural/dependency correctness, ChangeManagement is about the nature of *how* a change was made (size, generated-file edits, TODOs), not *where* it lives. Forcing this into an existing dimension would misrepresent what the finding is actually about to anyone filtering or reasoning by dimension.

**Alternatives considered**: Reusing `RiskDimension.Architecture` (since `ArchitectureViolationRule` is this rule's closest sibling) — rejected; architecture violations are about *how code is structured*, not *how much the business cares about this area*, and conflating the two would make the Architecture dimension less meaningful for both rules.

## 3. No git history, no external data, no LLM

**Decision**: This rule evaluates only the PR's own already-supplied `ChangedFiles` — the exact same input every Phase 2 rule already receives. It adds no new data dependency.

**Rationale**: This is deliberately the narrowest possible first step into Phase 4's broader theme. The phase's harder areas (blast-radius via dependency graph traversal, git-history hotspots, file churn) all require either a new GitHub API integration (commit history beyond a single PR's diff) or genuine inference — larger, riskier increments than the phase's own "prefer smaller, independently reviewable PRs" guidance (carried over from Phase 2) would want as a first step. This increment proves the new dimension and configuration shape work end-to-end before taking on that additional complexity.

**Alternatives considered**: Starting with git-history hotspots (file churn/frequency) — rejected for this increment; it requires a new GitHub API call path (fetching commit history per file) that doesn't exist yet in `IGitHubPullRequestClient`, a meaningfully larger scope than a first Phase 4 increment should take on.

## 4. Severity: Medium

**Decision**: `Severity.Medium`, matching `MissingRelatedTestsRule`'s and `009`/`010`'s calibration.

**Rationale**: Matching a critical-path pattern says nothing about whether the change itself is risky or correct — only that its blast radius, if something does go wrong, is higher. It's a "give this extra attention" signal, not a "this is likely broken" one, so it doesn't warrant `High` the way an active security weakening does.

**Alternatives considered**: `High` — rejected; would conflate "this touched a sensitive area" with "this change is dangerous," which this rule cannot determine from a path match alone.

## 5. No self-tripping-pattern risk for this increment

**Decision**: No proactive obscuring needed in this feature's own docs/tests.

**Rationale**: Unlike every Phase 2 rule, this rule matches configured path strings, not text content scanned from arbitrary source files — and its default configuration is empty, so no pattern exists to accidentally self-match against this feature's own new files even in principle.
