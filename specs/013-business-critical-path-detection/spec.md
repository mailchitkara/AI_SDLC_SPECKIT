# Feature Specification: Business-Critical Path Detection

**Feature Branch**: `feature/business-critical-path-detection`

**Created**: 2026-08-24

**Status**: Draft

**Input**: User description: "Phase 4 (first increment) of the multi-phase AgentGuard risk-analysis expansion — Contextual Risk Intelligence. Add a new deterministic rule that flags when a PR touches a file matching a configured business-critical path (e.g. payment processing, authentication, data deletion) — surfacing that this change lands in a high-stakes area of the codebase, distinct from what the change actually does. Mirrors ArchitectureViolationRule's ForbiddenDependencyConfig shape exactly: an empty default, a consuming team supplies its own list of critical-path patterns and labels, no fabricated or inferred criticality when no config is supplied. Unlike Phase 3, this requires no LLM or semantic reasoning — it's a deterministic, configuration-driven path match, the same shape as every Phase 2 rule, just surfacing a new risk dimension (business criticality) that none of the eight existing dimensions capture."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Flag a Change Landing in a Business-Critical Area (Priority: P1)

A developer reviewing a PR that touches a file the team has configured as business-critical (e.g. anything under `payments/` or `auth/`) is shown a finding identifying which critical area was touched and why it matters — giving the change extra scrutiny regardless of how large or small the diff itself is, since a one-line change to payment logic carries different stakes than a one-line change to a README.

**Why this priority**: This is the entire feature, and the simplest, safest possible entry point into Phase 4's "Contextual Risk Intelligence" theme — it adds context Phase 2's diff-content rules cannot see (*where* a change lives, not just *what* it contains) without requiring any of Phase 4's harder prerequisites (git history access, an LLM, or any external data source). It reuses `ArchitectureViolationRule`'s already-proven configurable-list architecture exactly, so a team not yet ready to configure critical paths gets zero findings and zero behavior change, rather than a guessed or default list.

**Independent Test**: Can be fully tested by supplying a critical-path configuration, submitting a PR that touches a matching file, and verifying a finding is produced identifying the matched pattern and its label; and by submitting the same PR with no configuration supplied, and verifying no finding is produced.

**Acceptance Scenarios**:

1. **Given** a configured critical-path pattern (e.g. `payments/*` labeled "Payment Processing") and a PR that adds or modifies a file under that path, **When** the PR is analyzed, **Then** a finding is produced identifying the matched pattern's label, the affected file, and guidance to give the change extra review scrutiny.
2. **Given** a PR that touches a file matching more than one configured critical-path pattern, **When** the PR is analyzed, **Then** one independent finding is produced per matched pattern, no deduplication, consistent with `ArchitectureViolationRule`'s existing multi-match behavior.
3. **Given** no critical-path configuration is supplied at all (the default, empty configuration), **When** any PR is analyzed, **Then** no finding is produced from this rule regardless of what files the PR touches — this rule MUST NOT guess or infer criticality when it hasn't been told what's critical.
4. **Given** a configured critical-path pattern and a PR that does not touch any matching file, **When** the PR is analyzed, **Then** no finding is produced.

---

### Edge Cases

- What happens when the same file matches the same pattern in a PR with multiple changed files? One finding per matching changed file, consistent with how every other AgentGuard rule handles multiple occurrences.
- What happens when a critical-path pattern is configured but never matches any real file in the repository (a stale or misconfigured pattern)? Out of scope for this rule to detect — it only evaluates whether a given PR's changed files match, not whether the configuration itself is sensible; matches `ArchitectureViolationRule`'s existing scope boundary for its own configuration.
- What happens when a matched file is only being deleted, not modified? Still flagged — removing a file from a business-critical area is itself a change worth extra scrutiny (e.g. deleting a payment-validation file), not a lower-risk event than modifying it.
- What happens when the configuration itself is malformed (e.g. supplied programmatically with an invalid pattern)? Out of scope for this increment — this rule accepts a config object the same way `ArchitectureViolationRule` does, with no additional validation layer of its own; a consuming team is responsible for supplying a well-formed configuration.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST produce a finding when a changed file's path matches a configured business-critical path pattern.
- **FR-002**: System MUST NOT produce any finding from this rule when no business-critical path configuration is supplied — the default configuration MUST be empty, matching `ArchitectureViolationRule`'s `ForbiddenDependencyConfig.Empty` precedent exactly.
- **FR-003**: Each finding MUST include the matched pattern's configured label, the affected file, and remediation-style guidance recommending the change receive additional review scrutiny given the area it touches.
- **FR-004**: System MUST produce one independent finding per matched pattern when a single changed file matches multiple configured patterns — no deduplication.
- **FR-005**: This rule MUST be classified under a new risk dimension representing business criticality, distinct from the eight existing risk dimensions (none of which represent "this code area matters more to the business"), and MUST report Deterministic kind and Certain confidence for every finding — this is a literal configuration match, not an inference.
- **FR-006**: System MUST assign this rule a severity that reflects "give this extra attention" rather than "this is inherently broken" — Medium, since matching a critical path says nothing about whether the change itself is risky, only that its blast radius, if something goes wrong, is higher.
- **FR-007**: This rule MUST have a stable, unique rule identifier that does not require modifying any other existing rule's definition.
- **FR-008**: This rule MUST be independently testable in isolation from the other existing rules and from the overall scoring/classification logic.
- **FR-009**: System MUST NOT fabricate or infer business criticality from a file's name, location, or content when no configuration is supplied — per the governance principle that unavailable organizational context (here: what the business actually considers critical) MUST be represented as unavailable, not guessed.
- **FR-010**: System MUST NOT require any external data source, network call, or LLM to evaluate this rule — matching pattern strings against a PR's own already-supplied changed-file paths is sufficient, keeping this rule's evaluation model identical in shape to `ArchitectureViolationRule`'s.

### Key Entities

- **Business-Critical Path Finding**: A finding produced by this rule — inherits every field the existing Finding model already requires, with no new fields introduced by this feature.
- **Business-Critical Path Pattern**: One configured entry — a path pattern (matched the same way `ForbiddenDependency`'s existing patterns are: a trailing `*` for a prefix match, otherwise case-insensitive substring containment) and a human-readable label describing what the area represents.
- **Business-Critical Path Configuration**: The full, consuming-team-supplied set of patterns — empty by default, structurally identical to `ForbiddenDependencyConfig`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of PRs that touch a file matching a configured critical-path pattern produce a finding identifying the matched label, file, and guidance.
- **SC-002**: 100% of PRs analyzed with no critical-path configuration supplied produce zero findings from this rule — behaviorally and byte-for-byte identical to before this feature existed.
- **SC-003**: This rule's detection logic can be tested in complete isolation from the other existing rules and the overall risk-scoring pipeline.
- **SC-004**: Adding this rule changes zero existing test expectations for the twelve previously-shipped rules — a PR analyzed without a critical-path configuration produces byte-for-byte the same result as it would have before this feature.

## Assumptions

- This is the first, deliberately narrowest possible increment of Phase 4 ("Contextual Risk Intelligence") — the remaining areas (blast-radius/dependency impact, git-history hotspots, file churn, novelty, risk zones combining multiple signals, reviewer recommendations) require either new external data access (git history beyond a single PR's diff) or combine multiple signals, and are separate, later increments.
- No new Finding fields, API contract changes, or UI changes are needed beyond the new risk dimension itself appearing wherever dimensions are already rendered generically (the existing dimension badge already handles any dimension value).
- This rule's configuration (like `ForbiddenDependencyConfig`) is supplied in code/DI, not via the request body or a persisted store — consistent with `ArchitectureViolationRule`'s existing configuration model, and out of scope to change in this increment.
