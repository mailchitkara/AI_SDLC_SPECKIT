# Feature Specification: Insecure Configuration Detection

**Feature Branch**: `feature/configuration-risk-rules`

**Created**: 2026-08-24

**Status**: Draft

**Input**: User description: "Phase 2 (sixth increment) of the multi-phase AgentGuard risk-analysis expansion — Core Deterministic Risk Rules. Add a new deterministic Configuration-dimension rule that flags a PR newly enabling Django's debug setting, or disabling TLS/certificate validation across .NET (a certificate-validation callback that unconditionally accepts every certificate), Node.js (an HTTPS option rejecting certificate rejection), or Python's requests library (a call that turns off TLS verification) — configuration choices that are reasonable in local development but dangerous if they reach production, and that AI agents commonly reach for to unblock themselves against a self-signed cert or an HTTPS error without realizing the change is unsafe outside a dev environment. This is distinct from 006-security-risk-rules (which covers access-control loosening, not configuration/transport-security settings) and from SECRET_DETECTED (this rule is about insecure settings, not exposed credential values). Uses the same count-based old-vs-new diffing shape as 006/007/008/010. Scoped narrowly per the phase's own guidance."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Catch a Newly-Introduced Insecure Configuration Setting (Priority: P1)

A developer reviewing a PR that newly enables Django debug mode or disables TLS certificate validation is shown a finding explaining exactly what configuration setting is now unsafe for production, where, and how to fix it — without needing to already know that setting is dangerous themselves.

**Why this priority**: This is the entire feature. These specific settings are well-documented, high-impact anti-patterns: Django's own documentation explicitly warns its debug setting must never be left enabled in production (it leaks stack traces and settings to any visitor), and disabling TLS certificate validation defeats the entire purpose of HTTPS, silently exposing the application to man-in-the-middle attacks. An AI agent debugging a local HTTPS/certificate error will often reach for the fastest fix — disabling verification — without recognizing the change is unsafe outside development. This is exactly the kind of objectively-detectable pattern AgentGuard's existing deterministic, evidence-based approach is suited to.

**Independent Test**: Can be fully tested by submitting a PR that newly introduces one of the recognized insecure-configuration patterns and verifying a finding is produced with the correct severity, dimension, evidence, and remediation; and by submitting a PR that does not, and verifying no finding is produced.

**Acceptance Scenarios**:

1. **Given** a PR that sets Django's `DEBUG` setting to enabled, **When** the PR is analyzed, **Then** a finding is produced identifying the setting, the file and location, and remediation guidance to disable debug mode outside local development.
2. **Given** a PR that adds a .NET TLS certificate validation callback that unconditionally accepts every certificate, **When** the PR is analyzed, **Then** a finding is produced distinct from the Django case, with evidence naming the specific construct.
3. **Given** a PR that adds a Node.js HTTPS/TLS option disabling certificate rejection, **When** the PR is analyzed, **Then** a finding is produced.
4. **Given** a PR that adds a Python `requests` call disabling TLS verification, **When** the PR is analyzed, **Then** a finding is produced.
5. **Given** a PR that changes code but introduces none of the recognized patterns, **When** the PR is analyzed, **Then** no finding from this rule is produced.
6. **Given** a file that already contained one of these patterns before the PR (unchanged by this PR), **When** the PR is analyzed, **Then** no finding is produced for that pre-existing, untouched occurrence — only newly introduced instances are flagged, consistent with every existing rule.

---

### Edge Cases

- What happens when the same pattern appears in multiple newly-changed files in one PR? Each occurrence produces its own independent finding, no deduplication, consistent with every other AgentGuard rule.
- What happens when an insecure setting is removed (reverted to secure) rather than added? No finding — only newly *introduced* occurrences are in scope.
- What happens when a file cannot be parsed as text at all (binary content, or content AgentGuard couldn't retrieve)? No finding for that file from this rule.
- What happens when the insecure setting appears in a test-only or local-development-only file (e.g. a file clearly scoped to local tooling)? Still flagged — this rule does not attempt to judge a file's deployment scope from its path or contents; it surfaces every newly-introduced occurrence for human review, the same accepted trade-off every other deterministic AgentGuard rule already has (matching `006`'s equivalent edge case for access-control patterns).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST produce a finding when a changed file newly introduces a recognized insecure-configuration pattern, covering at minimum: Django's `DEBUG` setting enabled, a .NET TLS certificate-validation callback that unconditionally accepts every certificate, a Node.js HTTPS/TLS option disabling certificate rejection, and a Python `requests` call disabling TLS verification.
- **FR-002**: System MUST NOT produce a finding for an occurrence of a recognized pattern that was already present in a file before the PR's changes — only newly introduced occurrences are in scope, matching every existing rule's "newly introduced" semantics.
- **FR-003**: Each finding MUST include which specific pattern matched, the affected file, evidence sufficient to locate the exact construct, and remediation guidance specific to that pattern.
- **FR-004**: This rule MUST be classified under the Configuration risk dimension (established in the risk-engine-foundation phase) and MUST report Deterministic kind and Certain confidence for every finding.
- **FR-005**: System MUST assign this rule High severity, matching the precedent set by `006-security-risk-rules` for a serious, security-adjacent configuration weakening.
- **FR-006**: This rule MUST have a stable, unique rule identifier that does not require modifying any other existing rule's definition.
- **FR-007**: This rule MUST be independently testable in isolation from the other existing rules and from the overall scoring/classification logic.
- **FR-008**: System MUST NOT implement this capability as a general-purpose configuration/infrastructure-as-code analyzer — detection MUST use the same direct, deterministic pattern-matching approach already established by the existing rules, scoped to a fixed, reviewable set of recognized patterns rather than open-ended configuration analysis.

### Key Entities

- **Insecure Configuration Finding**: A finding produced by this rule — inherits every field the existing Finding model already requires, with no new fields introduced by this feature.
- **Recognized Insecure-Configuration Pattern**: One entry in the fixed set of patterns this rule matches against (e.g. "Django debug mode enabled," "disabled TLS certificate validation" per stack).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of PRs that newly introduce a recognized insecure-configuration pattern produce a finding identifying the specific pattern, file, and remediation.
- **SC-002**: 100% of PRs that only contain pre-existing (untouched) occurrences of a recognized pattern produce zero findings from this rule.
- **SC-003**: This rule's detection logic can be tested in complete isolation from the other existing rules and the overall risk-scoring pipeline.
- **SC-004**: Adding this rule changes zero existing test expectations for the ten previously-shipped rules — a PR containing no newly-introduced insecure-configuration pattern produces byte-for-byte the same result as it would have before this feature.

## Assumptions

- This rule covers a fixed, deliberately narrow set of recognized patterns for this increment (Django debug mode, .NET/Node.js/Python TLS-validation-disabling) — expanding coverage further, or adding a dependency-scanning adapter (the one remaining Phase 2 area, architecturally distinct per the governance doc's explicit adapter guidance), are separate, later increments.
- No new Finding fields, API contract changes, or UI changes are needed — output flows through the exact response shape already established.
- This rule is distinct from `006-security-risk-rules` (access-control loosening) and from `SECRET_DETECTED` (exposed credential values) — it covers insecure *settings*, not access policy or leaked secrets.
