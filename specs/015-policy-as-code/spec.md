# Feature Specification: Policy-as-Code Configuration Loading

**Feature Branch**: `feature/policy-as-code`

**Created**: 2026-08-24

**Status**: Draft

**Input**: User description: "Phase 5 (first increment) of the multi-phase AgentGuard risk-analysis expansion — Enterprise Risk Governance, covering the 'policy-as-code' area. Two existing rules, ArchitectureViolationRule (004) and BusinessCriticalPathRule (013), each accept a configuration object (ForbiddenDependencyConfig, BusinessCriticalPathConfig) that is wired via dependency injection in AgentGuard.Api's Program.cs — but today, neither is actually populated with anything: ForbiddenDependencyConfig is hardcoded to .Empty and BusinessCriticalPathConfig isn't registered at all. There is currently no way for anyone operating an AgentGuard deployment to actually configure either rule without forking AgentGuard's own source code and redeploying. Add the ability to load both configurations from a single external JSON file at service startup, controlled by an environment variable pointing to the file's path — absent that environment variable (the default), behavior is byte-for-byte identical to today (both configs empty, matching current production behavior exactly)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configure Forbidden Dependencies and Business-Critical Paths Without Forking AgentGuard (Priority: P1)

An operator running an AgentGuard deployment for their organization wants to configure which architecture dependencies are forbidden and which paths are business-critical, without needing to fork AgentGuard's source code, add a C# change, and maintain their own build/deploy pipeline just to change a list of path patterns.

**Why this priority**: This is the entire feature, and it closes a real, already-existing gap rather than adding a new capability from scratch — both `ArchitectureViolationRule` and `BusinessCriticalPathRule` were already shipped with a configuration seam, but that seam has never actually been reachable by anyone except an AgentGuard core contributor editing `Program.cs` directly. Every other AgentGuard capability (thresholds, vulnerable dependencies) is already configurable per-request; these two configs are the only remaining "configurable in theory, unreachable in practice" gap.

**Independent Test**: Can be fully tested by starting the service with the policy-file environment variable pointing to a well-formed JSON file, submitting a PR that matches an entry from each configuration, and verifying both `ArchitectureViolationRule` and `BusinessCriticalPathRule` findings are produced accordingly; and by starting the service with the environment variable unset (or pointing to a missing file) and verifying both rules behave exactly as they do today (zero findings, regardless of what any PR touches).

**Acceptance Scenarios**:

1. **Given** a well-formed policy file containing one forbidden-dependency relationship and one business-critical path, and the service started with the policy-file environment variable pointing to it, **When** a PR matching both is analyzed, **Then** both rules produce their respective findings, unchanged in shape from their existing behavior.
2. **Given** the policy-file environment variable is unset, **When** the service starts and any PR is analyzed, **Then** both `ArchitectureViolationRule` and `BusinessCriticalPathRule` produce zero findings regardless of what the PR touches — identical to today's behavior.
3. **Given** the policy-file environment variable points to a file that does not exist, **When** the service starts, **Then** it behaves the same as Scenario 2 (empty configs) rather than failing to start — a missing file is treated the same as an unset variable, not as an error, since "no policy file yet" is a normal, expected operating state.
4. **Given** the policy-file environment variable points to a file that exists but contains malformed JSON or a value that doesn't match the expected shape, **When** the service starts, **Then** startup fails loudly with a clear error identifying the problem, rather than silently falling back to empty configs — a policy file the operator explicitly pointed to that turns out to be broken is an operator error that should be surfaced immediately, not silently ignored in a way that could leave real coverage gaps unnoticed.

---

### Edge Cases

- What happens when the policy file is well-formed but supplies an empty list for one or both configuration sections? Treated identically to omitting that section entirely — an explicit empty list and an absent key both produce `.Empty` for that config, no findings from that rule.
- What happens when the policy file contains additional, unrecognized JSON fields? Ignored — this feature only reads the two recognized sections; forward-compatibility for future policy sections is not broken by strict rejection of unknown fields.
- What happens when both environment-variable-based configuration and any future per-request configuration exist for the same rule? Out of scope for this increment — `ArchitectureViolationRule` and `BusinessCriticalPathRule` have no per-request configuration today (unlike thresholds or vulnerable dependencies), so there is no conflict to resolve; this feature is the only configuration path for these two rules.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support loading both `ForbiddenDependencyConfig` and `BusinessCriticalPathConfig` from a single external JSON file at service startup.
- **FR-002**: System MUST determine the policy file's path from an environment variable; when that variable is unset, both configurations MUST be empty, producing byte-for-byte identical behavior to the service's current, unconfigured state.
- **FR-003**: System MUST treat a policy-file path that does not point to an existing file the same as an unset environment variable (empty configurations, no startup failure) — a missing file is a normal, expected state, not an error.
- **FR-004**: System MUST fail service startup with a clear, identifying error when the policy file exists but cannot be parsed into the expected shape (malformed JSON, or a value not matching the expected structure) — this is the one case that must be loud, not silent.
- **FR-005**: The policy file's JSON shape for forbidden dependencies MUST express the same two fields `ForbiddenDependency` already has (a source pattern, a target pattern); the shape for business-critical paths MUST express the same two fields `BusinessCriticalPath` already has (a path pattern, a label).
- **FR-006**: This feature MUST NOT change either `ArchitectureViolationRule`'s or `BusinessCriticalPathRule`'s own evaluation logic, findings shape, or any other existing rule's behavior — it only changes how their existing configuration objects get populated.
- **FR-007**: This feature MUST NOT introduce any new request-level API field, response field, or persisted state — configuration happens once, at service startup, from an operator-controlled file, not per-request.

### Key Entities

- **Policy File**: A single JSON document, read once at startup, containing zero or more forbidden-dependency relationships and zero or more business-critical path patterns.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of services started with a well-formed policy file produce findings from `ArchitectureViolationRule`/`BusinessCriticalPathRule` matching every entry in that file, with no change to either rule's finding shape.
- **SC-002**: 100% of services started with the environment variable unset, or pointing to a missing file, behave identically to the service's current production behavior — zero findings from either rule regardless of PR content.
- **SC-003**: 100% of services started with a malformed policy file fail to start, with an error message identifying the problem, rather than starting successfully with silently-empty configuration.
- **SC-004**: Adding this feature changes zero existing test expectations for any of the fourteen previously-shipped rules — analysis behavior for every existing test scenario, run with no policy file configured, is byte-for-byte unchanged.

## Assumptions

- This is an operator-level, service-startup-time configuration mechanism — it configures the AgentGuard deployment as a whole, the same scope `ForbiddenDependencyConfig`/`BusinessCriticalPathConfig` already have today (both are process-wide singletons, not per-request or per-consuming-repository). A future increment could add per-repository or per-request policy scoping (the governance doc's separate "repo/org rule profiles" area); that is explicitly out of scope here.
- The policy file's location is controlled by an environment variable rather than a fixed, hardcoded path, matching how this deployment's other environment-driven configuration already works (`FRONTEND_ORIGIN`, `PORT`, `RENDER_DEPLOY_HOOK_URL`).
- No UI changes are needed — this is a backend/operator-facing capability with no new API surface for the frontend to render.
