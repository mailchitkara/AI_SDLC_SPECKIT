# Feature Specification: Large New File Detection

**Feature Branch**: `feature/large-new-file-detection`

**Created**: 2026-08-24

**Status**: Draft

**Input**: User description: "Phase 4 (second increment) of the multi-phase AgentGuard risk-analysis expansion — Contextual Risk Intelligence, covering the 'novelty' area. Add a new deterministic rule that flags when a PR introduces a substantial brand-new file — code with no track record of real-world usage, review history, or production exposure, which carries statistically higher defect risk than mature, previously-reviewed code. Deliberately scoped to what's derivable from data already present on every changed file (ChangeType and LinesAdded) rather than the true git-history-based novelty signal (how long ago a file was actually created across the repository's full history), which requires a new GitHub commit-history API integration deferred to a later increment. A small brand-new file (a new export, a new small config) should not fire — only a genuinely substantial one."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Flag a Substantial Brand-New File (Priority: P1)

A developer reviewing a PR that introduces a large, entirely new file is shown a finding noting that this code has no track record — no prior review history, no production exposure, no accumulated bug-fix history — and so may warrant closer scrutiny than an equivalent-sized change to an already-established file.

**Why this priority**: This is the entire feature, and the simplest possible entry point into Phase 4's "novelty" area — genuinely new code is a well-documented higher-defect-risk category (it hasn't had the chance to be battle-tested), and this signal is fully computable from data every changed file already carries, with no new data source, matching the same "prefer smaller, independently reviewable increments" discipline every prior phase has followed.

**Independent Test**: Can be fully tested by submitting a PR that adds one new file at or above the size threshold and verifying a finding is produced; and by submitting a PR that adds a new file below the threshold, or modifies an existing file of any size, and verifying no finding is produced.

**Acceptance Scenarios**:

1. **Given** a PR that adds a brand-new file with a line count at or above the configured threshold, **When** the PR is analyzed, **Then** a finding is produced identifying the file, its size, and guidance noting the file has no prior review or production history.
2. **Given** a PR that adds a brand-new file with a line count below the configured threshold, **When** the PR is analyzed, **Then** no finding is produced.
3. **Given** a PR that substantially modifies an existing (non-new) file, regardless of how many lines change, **When** the PR is analyzed, **Then** no finding is produced from this rule — only genuinely new files are in scope, not large changes to established ones (that risk is `LargeChangeSizeRule`'s job, unaffected by this feature).
4. **Given** a PR that adds multiple large new files, **When** the PR is analyzed, **Then** one independent finding is produced per qualifying file, no deduplication.

---

### Edge Cases

- What happens when a "new" file is technically a rename/move of an existing file with substantial content (not truly novel code)? Out of scope for this increment to distinguish — `ChangeType.Renamed` is a distinct value from `ChangeType.Added` in AgentGuard's existing model, and this rule only evaluates `Added` files, so a rename (even one GitHub reports with a large diff) is correctly out of scope without needing special-case logic.
- What happens when a large new file is deleted in the same PR that added it (net churn within one PR)? Out of scope to detect as a special case — this rule evaluates each changed file independently, matching every other AgentGuard rule's behavior; a deleted file is `ChangeType.Deleted`, not `Added`, so it's simply not in scope.
- What happens when line-count data is unavailable for a changed file? No finding for that file — consistent with how every other AgentGuard rule skips content/data it cannot evaluate rather than guessing.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST produce a finding when a changed file is newly added in the PR and its added-line count meets or exceeds a fixed threshold.
- **FR-002**: System MUST NOT produce a finding for a newly-added file below the threshold, or for a modified, deleted, or renamed file of any size.
- **FR-003**: Each finding MUST include the affected file, its line count, and remediation-style guidance noting the file has no prior review or production history and may warrant closer scrutiny.
- **FR-004**: This rule MUST be classified under the ChangeManagement risk dimension (established in the risk-engine-foundation phase, and already used by `009`/`010` for other "nature of the change" signals) and MUST report Deterministic kind and Certain confidence for every finding.
- **FR-005**: System MUST assign this rule Medium severity — a large new file isn't inherently broken, only less proven, matching the same calibration reasoning used for `009`/`010`.
- **FR-006**: This rule MUST have a stable, unique rule identifier that does not require modifying any other existing rule's definition.
- **FR-007**: This rule MUST be independently testable in isolation from the other existing rules and from the overall scoring/classification logic.
- **FR-008**: System MUST NOT require any external data source, network call, or LLM to evaluate this rule — a changed file's own `ChangeType` and `LinesAdded`, already present in every existing analysis request, are sufficient.
- **FR-009**: System MUST produce one independent finding per qualifying file when a PR introduces multiple large new files — no deduplication.

### Key Entities

- **Large New File Finding**: A finding produced by this rule — inherits every field the existing Finding model already requires, with no new fields introduced by this feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of PRs that add a new file at or above the threshold produce a finding identifying the file, its size, and remediation guidance.
- **SC-002**: 100% of PRs that only add new files below the threshold, or only modify/delete/rename existing files, produce zero findings from this rule.
- **SC-003**: This rule's detection logic can be tested in complete isolation from the other existing rules and the overall risk-scoring pipeline.
- **SC-004**: Adding this rule changes zero existing test expectations for the thirteen previously-shipped rules — a PR with no qualifying new file produces byte-for-byte the same result as it would have before this feature.

## Assumptions

- This increment covers only "new in this specific PR" as its novelty proxy — the true git-history-based signal (how long ago a file was actually first created, regardless of which PR is being analyzed now) requires a new GitHub commit-history API integration, out of scope here and deferred to a later Phase 4 increment.
- The size threshold is a fixed constant for this increment (not user-configurable), matching `LargeChangeSizeRule`'s existing precedent of fixed, in-code thresholds rather than request-level configuration.
- No new Finding fields, API contract changes, or UI changes are needed — output flows through the exact response shape already established.
