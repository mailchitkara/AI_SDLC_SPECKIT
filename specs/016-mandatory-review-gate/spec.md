# Feature Specification: Mandatory Review Gate by Risk Dimension

**Feature Branch**: `feature/mandatory-review-gate`

**Created**: 2026-08-24

**Status**: Draft

**Input**: User description: "Phase 5 (second increment) of the multi-phase AgentGuard risk-analysis expansion — Enterprise Risk Governance, combining the 'configurable mandatory human gates' and 'policy overrides for business-critical areas' areas. Add a governance policy, configured through the same policy file 015-policy-as-code introduced, that names a set of risk dimensions (e.g. BusinessCriticality) for which any finding at all — regardless of severity or score — forces the recommendation to be at least HUMAN_REVIEW_REQUIRED, never lower. This is distinct from the existing per-finding MandatoryOverride mechanism (005-risk-engine-foundation), which is a rule's own decision made at evaluation time and can force all the way to BLOCK_MERGE; this is a policy-layer floor, applied after all findings are computed, configured by the operator rather than any individual rule, and never exceeds HUMAN_REVIEW_REQUIRED on its own. Matches the governing principle already stated in the pasted governance document: AgentGuard assists and governs software-change decisions, it does not autonomously approve high-impact production changes by letting a low score alone wave through a change in a dimension the organization has decided always needs a human look."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Guarantee Human Review for Configured Risk Dimensions (Priority: P1)

An operator running AgentGuard for their organization wants to guarantee that any PR touching a business-critical area (or any other dimension they choose) always gets at least a human review, even if the PR's overall computed score would otherwise classify it as low-risk and safe to auto-approve — because a low score from AgentGuard's other rules says nothing about how much the organization cares about that specific area.

**Why this priority**: This is the entire feature, and it directly closes the gap `013-business-critical-path-detection` left open — that feature could identify when a PR touches a business-critical path, but a low-severity finding in a low-scoring PR could still result in a `SAFE_TO_REVIEW` recommendation despite touching a critical area. This increment makes that identification actually govern the outcome, matching the governance document's core principle that AgentGuard assists and governs decisions rather than letting a score alone wave through what the organization has decided always needs a human look.

**Independent Test**: Can be fully tested by configuring one risk dimension as mandatory-review, submitting a PR whose only finding is in that dimension and would otherwise score low enough for `SAFE_TO_REVIEW`, and verifying the recommendation is instead `HUMAN_REVIEW_REQUIRED`; and by submitting the same PR with no such dimension configured, and verifying the original, unfloored recommendation is produced.

**Acceptance Scenarios**:

1. **Given** a configured mandatory-review dimension and a PR whose only finding is in that dimension, with a score that would otherwise classify as `SAFE_TO_REVIEW` or `REVIEW_RECOMMENDED`, **When** the PR is analyzed, **Then** the recommendation is `HUMAN_REVIEW_REQUIRED`, and the response indicates the recommendation was forced by this policy.
2. **Given** a configured mandatory-review dimension and a PR whose findings, independent of this policy, already classify at `HUMAN_REVIEW_REQUIRED` or `BLOCK_MERGE`, **When** the PR is analyzed, **Then** the recommendation is unaffected by this policy — a floor never lowers a recommendation, and the response does not claim this policy forced an outcome the score/override already reached on its own.
3. **Given** no mandatory-review dimensions are configured (the default), **When** any PR is analyzed, **Then** every recommendation is produced exactly as it was before this feature existed — byte-for-byte identical behavior.
4. **Given** a PR with a mandatory-override finding (an existing, unrelated mechanism) that already forces `BLOCK_MERGE`, **When** the PR is analyzed, **Then** `BLOCK_MERGE` is unaffected — this policy's floor never lowers an outcome a stronger, existing mechanism already reached.

---

### Edge Cases

- What happens when a PR has findings in multiple dimensions, only one of which is configured as mandatory-review? The floor still applies — a single matching finding is sufficient to raise the floor, regardless of how many other, non-matching findings exist.
- What happens when the configured mandatory-review dimension never actually has any findings from any shipped rule (a misconfigured or future-reserved dimension name)? No effect — this policy only ever raises the floor when a finding in that dimension actually exists; an unused dimension name in the configuration is inert, not an error, since a future rule might yet use it.
- What happens when this policy and a rule's own `MandatoryOverride` both apply to the same PR? `MandatoryOverride` (an existing mechanism reaching all the way to `BLOCK_MERGE`) already satisfies this policy's weaker floor (`HUMAN_REVIEW_REQUIRED`) — the response only reports this policy as the forcing mechanism when it was the reason the recommendation moved, not when a stronger mechanism already accounts for the outcome.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support configuring a set of risk dimensions for which any finding at all forces the recommendation to be at least `HUMAN_REVIEW_REQUIRED`.
- **FR-002**: System MUST NOT lower a recommendation that this policy's own floor is already at or below — the floor only ever raises a recommendation toward `HUMAN_REVIEW_REQUIRED`, never past it, and never below whatever the score/classification/mandatory-override pipeline already produced.
- **FR-003**: System MUST produce an unaffected, byte-for-byte identical recommendation to today's behavior when no mandatory-review dimensions are configured — the default MUST be an empty set.
- **FR-004**: The analysis response MUST distinguish whether the final recommendation was forced by this policy specifically, separately from whether it was forced by the existing per-finding mandatory-override mechanism — the two are different mechanisms with different ceilings (`HUMAN_REVIEW_REQUIRED` vs. `BLOCK_MERGE`) and a caller inspecting the result MUST be able to tell which, if either, applied.
- **FR-005**: This policy MUST be configured the same way `015-policy-as-code`'s existing configuration is — operator-level, loaded from the same policy file at service startup, not a per-request field.
- **FR-006**: System MUST treat an unrecognized risk-dimension name in the configuration the same way `015-policy-as-code` treats other malformed policy-file content — a loud startup failure, not a silently-ignored entry, since a typo'd dimension name would otherwise silently mean the gate never applies.
- **FR-007**: This feature MUST NOT change any existing rule's own evaluation logic, `MandatoryOverride` behavior, or scoring arithmetic — it is a policy layer applied after all findings and the score/classification are already computed, exactly like the existing `MandatoryOverride` mechanism's own relationship to scoring.

### Key Entities

- **Risk Governance Policy**: The configured set of risk dimensions for which any finding forces at least `HUMAN_REVIEW_REQUIRED` — empty by default.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of PRs whose only findings are in a configured mandatory-review dimension, and would otherwise classify below `HUMAN_REVIEW_REQUIRED`, are instead recommended `HUMAN_REVIEW_REQUIRED`, with the response indicating this policy forced it.
- **SC-002**: 100% of PRs analyzed with no mandatory-review dimensions configured produce byte-for-byte identical results to before this feature existed.
- **SC-003**: 100% of PRs whose recommendation already reaches `HUMAN_REVIEW_REQUIRED` or `BLOCK_MERGE` through the existing score/override pipeline are unaffected by this policy, and the response does not claim this policy was the forcing mechanism when it wasn't.
- **SC-004**: Adding this feature changes zero existing test expectations for the fourteen previously-shipped rules or the `015` policy-loading feature — a PR analyzed with no mandatory-review dimensions configured produces byte-for-byte the same result as it would have before this feature.

## Assumptions

- This increment adds one new boolean field to the analysis response (distinguishing this policy's floor from the existing `MandatoryOverride` mechanism, FR-004) — the only response-shape change in this feature; no new request-level field is added (FR-005).
- Combining this with `015-policy-as-code`'s existing JSON file is a natural, minimal extension rather than a new configuration mechanism — both are operator-level, startup-time, empty-by-default policy inputs with the same lifecycle.
- This is the second and, for now, final Phase 5 increment in this work session — remaining Phase 5 areas (risk delta between runs, auditability, reviewer routing, repo/org rule profiles, reporting) each need either persisted state (a database, which this service has explicitly avoided everywhere so far) or a larger scoping decision, and are separate, later increments.
