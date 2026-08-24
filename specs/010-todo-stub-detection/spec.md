# Feature Specification: Newly Introduced TODO/Stub Detection

**Feature Branch**: `feature/todo-stub-detection`

**Created**: 2026-08-24

**Status**: Draft

**Input**: User description: "Phase 2 (fifth increment) of the multi-phase AgentGuard risk-analysis expansion — Core Deterministic Risk Rules. Add a new deterministic ChangeManagement-dimension rule that flags a PR newly introducing a TODO/FIXME/HACK comment marker, an unimplemented-stub throw (C#'s NotImplementedException), or an unimplemented-stub raise (Python's NotImplementedError) — a common shortcut where an AI agent leaves incomplete work behind a marker or a stub exception instead of finishing the implementation. Uses the same count-based old-vs-new diffing shape as 006/007/008: a pre-existing, untouched occurrence is never flagged, only a newly introduced one. Scoped narrowly per the phase's own guidance."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Catch Newly-Introduced Incomplete Work (Priority: P1)

A developer reviewing a PR that newly adds a TODO/FIXME/HACK comment or a not-implemented stub is shown a finding explaining what was left incomplete and where — without needing to notice the marker themselves while scanning a diff, and without it silently merging as if the work were done.

**Why this priority**: This is the entire feature. Leaving a TODO marker or a not-implemented stub behind is a well-documented shortcut in both human- and AI-agent-generated code — an agent asked to implement a feature will sometimes stub out the hard part and move on, or leave a comment marking work it didn't finish. Surfacing every newly-introduced instance for human review closes a real gap: a PR can otherwise look complete and pass automated tests while quietly containing acknowledged-incomplete work.

**Independent Test**: Can be fully tested by submitting a PR that newly introduces one of the recognized markers/stubs and verifying a finding is produced; and by submitting a PR that does not, and verifying no finding is produced.

**Acceptance Scenarios**:

1. **Given** a PR that adds a new TODO, FIXME, or HACK comment marker, **When** the PR is analyzed, **Then** a finding is produced identifying the marker, the file and location, and remediation guidance to finish the work or track it explicitly rather than leaving an inline marker.
2. **Given** a PR that adds a C# method body that throws a not-implemented stub exception, **When** the PR is analyzed, **Then** a finding is produced distinct from the comment-marker case.
3. **Given** a PR that adds a Python function body that raises a not-implemented stub error, **When** the PR is analyzed, **Then** a finding is produced.
4. **Given** a PR that changes code but introduces none of the recognized markers or stubs, **When** the PR is analyzed, **Then** no finding from this rule is produced.
5. **Given** a file that already contained one of these markers or stubs before the PR (unchanged by this PR), **When** the PR is analyzed, **Then** no finding is produced for that pre-existing, untouched occurrence — only newly introduced instances are flagged, consistent with the existing rules.

---

### Edge Cases

- What happens when the same marker or stub appears in multiple newly-changed files in one PR? Each occurrence produces its own independent finding, no deduplication, consistent with every other AgentGuard rule.
- What happens when a marker or stub is removed (the work gets finished) rather than added? No finding — only newly *introduced* occurrences are in scope.
- What happens when a file cannot be parsed as text at all (binary content, or content AgentGuard couldn't retrieve)? No finding for that file from this rule.
- What happens when "TODO" or "HACK" appears as part of an unrelated word (e.g. "Hackathon", "TODOClient")? Not flagged — the pattern requires a word boundary immediately after the marker, so it only matches the marker as a standalone word, not as a substring of a longer identifier.
- What happens when a TODO comment includes a tracked issue reference (e.g. "TODO(JIRA-123): ...")? Still flagged — this rule does not attempt to judge whether a given TODO is adequately tracked; it surfaces every newly-introduced instance for human review, the same accepted trade-off every other deterministic AgentGuard rule already has.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST produce a finding when a changed file newly introduces a recognized pattern, covering at minimum: a TODO/FIXME/HACK comment marker (`//` or `#` style), a C# not-implemented-exception stub, and a Python not-implemented-error stub.
- **FR-002**: System MUST NOT produce a finding for an occurrence of a recognized pattern that was already present in a file before the PR's changes — only newly introduced occurrences are in scope, matching every existing rule's "newly introduced" semantics.
- **FR-003**: Each finding MUST include which specific pattern matched, the affected file, evidence sufficient to locate the exact construct, and remediation guidance recommending the work be finished or explicitly tracked rather than left as an inline marker.
- **FR-004**: This rule MUST be classified under the ChangeManagement risk dimension (established in the risk-engine-foundation phase) and MUST report Deterministic kind and Certain confidence for every finding.
- **FR-005**: System MUST assign this rule Medium severity — a worth-a-second-look signal rather than a high-confidence serious risk, since a TODO or stub is sometimes a deliberate, reasonable placeholder within a larger incremental change.
- **FR-006**: This rule MUST have a stable, unique rule identifier that does not require modifying any other existing rule's definition.
- **FR-007**: This rule MUST be independently testable in isolation from the other existing rules and from the overall scoring/classification logic.
- **FR-008**: System MUST NOT implement this capability as a general-purpose code-completeness or static analysis engine — detection MUST use the same direct, deterministic pattern-matching approach already established by the existing rules, scoped to a fixed, reviewable set of recognized markers/stubs.

### Key Entities

- **TODO/Stub Finding**: A finding produced by this rule — inherits every field the existing Finding model already requires, with no new fields introduced by this feature.
- **Recognized Incompleteness Pattern**: One entry in the fixed set of patterns this rule matches against (e.g. "TODO/FIXME/HACK comment marker," "C# not-implemented stub," "Python not-implemented stub").

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of PRs that newly introduce a recognized marker or stub produce a finding identifying the specific pattern, file, and remediation.
- **SC-002**: 100% of PRs that only contain pre-existing (untouched) occurrences of a recognized pattern produce zero findings from this rule.
- **SC-003**: This rule's detection logic can be tested in complete isolation from the other existing rules and the overall risk-scoring pipeline.
- **SC-004**: Adding this rule changes zero existing test expectations for the nine previously-shipped rules — a PR containing no newly-introduced marker or stub produces byte-for-byte the same result as it would have before this feature.

## Assumptions

- This rule covers a fixed, deliberately narrow set of recognized markers and stubs for this increment (TODO/FIXME/HACK, C# NotImplementedException, Python NotImplementedError) — expanding coverage further (e.g. JS/TS-specific stub idioms), or adding the remaining Phase 2 risk areas (configuration risk, a dependency-scanning adapter), are separate, later increments.
- No new Finding fields, API contract changes, or UI changes are needed — output flows through the exact response shape already established.
- Judging whether a given TODO or stub is adequately justified or tracked is explicitly out of scope — this rule surfaces every newly-introduced occurrence for human review rather than attempting that judgment itself.
