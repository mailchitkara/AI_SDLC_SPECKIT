# Feature Specification: Risk Engine Foundation

**Feature Branch**: `feature/risk-engine-foundation`

**Created**: 2026-08-23

**Status**: Draft

**Input**: User description: "Phase 1 of a multi-phase AgentGuard risk-analysis expansion. Build the extensible foundation future rule packs will be built on — stable rule IDs, risk dimensions/categories, a richer finding/evidence model (severity, confidence, deterministic-vs-contextual classification, explainability), configurable thresholds, improved risk aggregation, a mandatory override/blocking capability — while remaining fully compatible with the existing five AgentGuard V1 rules. Do not add new detection rules in this phase; that is deferred to later phases."

## Clarifications

### Session 2026-08-23

- Q: How should a finding's confidence be represented? → A: Fixed enum: Certain / High / Medium / Low (mirrors the existing Severity enum's style)
- Q: What set of risk dimensions should exist for this phase? → A: Broader set anticipating later phases' stated rule areas: Security, Testing, Compatibility, Architecture, Change Management, Dependencies, Reliability, Configuration
- Q: Where does a threshold configuration live when overridden? → A: Per-request — the caller optionally supplies a threshold configuration alongside the change data on each analyze call; no server-side config store
- Q: Does mandatory-override apply per-finding or per-rule? → A: Per-finding — each individual finding instance carries its own mandatory-override flag, set by the rule's evaluation logic at analysis time

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Understand a Finding's Full Context, Not Just Its Severity (Priority: P1)

A developer reviewing a finding can see which risk dimension it belongs to (e.g. Security, Testing, Architecture) and how confident AgentGuard is in it (a deterministic rule's findings are always fully certain; this foundation exists so a future rule can honestly report partial confidence), in addition to the severity, evidence, and remediation V1 already provides.

**Why this priority**: This is the entire point of "richer analysis" — without dimension and confidence surfaced, a developer still can't tell "is this a hard fact or an inference" or "which part of my PR's risk profile does this affect," which is exactly the gap later phases (contextual analysis, governance) depend on being closed first.

**Independent Test**: Can be fully tested by submitting a PR that trips one or more of the existing five rules and verifying each returned finding includes a risk dimension and a confidence level, with identical score/classification/recommendation to what the same input produced before this feature (no behavior regression for existing rules).

**Acceptance Scenarios**:

1. **Given** a PR that trips the existing SecretDetected rule, **When** the developer views the finding, **Then** it shows a risk dimension (Security), a confidence level indicating certainty, and all fields V1 already provided (severity, evidence, remediation), unchanged in value.
2. **Given** the same PR change data submitted before and after this feature ships, **When** analysis runs on both, **Then** the overall score, classification, and recommendation are identical both times — this feature enriches findings, it does not change what the existing five rules detect or how they score.
3. **Given** a finding produced by one of the five existing (fully deterministic) rules, **When** the developer views its confidence, **Then** it is reported as Certain, never as High, Medium, or Low.

---

### User Story 2 - Tune Risk Thresholds Instead of Accepting One Fixed Scale (Priority: P2)

A person configuring AgentGuard for a repository can adjust the score bands that separate LOW/MEDIUM/HIGH/CRITICAL, instead of being locked to the fixed 0–24/25–49/50–74/75–100 bands V1 shipped with.

**Why this priority**: Different teams have different risk tolerance — V1's fixed bands were a reasonable default, not a universal constant. This is foundational for the governance work planned in later phases (repository-specific policy), but it must exist before that work can build on it. Ranked below User Story 1 because richer findings deliver value even with fixed thresholds, whereas configurable thresholds alone don't improve what a developer sees in a single finding.

**Independent Test**: Can be fully tested by configuring a non-default set of score bands, submitting a PR whose score falls in a range that would classify differently under the old fixed bands versus the new configuration, and verifying the classification and recommendation follow the configured bands.

**Acceptance Scenarios**:

1. **Given** no explicit threshold configuration, **When** a PR is analyzed, **Then** the classification bands are identical to V1's fixed bands (0–24 Low, 25–49 Medium, 50–74 High, 75–100 Critical) — the default behavior is unchanged.
2. **Given** an explicit, valid threshold configuration, **When** a PR is analyzed, **Then** the classification and recommendation are derived from the configured bands instead of the fixed defaults.
3. **Given** an invalid threshold configuration (e.g. overlapping or out-of-order bands), **When** it is supplied, **Then** the system rejects it with a clear error rather than silently falling back to defaults or producing an inconsistent classification.

---

### User Story 3 - A Rule Can Mandatorily Block a Merge, Independent of Score (Priority: P3)

A rule author can mark a specific finding as an unconditional block — the overall recommendation becomes BLOCK_MERGE regardless of the numeric score — rather than relying on severity-weight arithmetic happening to reach the top band.

**Why this priority**: V1 already has one case that behaves this way in practice (a BLOCKER-severity secret finding always caps the score at 100, which always resolves to CRITICAL/BLOCK_MERGE), but that is an emergent side effect of the weight table, not a reusable capability. Future rule packs (e.g. a rule that detects a hardcoded production credential, or a rule enforcing a non-negotiable compliance control) need to force a block explicitly without depending on the scoring arithmetic continuing to line up. Ranked last because it generalizes an existing behavior rather than introducing new user-facing value on its own.

**Independent Test**: Can be fully tested by defining a rule/finding with a mandatory-override flag set at a severity that would not otherwise reach CRITICAL, and verifying the overall recommendation is BLOCK_MERGE regardless of the computed score.

**Acceptance Scenarios**:

1. **Given** a finding with the mandatory-override flag set, **When** the overall result is computed, **Then** the recommendation is BLOCK_MERGE regardless of what the numeric score alone would classify to.
2. **Given** no finding has the mandatory-override flag set, **When** the overall result is computed, **Then** the recommendation is derived purely from the score/threshold bands, exactly as in V1.
3. **Given** a finding with the mandatory-override flag set, **When** the developer views the overall result, **Then** it is clear from the result *why* the merge is blocked (i.e., which finding forced it), not just that it is blocked.

---

### Edge Cases

- What happens to the five existing V1 rules under this feature? They keep their existing rule identifiers, names, severities, and scoring behavior exactly as today; they are additionally classified as deterministic (not contextual) findings with full confidence and an assigned risk dimension, per FR-002/FR-003 below.
- What happens if a future rule fails to declare a risk dimension? The system must reject that rule's registration rather than silently defaulting to an unlabeled or misleading dimension — every finding must be classifiable.
- What happens when both a mandatory-override finding and a normal high-scoring finding are present in the same PR? The result still reports the full computed score and all findings; the override only changes the final recommendation, it does not hide or suppress the score-based classification information.
- What happens to a PR with zero findings under the new model? Score 0, LOW, SAFE_TO_REVIEW, exactly as in V1 — an empty-findings PR is unaffected by any of this feature's additions.
- What happens when a repository has no explicit threshold configuration and no mandatory-override findings occur? The result is byte-for-byte equivalent (aside from the additive new fields) to what V1 already returns — this feature is additive, not a breaking change to existing behavior.

## Requirements *(mandatory)*

### Functional Requirements

**Stable rule identity**

- **FR-001**: System MUST identify every rule (existing and future) by a stable, unique identifier that does not require modifying a shared, closed set of identifiers merely to add a new rule — adding rule N+1 must not require changing the definition of any existing rule's identifier.

**Risk dimensions**

- **FR-002**: System MUST classify every finding under exactly one risk dimension, drawn from this fixed, extensible set: Security, Testing, Compatibility, Architecture, Change Management, Dependencies, Reliability, Configuration.
- **FR-003**: System MUST assign each of the five existing V1 rules a risk dimension consistent with what it actually checks (e.g. the secret-detection rule under Security, the missing-tests rule under Testing) without changing any of their existing detection logic, severity, or scoring.

**Richer finding model**

- **FR-004**: System MUST report, for every finding, whether it was produced by deterministic analysis or contextual/inferential analysis. All five existing V1 rules MUST always be reported as deterministic.
- **FR-005**: System MUST report a confidence level for every finding, using a fixed set of levels (Certain / High / Medium / Low). A deterministic finding MUST always report Certain confidence — this feature does not change what confidence the five existing rules report, since they are exact, not probabilistic.
- **FR-006**: System MUST continue to provide every field V1 already required for a finding (rule id, rule name, severity, explanation, evidence, optional location, remediation) unchanged in meaning.

**Configurable scoring thresholds**

- **FR-007**: System MUST allow the caller to optionally supply a threshold configuration alongside the change data on an analysis request, and MUST apply V1's fixed bands (0–24 Low, 25–49 Medium, 50–74 High, 75–100 Critical) as the default when none is supplied. Threshold configuration is per-request; AgentGuard MUST NOT require any server-side configuration store to support it, consistent with its existing no-database, single-synchronous-operation constraint.
- **FR-008**: System MUST validate a supplied threshold configuration and reject one that is invalid (e.g. non-contiguous, overlapping, or out-of-order bands) with a clear error, rather than accepting it and producing an inconsistent classification.
- **FR-009**: System MUST continue to compute the overall numeric score exactly as in V1 (sum of severity weights, capped at 100) — this feature makes the *classification bands* applied to that score configurable, not the score computation itself.

**Mandatory override**

- **FR-010**: System MUST allow an individual finding instance to be marked as a mandatory override at the time a rule evaluates it (not as a fixed, all-or-nothing property of the rule itself), which forces the overall recommendation to BLOCK_MERGE regardless of the computed score or configured classification bands.
- **FR-011**: System MUST make it possible to determine, from the overall result, which finding(s) triggered a mandatory override when one is present.
- **FR-012**: System MUST NOT apply a mandatory override's effect when no finding has been marked as one — the recommendation MUST then be derived purely from score and thresholds, exactly as in V1.

**Compatibility and scope**

- **FR-013**: System MUST produce identical score, classification, and recommendation for the five existing V1 rules against identical input, both before and after this feature ships, when no explicit threshold configuration is supplied and no mandatory override applies — this feature MUST NOT change V1's existing detection or scoring behavior.
- **FR-014**: System MUST NOT introduce any new detection rule in this feature — this feature is the extensible foundation only; new rule packs are explicitly out of scope here.
- **FR-015**: System MUST expose the new finding fields (risk dimension, confidence, deterministic-vs-contextual classification) and any active mandatory override through the same API surfaces that already return analysis results (both the manually-submitted and the GitHub-PR-reference analysis endpoints), so no analysis consumer needs a second call to get the fuller picture.
- **FR-016**: The UI MUST display each finding's risk dimension and confidence alongside the fields it already shows, and MUST visibly indicate when the overall recommendation was forced by a mandatory override rather than derived from the score.

### Key Entities

- **Risk Dimension**: A category a finding belongs to — Security, Testing, Compatibility, Architecture, Change Management, Dependencies, Reliability, or Configuration — independent of severity. Lets findings be grouped and reasoned about by *what kind* of risk they represent, not just *how severe*. Three dimensions (Dependencies, Reliability, Configuration) have no rule assigned to them yet in this phase; they exist so later rule packs already have a home to classify under.
- **Finding Kind**: Whether a finding was produced by deterministic analysis (an exact, rule-based check) or contextual/inferential analysis (not introduced by any rule in this feature, but the classification itself is part of this foundation).
- **Confidence**: How certain AgentGuard is in a given finding — one of a fixed set of levels (Certain / High / Medium / Low). Always Certain for deterministic findings; exists as a foundation for future non-deterministic rules to report honestly.
- **Threshold Configuration**: The score bands used to map a numeric score to a risk classification — defaults to V1's fixed bands, may be overridden per-request by the caller alongside the submitted change data. Never stored server-side.
- **Mandatory Override**: A flag an individual finding instance can carry (set by the producing rule's own evaluation logic, not fixed at the rule's definition) that forces the overall recommendation to BLOCK_MERGE independent of score.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of findings from the five existing V1 rules include a risk dimension and a confidence level, with zero change to their severity, evidence, or contribution to the overall score, versus V1's pre-existing behavior.
- **SC-002**: Identical PR change data, with no explicit threshold configuration and no mandatory override present, produces an identical score, classification, and recommendation before and after this feature ships, 100% of the time.
- **SC-003**: 100% of analysis requests that supply a threshold configuration classify strictly according to those configured bands; 100% that omit one use the documented default bands.
- **SC-004**: 100% of results containing a mandatory-override finding resolve to BLOCK_MERGE, and the result makes it possible to identify which finding caused it, regardless of the computed score.
- **SC-005**: A developer can identify a finding's risk dimension and confidence from the analysis result or the UI without needing to consult documentation or source code to interpret it.

## Assumptions

- This feature only touches `AgentGuard.Core`'s finding/scoring model, the API's response shape, and the UI's display of results — it does not add, remove, or change the behavior of any of the five existing V1 detection rules.
- "Contextual" findings (non-deterministic, inference-based) are given a place in the data model by this feature, but no rule that actually produces a contextual finding is introduced here — that is explicitly deferred to a later phase, and this feature makes no commitment yet about *how* a future contextual rule would be implemented (including whether it would involve an LLM, which would need to be reconciled with AgentGuard's existing no-LLM constraint before that later phase can proceed).
- Threshold configuration and mandatory overrides are data-model and evaluation-logic foundations in this phase; a full external policy-configuration surface (e.g. per-repository config files, an admin UI) is out of scope here and deferred to the governance phase already planned.
- The existing five rules' new risk-dimension assignments are a reasonable, human-reviewable default mapping, not something this feature needs to make independently configurable.
- No new persistence is introduced — analysis remains a single synchronous, stateless operation per request, consistent with AgentGuard's existing constraints.
