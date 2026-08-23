# Feature Specification: Overly Permissive Access Control Detection

**Feature Branch**: `feature/security-risk-rules`

**Created**: 2026-08-24

**Status**: Draft

**Input**: User description: "Phase 2 (first increment) of the multi-phase AgentGuard risk-analysis expansion — Core Deterministic Risk Rules. Add a new deterministic Security-dimension rule that flags newly-introduced overly-permissive access control changes (wildcard CORS, disabled authentication/authorization, wildcard allowed-hosts) across common backend stacks, using the same direct pattern-matching approach as the existing SecretDetected rule rather than duplicating a SAST tool. Scoped narrowly per the phase's own guidance to prefer smaller, independently reviewable increments over one large batch covering every listed risk area at once."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Catch a Newly-Loosened Access Control Change (Priority: P1)

A developer reviewing a PR that adds a wildcard CORS policy, disables an authentication check, or opens up an allowed-hosts list is shown a finding explaining exactly what was loosened, where, and how to fix it — without needing to spot the change by reading a diff themselves.

**Why this priority**: This is the entire feature. Overly permissive access control is a well-documented, common failure mode in AI-agent-generated code (an agent asked to "make the API work" will often reach for an unrestricted CORS policy or the AllowAnonymous attribute rather than a properly scoped policy), and it is exactly the kind of objectively-detectable pattern AgentGuard's existing deterministic, evidence-based approach is suited to — the same shape of capability as the existing secret-detection rule, applied to a different pattern family.

**Independent Test**: Can be fully tested by submitting a PR that newly introduces one of the recognized permissive-access patterns and verifying a finding is produced with the correct severity, dimension, evidence, and remediation; and by submitting a PR that does not, and verifying no finding is produced.

**Acceptance Scenarios**:

1. **Given** a PR that adds a new file enabling a wildcard CORS origin (e.g. an ASP.NET Core startup file calling the CORS builder's no-restriction method with no origin allow-list, or a Node/Express CORS configuration whose origin option is set to a bare wildcard), **When** the PR is analyzed, **Then** a finding is produced identifying the specific pattern matched, the file and location, and remediation guidance for scoping the policy down.
2. **Given** a PR that adds the AllowAnonymous attribute (or an equivalent disabled-authorization marker) to previously-protected code, **When** the PR is analyzed, **Then** a finding is produced distinct from the CORS case, with evidence naming the specific construct.
3. **Given** a PR that sets a wildcard allowed-hosts configuration (e.g. Django's `ALLOWED_HOSTS` set to a single-element list containing only a wildcard), **When** the PR is analyzed, **Then** a finding is produced.
4. **Given** a PR that changes code but introduces none of the recognized patterns, **When** the PR is analyzed, **Then** no finding from this rule is produced.
5. **Given** a file that already contained one of these patterns before the PR (unchanged by this PR), **When** the PR is analyzed, **Then** no finding is produced for that pre-existing, untouched occurrence — only newly introduced instances are flagged, consistent with how the existing secret-detection rule already behaves.

---

### Edge Cases

- What happens when the same permissive pattern appears in multiple newly-changed files in one PR? Each occurrence produces its own independent finding, consistent with how every other AgentGuard rule already handles multiple occurrences (no deduplication or merging across findings).
- What happens when a permissive pattern is removed (loosening reverted) rather than added? No finding — only newly *introduced* permissiveness is in scope, mirroring the existing secret-detection and architecture-violation rules' "newly introduced" semantics.
- What happens when a file cannot be parsed as source code at all (e.g., binary content, or content AgentGuard couldn't retrieve)? No finding for that file from this rule — consistent with how other content-scanning rules already skip content they cannot read, rather than erroring the whole analysis.
- What happens when a pattern is present only inside a comment or a test fixture, not executable configuration? Out of scope for this deterministic, text-pattern-based rule to distinguish — the same accepted limitation the existing secret-detection rule already has (it is a known, documented trade-off of staying pattern-based rather than building a full parser per language, and is not new to this feature).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST produce a finding when a changed file newly introduces a recognized overly-permissive access control pattern, covering at minimum: a wildcard CORS origin configuration, a disabled/bypassed authentication or authorization marker, and a wildcard allowed-hosts configuration.
- **FR-002**: System MUST NOT produce a finding for an occurrence of a recognized pattern that was already present in a file before the PR's changes — only newly introduced occurrences are in scope, matching the existing secret-detection and architecture-violation rules' established "newly introduced" semantics.
- **FR-003**: Each finding MUST include which specific pattern matched, the affected file, evidence sufficient to locate the exact construct without needing to re-read the whole file, and remediation guidance specific to that pattern (e.g., scoping a CORS policy vs. restoring an authorization check).
- **FR-004**: This rule MUST be classified under the Security risk dimension (established in the risk-engine-foundation phase) and MUST report Deterministic kind and Certain confidence for every finding, consistent with every other rule shipped so far.
- **FR-005**: System MUST assign this rule a severity that reflects a serious but not always-instantly-catastrophic issue (distinct from the Blocker severity reserved for exposed secrets) — High severity, matching the existing precedent set by the architecture-violation and API-contract-breaking-change rules.
- **FR-006**: This rule MUST have a stable, unique rule identifier that does not require modifying any other existing rule's definition, per the risk-engine-foundation phase's existing rule-identity model.
- **FR-007**: This rule MUST be independently testable — its detection logic MUST be verifiable in isolation from the other four-plus existing rules, and from the overall scoring/classification logic.
- **FR-008**: System MUST NOT implement this capability as a general-purpose static analysis engine or by wrapping/reimplementing an existing SAST tool — detection MUST use the same direct, deterministic pattern-matching approach already established by the secret-detection rule, scoped to a fixed, reviewable set of recognized patterns rather than open-ended code analysis.

### Key Entities

- **Overly Permissive Access Control Finding**: A finding produced by this rule — inherits every field the existing Finding model already requires (rule id, name, severity, explanation, evidence, location, remediation, risk dimension, confidence, kind), with no new fields introduced by this feature.
- **Recognized Permissive Pattern**: One entry in the fixed set of patterns this rule matches against (e.g. "wildcard CORS origin," "disabled authorization marker," "wildcard allowed-hosts") — analogous to the existing secret-detection rule's fixed set of recognized secret patterns.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of PRs that newly introduce a recognized permissive-access pattern produce a finding identifying the specific pattern, file, and remediation — verifiable without reading the PR's raw diff.
- **SC-002**: 100% of PRs that only contain pre-existing (untouched) occurrences of a recognized pattern produce zero findings from this rule.
- **SC-003**: This rule's detection logic can be tested in complete isolation — 100% of its test cases exercise the rule directly, without depending on the other existing rules or the overall risk-scoring pipeline.
- **SC-004**: Adding this rule changes zero existing test expectations for the five V1 rules or the risk-engine-foundation behavior — a PR containing no permissive-access pattern produces byte-for-byte the same result as it would have before this feature.

## Assumptions

- This rule covers a fixed, deliberately narrow set of recognized patterns for this increment (wildcard CORS, disabled authorization, wildcard allowed-hosts) across a small number of common stacks (ASP.NET Core, Node/Express, Django-style Python) — expanding pattern coverage further, or adding the remaining Phase 2 risk areas (testing, additional architecture rules, dependency scanning adapters, reliability, generated-file contamination, TODO/stub detection, configuration risk) are separate, later increments, per the phase's own preference for smaller independently-reviewable PRs over one large batch.
- No new Finding fields, API contract changes, or UI changes are needed — this rule's output flows through the exact same result shape the risk-engine-foundation phase already established, so it appears in existing UI displays and API responses automatically.
- Dependency-vulnerability scanning and any other capability better served by an existing, mature external tool are explicitly out of scope for AgentGuard to reimplement — a later increment may instead define an *adapter* to an external tool's output rather than original detection logic, per the phase's explicit instruction.
