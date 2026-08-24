# Feature Specification: Newly Swallowed Exception Detection

**Feature Branch**: `feature/reliability-risk-rules`

**Created**: 2026-08-24

**Status**: Draft

**Input**: User description: "Phase 2 (third increment) of the multi-phase AgentGuard risk-analysis expansion — Core Deterministic Risk Rules. Add a new deterministic Reliability-dimension rule that flags a PR newly introducing a swallowed error/exception: an empty catch block (C#/JavaScript/TypeScript), a bare except block whose body only passes (Python), or an ignored error check (Go's `if err != nil` with an empty body) — a common shortcut where an error is caught but silently discarded rather than handled or the underlying issue fixed. Uses the same count-based old-vs-new diffing shape as the existing rules: a pre-existing, untouched occurrence is never flagged, only a newly introduced one. Scoped narrowly per the phase's own guidance."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Catch a Newly-Introduced Swallowed Error (Priority: P1)

A developer reviewing a PR that newly adds an empty catch block, a no-op except clause, or an ignored Go error check is shown a finding explaining which error-handling construct now silently discards a failure, where, and a reminder to either handle it or at least log/propagate it — without needing to notice the empty block themselves while scanning a diff.

**Why this priority**: This is the entire feature. Silently swallowing an error is a well-documented reliability anti-pattern in both human- and AI-agent-generated code — an agent asked to "make the exception go away" will sometimes wrap the failing call in a catch block and do nothing with it, rather than fix the underlying issue or handle it deliberately. It's the same shape of capability as the existing testing- and security-dimension rules, applied to a different pattern family under the Reliability dimension.

**Independent Test**: Can be fully tested by submitting a PR that newly introduces one of the recognized swallowed-error patterns and verifying a finding is produced with the correct severity, dimension, evidence, and remediation; and by submitting a PR that does not, and verifying no finding is produced.

**Acceptance Scenarios**:

1. **Given** a PR that adds an empty catch block (C# or JavaScript/TypeScript) around a call that can throw, **When** the PR is analyzed, **Then** a finding is produced identifying the pattern, the file and location, and remediation guidance to handle, log, or propagate the error.
2. **Given** a PR that adds a Python except clause whose entire body is just `pass`, **When** the PR is analyzed, **Then** a finding is produced distinct from the catch-block case.
3. **Given** a PR that adds a Go error check (`if err != nil`) with an empty body, **When** the PR is analyzed, **Then** a finding is produced.
4. **Given** a PR that changes code but introduces none of the recognized patterns (e.g. a catch block that logs or rethrows), **When** the PR is analyzed, **Then** no finding from this rule is produced.
5. **Given** a file that already contained one of these patterns before the PR (unchanged by this PR), **When** the PR is analyzed, **Then** no finding is produced for that pre-existing, untouched occurrence — only newly introduced instances are flagged, consistent with the existing rules.

---

### Edge Cases

- What happens when the same pattern appears in multiple newly-changed files in one PR? Each occurrence produces its own independent finding, no deduplication, consistent with every other AgentGuard rule.
- What happens when a swallowed-error pattern is removed (a previously-empty catch block gets real handling) rather than added? No finding — only newly *introduced* occurrences are in scope.
- What happens when a file cannot be parsed as source code at all (binary content, or content AgentGuard couldn't retrieve)? No finding for that file from this rule.
- What happens when a catch block or except clause's body contains only a comment, no other code? Not flagged by this increment — this rule matches a body containing only whitespace, the same accepted trade-off the existing pattern-based rules already have (distinguishing "whitespace-only" from "comment-only" would require comment-aware parsing per language, which this deterministic, text-pattern-based rule deliberately does not attempt, per FR-008). A future increment could extend pattern coverage to include this case.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST produce a finding when a changed file newly introduces a recognized swallowed-error pattern, covering at minimum: an empty catch block (C#/JavaScript/TypeScript), a Python except clause whose body is only `pass`, and a Go `if err != nil` check with an empty body.
- **FR-002**: System MUST NOT produce a finding for an occurrence of a recognized pattern that was already present in a file before the PR's changes — only newly introduced occurrences are in scope, matching every existing rule's "newly introduced" semantics.
- **FR-003**: Each finding MUST include which specific pattern matched, the affected file, evidence sufficient to locate the exact construct, and remediation guidance recommending the error be handled, logged, or propagated.
- **FR-004**: This rule MUST be classified under the Reliability risk dimension (established in the risk-engine-foundation phase) and MUST report Deterministic kind and Certain confidence for every finding.
- **FR-005**: System MUST assign this rule High severity, matching the precedent set by the other Phase 2 rules.
- **FR-006**: This rule MUST have a stable, unique rule identifier that does not require modifying any other existing rule's definition.
- **FR-007**: This rule MUST be independently testable in isolation from the other existing rules and from the overall scoring/classification logic.
- **FR-008**: System MUST NOT implement this capability as a general-purpose static analysis or control-flow engine — detection MUST use the same direct, deterministic pattern-matching approach already established by the existing rules, scoped to a fixed, reviewable set of recognized patterns.

### Key Entities

- **Swallowed Exception Finding**: A finding produced by this rule — inherits every field the existing Finding model already requires, with no new fields introduced by this feature.
- **Recognized Swallowed-Error Pattern**: One entry in the fixed set of patterns this rule matches against (e.g. "empty catch block," "bare except with pass," "ignored Go error check").

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of PRs that newly introduce a recognized swallowed-error pattern produce a finding identifying the specific pattern, file, and remediation.
- **SC-002**: 100% of PRs that only contain pre-existing (untouched) occurrences of a recognized pattern produce zero findings from this rule.
- **SC-003**: This rule's detection logic can be tested in complete isolation from the other existing rules and the overall risk-scoring pipeline.
- **SC-004**: Adding this rule changes zero existing test expectations for the seven previously-shipped rules — a PR containing no swallowed-error pattern produces byte-for-byte the same result as it would have before this feature.

## Assumptions

- This rule covers a fixed, deliberately narrow set of recognized patterns for this increment (C#/JS/TS empty catch, Python bare-except-pass, Go ignored-error-check) — expanding coverage further, or adding the remaining Phase 2 risk areas (generated-file contamination, TODO/stub detection, configuration risk, a dependency-scanning adapter), are separate, later increments.
- No new Finding fields, API contract changes, or UI changes are needed — output flows through the exact response shape already established.
- Judging whether a given swallow is *justified* (e.g. by an explanatory comment) is explicitly out of scope — this rule surfaces every newly-introduced occurrence for human review rather than attempting that judgment itself.
