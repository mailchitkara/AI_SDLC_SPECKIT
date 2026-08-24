# Feature Specification: Vulnerable Dependency Adapter

**Feature Branch**: `feature/dependency-risk-rules`

**Created**: 2026-08-24

**Status**: Draft

**Input**: User description: "Phase 2 (seventh and final increment) of the multi-phase AgentGuard risk-analysis expansion — Core Deterministic Risk Rules, covering the 'dependencies' area. Per the governance doc's explicit instruction, do NOT reimplement dependency-vulnerability scanning — AgentGuard cannot itself resolve a project's dependency tree or query vulnerability databases from a PR diff alone. Instead, add an adapter: accept an optional list of already-identified vulnerable dependencies (package name, version, severity, advisory id/url) as part of the analysis request — supplied by the caller, e.g. a CI step that already ran `dotnet list package --vulnerable`, `npm audit`, or `pip-audit` and parsed the results — and turn each into a Finding under the Dependencies risk dimension. This is architecturally different from every other Phase 2 rule shipped so far: it doesn't scan diff content at all, it translates externally-supplied, already-deterministic findings into AgentGuard's own finding shape. Requires an additive, optional API request field (mirroring how 005-risk-engine-foundation added the Thresholds field), not a Core-only change."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Surface Externally-Detected Vulnerable Dependencies in One Unified Result (Priority: P1)

A developer (or a CI pipeline) that has already run an external dependency-vulnerability scanner against a PR's dependency tree can pass those results into AgentGuard's analysis request, and see them appear as findings alongside every other AgentGuard rule's findings — in one unified risk score, classification, and recommendation, rather than as a separate report the reviewer has to check independently.

**Why this priority**: This is the entire feature. AgentGuard's core value is being the single place a reviewer looks for PR risk — a vulnerable dependency that's already been detected by a specialized, mature external tool (which AgentGuard explicitly must not reimplement) is exactly the kind of signal that should feed into that same unified picture rather than living in a separate, easy-to-miss report.

**Independent Test**: Can be fully tested by submitting an analysis request that includes one or more externally-supplied vulnerable-dependency entries and verifying a finding is produced for each, with the correct severity mapping, evidence, and remediation; and by submitting a request that omits this field entirely and verifying behavior is identical to before this feature (an empty list, not an error).

**Acceptance Scenarios**:

1. **Given** an analysis request that includes one vulnerable-dependency entry with a recognized severity level, **When** the PR is analyzed, **Then** a finding is produced identifying the package, version, and advisory, with a severity mapped from the entry's own severity level.
2. **Given** an analysis request that includes multiple vulnerable-dependency entries, **When** the PR is analyzed, **Then** one independent finding is produced per entry, with no deduplication.
3. **Given** an analysis request that omits the vulnerable-dependencies field entirely, **When** the PR is analyzed, **Then** the result is identical to what it would have been before this feature — zero findings from this rule, and the check for this rule appears as passed.
4. **Given** an analysis request that includes a vulnerable-dependency entry with an unrecognized severity value, **When** the request is submitted, **Then** the request is rejected with a validation error, the same way an invalid `changeType` on a changed file is already rejected.
5. **Given** an analysis request that includes a vulnerable-dependency entry missing a required field (package name or version), **When** the request is submitted, **Then** the request is rejected with a validation error.

---

### Edge Cases

- What happens when the external severity level is the highest recognized level (critical)? It is still mapped to AgentGuard's High severity, not Blocker — Blocker is reserved exclusively for `SECRET_DETECTED`'s "a credential is now live" certainty (established in `006-security-risk-rules`), and this rule must not dilute that invariant.
- What happens when the advisory URL or advisory ID is omitted? Still a valid finding — only package name, version, and severity are required; the advisory identifier fields are optional context included in evidence when present.
- What happens when the same package/version pair appears twice in the supplied list? Two independent findings, no deduplication, consistent with how every other AgentGuard rule already handles multiple occurrences (e.g. two different advisories against the same package version).
- What happens when the caller supplies a well-formed but empty vulnerable-dependencies list (`[]`) versus omitting the field entirely? Both produce zero findings — the two are treated identically.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept an optional list of externally-identified vulnerable dependencies as part of an analysis request, each specifying at minimum a package name, a version, and a severity level.
- **FR-002**: System MUST produce one finding per supplied vulnerable-dependency entry, with no deduplication across entries.
- **FR-003**: Each finding MUST include the package name, version, and (when supplied) the advisory identifier and/or URL as evidence, and remediation guidance to upgrade to a patched version per the advisory.
- **FR-004**: System MUST map each entry's external severity level to AgentGuard's own severity scale, with the external "critical" level capped at AgentGuard's High severity — never Blocker, which remains exclusive to `SECRET_DETECTED`.
- **FR-005**: This rule MUST be classified under the Dependencies risk dimension (established in the risk-engine-foundation phase) and MUST report Deterministic kind and Certain confidence for every finding — the underlying detection was already deterministic (performed by the external tool); this rule only translates it.
- **FR-006**: System MUST reject an analysis request containing a vulnerable-dependency entry with an unrecognized severity level, or missing a required field, the same way other malformed request fields are already rejected.
- **FR-007**: System MUST produce zero findings from this rule, and report its check as passed, when the vulnerable-dependencies field is omitted entirely or supplied as an empty list — both cases are equivalent.
- **FR-008**: This rule MUST NOT attempt to resolve a project's dependency tree, query a vulnerability database, or otherwise reimplement dependency-vulnerability scanning — it strictly translates already-supplied, externally-detected findings into AgentGuard's finding shape.
- **FR-009**: This rule MUST have a stable, unique rule identifier that does not require modifying any other existing rule's definition.
- **FR-010**: This rule MUST be independently testable in isolation from the other existing rules and from the overall scoring/classification logic.

### Key Entities

- **Vulnerable Dependency Finding**: A finding produced by this rule — inherits every field the existing Finding model already requires, with no new `Finding` fields introduced by this feature.
- **Vulnerable Dependency Entry**: One externally-supplied item in the request's optional vulnerable-dependencies list — package name, version, severity level, and optional advisory identifier/URL.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of analysis requests supplying one or more well-formed vulnerable-dependency entries produce one finding per entry, correctly evidenced and severity-mapped.
- **SC-002**: 100% of analysis requests that omit the vulnerable-dependencies field, or supply an empty list, produce zero findings from this rule and identical results to before this feature.
- **SC-003**: 100% of analysis requests supplying a malformed vulnerable-dependency entry (unrecognized severity, or a missing required field) are rejected with a validation error rather than silently accepted or silently dropped.
- **SC-004**: Adding this rule changes zero existing test expectations for the eleven previously-shipped rules — a request with no vulnerable-dependency entries produces byte-for-byte the same result as it would have before this feature.

## Assumptions

- This feature does not run any external tool itself, and does not specify or standardize which external tool a caller should use — it only defines the shape AgentGuard accepts once a caller has already run one. Documenting integration guidance for any specific tool (e.g. a ready-made GitHub Actions step that runs `dotnet list package --vulnerable --format json` and reshapes its output) is explicitly out of scope for this increment.
- No new `Finding` fields are needed — output flows through the exact response shape already established. The request-side addition is the only contract change, mirroring how `005-risk-engine-foundation` added the optional `Thresholds` field.
- This is the last of the seven Phase 2 areas identified in the governance doc's Section 23. After this ships, Phase 2 (Core Deterministic Risk Rules) is complete.
