# Feature Specification: Newly Disabled Test Detection

**Feature Branch**: `feature/testing-risk-rules`

**Created**: 2026-08-24

**Status**: Draft

**Input**: User description: "Phase 2 (second increment) of the multi-phase AgentGuard risk-analysis expansion — Core Deterministic Risk Rules. Add a new deterministic Testing-dimension rule that flags a PR newly marking a test as skipped/ignored (an xUnit skip parameter, a Jest/Mocha skip modifier or skip-prefixed test function, a pytest skip marker, a Go test's early-skip call) — a common shortcut where a failing test is disabled rather than the underlying issue being fixed — rather than a general test-coverage or test-quality analyzer. Uses the same count-based old-vs-new diffing shape as the existing OVERLY_PERMISSIVE_ACCESS_CONTROL and SECRET_DETECTED rules: a pre-existing, untouched skip marker is never flagged, only a newly introduced one. Scoped narrowly per the phase's own guidance to prefer smaller, independently reviewable increments."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Catch a Newly-Disabled Test (Priority: P1)

A developer reviewing a PR that newly marks an existing or new test as skipped is shown a finding explaining which test was disabled, where, and a reminder to either fix the underlying failure or provide a documented reason for skipping it — without needing to notice the skip marker themselves while scanning a diff.

**Why this priority**: This is the entire feature. Silently skipping a failing test is a well-documented shortcut in both human- and AI-agent-generated code — an agent asked to "make the build pass" will sometimes disable the test that's failing rather than fix the code or the test itself, which quietly erodes the safety net the test suite is supposed to provide. It's exactly the kind of objectively-detectable pattern AgentGuard's existing deterministic, evidence-based approach is suited to — the same shape of capability as the existing overly-permissive-access-control rule, applied to a different pattern family under the Testing dimension.

**Independent Test**: Can be fully tested by submitting a PR that newly introduces one of the recognized test-skip patterns and verifying a finding is produced with the correct severity, dimension, evidence, and remediation; and by submitting a PR that does not, and verifying no finding is produced.

**Acceptance Scenarios**:

1. **Given** a PR that adds a skip marker to an existing xUnit test method (the `Skip` parameter on a test attribute), **When** the PR is analyzed, **Then** a finding is produced identifying the specific pattern matched, the file and location, and remediation guidance to fix the underlying issue or document why the test is skipped.
2. **Given** a PR that adds a JavaScript/TypeScript test-runner skip call (e.g. a Jest/Mocha `.skip()` modifier, or an `xit`/`xdescribe` block) around a test, **When** the PR is analyzed, **Then** a finding is produced distinct from the xUnit case, with evidence naming the specific construct.
3. **Given** a PR that adds a pytest skip marker (a decorator that unconditionally or conditionally skips a test) to a Python test function, **When** the PR is analyzed, **Then** a finding is produced.
4. **Given** a PR that adds a Go test's early-skip call at the top of a test function body, **When** the PR is analyzed, **Then** a finding is produced.
5. **Given** a PR that changes test code but introduces none of the recognized skip patterns, **When** the PR is analyzed, **Then** no finding from this rule is produced.
6. **Given** a file that already contained one of these skip markers before the PR (unchanged by this PR), **When** the PR is analyzed, **Then** no finding is produced for that pre-existing, untouched marker — only newly introduced instances are flagged, consistent with how the existing secret-detection and overly-permissive-access-control rules already behave.

---

### Edge Cases

- What happens when the same skip pattern appears in multiple newly-changed test files in one PR? Each occurrence produces its own independent finding, consistent with how every other AgentGuard rule already handles multiple occurrences (no deduplication or merging across findings).
- What happens when a skip marker is removed (a previously-skipped test is re-enabled) rather than added? No finding — only newly *introduced* skip markers are in scope, mirroring the existing secret-detection and overly-permissive-access-control rules' "newly introduced" semantics; re-enabling a test is a positive change and must never be flagged.
- What happens when a file cannot be parsed as source code at all (e.g., binary content, or content AgentGuard couldn't retrieve)? No finding for that file from this rule — consistent with how other content-scanning rules already skip content they cannot read, rather than erroring the whole analysis.
- What happens when a skip marker appears in a non-test file (e.g. a helper or comment that happens to contain matching text)? Out of scope for this deterministic, text-pattern-based rule to distinguish by file role — the same accepted limitation the existing pattern-based rules already have (a known, documented trade-off of staying pattern-based rather than building a full parser per language and test framework).
- What happens when a test is skipped with a documented reason string (e.g. `Skip = "tracked in ISSUE-123"`)? Still flagged — this rule does not attempt to judge whether a given reason is acceptable; it surfaces every newly-introduced skip for human review, since that judgment call belongs to the reviewer, not to a deterministic pattern match.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST produce a finding when a changed file newly introduces a recognized test-skip/ignore pattern, covering at minimum: an xUnit test-attribute skip parameter, a JavaScript/TypeScript test-runner skip modifier or skip-prefixed test/suite function, a pytest skip decorator, and a Go test's early-skip call.
- **FR-002**: System MUST NOT produce a finding for an occurrence of a recognized skip pattern that was already present in a file before the PR's changes — only newly introduced occurrences are in scope, matching the existing secret-detection and overly-permissive-access-control rules' established "newly introduced" semantics.
- **FR-003**: Each finding MUST include which specific pattern matched, the affected file, evidence sufficient to locate the exact construct without needing to re-read the whole file, and remediation guidance recommending either fixing the underlying issue or documenting the reason the test is skipped.
- **FR-004**: This rule MUST be classified under the Testing risk dimension (established in the risk-engine-foundation phase) and MUST report Deterministic kind and Certain confidence for every finding, consistent with every other rule shipped so far.
- **FR-005**: System MUST assign this rule a severity that reflects a serious but not always-instantly-catastrophic issue (distinct from the Blocker severity reserved for exposed secrets) — High severity, matching the precedent set by the overly-permissive-access-control rule.
- **FR-006**: This rule MUST have a stable, unique rule identifier that does not require modifying any other existing rule's definition, per the risk-engine-foundation phase's existing rule-identity model.
- **FR-007**: This rule MUST be independently testable — its detection logic MUST be verifiable in isolation from the other existing rules, and from the overall scoring/classification logic.
- **FR-008**: System MUST NOT implement this capability as a general-purpose test-coverage or test-quality analyzer — detection MUST use the same direct, deterministic pattern-matching approach already established by the secret-detection and overly-permissive-access-control rules, scoped to a fixed, reviewable set of recognized skip patterns rather than open-ended test analysis.

### Key Entities

- **Disabled Test Finding**: A finding produced by this rule — inherits every field the existing Finding model already requires (rule id, name, severity, explanation, evidence, location, remediation, risk dimension, confidence, kind), with no new fields introduced by this feature.
- **Recognized Skip Pattern**: One entry in the fixed set of patterns this rule matches against (e.g. "xUnit Skip parameter," "Jest/Mocha skip modifier," "pytest skip decorator," "Go early-skip call") — analogous to the existing overly-permissive-access-control rule's fixed set of recognized permissive patterns.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of PRs that newly introduce a recognized test-skip pattern produce a finding identifying the specific pattern, file, and remediation — verifiable without reading the PR's raw diff.
- **SC-002**: 100% of PRs that only contain pre-existing (untouched) occurrences of a recognized skip pattern produce zero findings from this rule.
- **SC-003**: This rule's detection logic can be tested in complete isolation — 100% of its test cases exercise the rule directly, without depending on the other existing rules or the overall risk-scoring pipeline.
- **SC-004**: Adding this rule changes zero existing test expectations for the six previously-shipped rules or the risk-engine-foundation behavior — a PR containing no test-skip pattern produces byte-for-byte the same result as it would have before this feature.

## Assumptions

- This rule covers a fixed, deliberately narrow set of recognized skip patterns for this increment (xUnit, Jest/Mocha, pytest, Go) — expanding pattern coverage further, or adding the remaining Phase 2 risk areas (additional architecture rules, dependency scanning adapters, reliability, generated-file contamination, TODO/stub detection, configuration risk), are separate, later increments, per the phase's own preference for smaller independently-reviewable PRs over one large batch.
- No new Finding fields, API contract changes, or UI changes are needed — this rule's output flows through the exact same result shape the risk-engine-foundation phase already established, so it appears in existing UI displays and API responses automatically.
- Judging whether a given skip is *justified* (e.g. by evaluating a documented reason string, or checking for a linked tracking issue) is explicitly out of scope for this deterministic rule — it surfaces every newly-introduced skip for human review rather than attempting that judgment itself.
