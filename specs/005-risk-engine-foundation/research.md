# Phase 0 Research: Risk Engine Foundation

No `[NEEDS CLARIFICATION]` markers remain — the four architecturally significant decisions were already resolved in `/speckit-clarify` (confidence shape, dimension set, threshold-config location, mandatory-override attachment point). This records the remaining technology/design decisions needed to actually build it.

## 1. Rule identity: enum → stable string-backed type

**Decision**: Replace the closed `enum RuleId` with a small `readonly record struct RuleId(string Value)`, constructed with the exact same SCREAMING_SNAKE_CASE strings the API already exposes (e.g. `new RuleId("LARGE_CHANGE_SIZE")`).

**Rationale**: FR-001 requires that adding rule N+1 never requires modifying the definition of any existing rule's identifier. A C# `enum` is a closed set — every new value is a change to the same shared type declaration, which is exactly the kind of shared-file bottleneck the user's own stated branching strategy (Section 27: parallel rule-pack branches) would collide on. A string-backed struct lets each rule declare its own identity independently. Using the struct (not a bare `string`) keeps compile-time type safety at call sites (`Rule.Id`, `Finding.RuleId`) instead of stringly-typed parameters. Choosing the *same* string values already used at the wire boundary (`EnumMappings.ToApiString` today) means `ToApiString` becomes a trivial `.Value` passthrough instead of a switch statement, and — critically — the JSON the API returns for the five existing rules is byte-for-byte unchanged (FR-013).

**Alternatives considered**: Keep the enum and add new values to it as later phases add rules — rejected, it directly reproduces the shared-bottleneck problem FR-001 exists to avoid. A fully external rule registry (e.g. rules declared in JSON/YAML rather than C#) — rejected as out of scope for this phase; FR-014 explicitly excludes adding new rules here, so there's no immediate need for a non-code rule-authoring surface, and the spec's own Assumptions defer that kind of decision to later phases.

## 2. Where Dimension/Confidence/Kind live: mirror the existing Severity pattern

**Decision**: `Rule` (the static catalog entry) gains a `DefaultDimension`; `Finding` (the actual instance a rule produces) gains `Dimension`, `Confidence`, and `Kind` fields, populated by each rule's own `Evaluate` method.

**Rationale**: This exactly mirrors how `Severity` already works today — `Rule.DefaultSeverity` exists, but the *actual* severity on a produced `Finding` is what the rule's evaluation logic sets (in V1, always equal to the default, but the model already supports per-instance variation). Reusing an established pattern rather than inventing a new one keeps the five existing rule implementations' shape of change minimal and predictable.

**Alternatives considered**: Only on `Rule`, not `Finding` — rejected because `Confidence` in particular is specified (FR-005) as something a *future* non-deterministic rule needs to vary per-finding (e.g. one contextual finding more confident than another from the same rule), which a rule-level-only field couldn't express.

## 3. Mandatory override and the five existing rules

**Decision**: All five existing V1 rules always produce `MandatoryOverride: false`. `SecretDetected` is *not* changed to set it `true`, even though its BLOCKER severity already forces BLOCK_MERGE via the existing weight table.

**Rationale**: FR-013 requires the five existing rules to be unchanged. `SecretDetected` already reaches BLOCK_MERGE today purely through score arithmetic (BLOCKER weight 100, capped score, CRITICAL band) — setting `MandatoryOverride: true` on it would not change the user-visible score/classification/recommendation, but it would change the new `RecommendationForcedByOverride` result field's value for that rule specifically, which is an unnecessary, avoidable coupling between "a rule that happens to already reach the top band" and "a rule that explicitly bypasses banding." Keeping the override mechanism demonstrated only by dedicated tests (not retrofitted onto production rule behavior) keeps its meaning unambiguous: `RecommendationForcedByOverride` becomes `true` if and only if a rule *chose* to bypass scoring, never as an incidental side effect of a rule's existing severity happening to top out the scale.

**Alternatives considered**: Set `SecretDetected`'s findings to `MandatoryOverride: true` — rejected per above; it would be redundant (already blocks via score) and would muddy what the new field actually signals to a consumer.

## 4. Threshold validation location

**Decision**: Validate a supplied `ThresholdConfiguration` at the `AgentGuard.Api` contracts layer (a new validator, following the existing `PullRequestChangeSetValidator` pattern), returning `400` with a clear message for an invalid configuration. `RiskEngine.Evaluate` itself continues to assume a valid, already-checked configuration and remains a pure function with no validation branching of its own.

**Rationale**: Matches the codebase's existing convention exactly — validation lives in `Contracts/`, right before the analyzer is invoked, not inside `AgentGuard.Core`'s pure evaluation logic. Keeps `RiskEngine.Evaluate`'s documented contract ("no I/O, no randomness... identical input always produces an identical result," FR-013/existing doc comment) unchanged in spirit — it stays a function of *valid* inputs, same as it already assumes valid `Severity` values today.

**Alternatives considered**: Validate inside `RiskEngine` and throw an exception the API layer catches — rejected as inconsistent with how the existing `PullRequestChangeSetValidator` → `AgentGuardAnalyzer.Analyze` flow already separates "reject bad input" from "compute the pure result."

## 5. Backward-compatible method signatures

**Decision**: `AgentGuardAnalyzer.Analyze` and `RiskEngine.Evaluate` both gain a new optional parameter (`ThresholdConfiguration? thresholds = null`) rather than a new overload or a breaking signature change.

**Rationale**: Every existing call site (both current endpoints, all existing tests) continues to compile and behave identically with zero changes, directly satisfying FR-013's "identical input → identical output" guarantee at the code level, not just the API's JSON level.

**Alternatives considered**: A new `AnalysisOptions` parameter object bundling thresholds and any future per-request option — considered, but rejected as speculative for a single optional field; easy to introduce later if a second per-request option appears, per the project's stated preference against premature abstraction.
