# Phase 0 Research: Mandatory Review Gate by Risk Dimension

No `[NEEDS CLARIFICATION]` markers. This records the technology/design decisions.

## 1. A floor applied after scoring, not a new scoring input

**Decision**: `RiskEngine.Evaluate` computes score/classification/override-driven recommendation exactly as it does today, then applies one additional step: if any finding's dimension is in the configured mandatory-review set, raise the recommendation to at least `HumanReviewRequired` (never lower it).

**Rationale**: This mirrors `MandatoryOverride`'s own existing relationship to the pipeline exactly (a post-processing step over already-computed findings, not a change to the weight-sum arithmetic) — a proven, already-shipped shape for "some findings should influence the *recommendation* directly, independent of the numeric score." Two ceilings (`HumanReviewRequired` from this policy, `BlockMerge` from `MandatoryOverride`) coexist cleanly as two independent floors applied to the same underlying classification-derived recommendation.

**Alternatives considered**: Folding this into the score itself (e.g. adding synthetic weight for a matching dimension) — rejected; it would make the *numeric* score less meaningful (two PRs with the same real risk could get different scores depending on unrelated policy configuration) and wouldn't guarantee the floor the way a direct recommendation override does (a large enough default threshold could still theoretically classify below `HumanReviewRequired` even with extra weight added).

## 2. `Recommendation` is already a linearly-ordered enum — floor via `Math.Max`

**Decision**: `Recommendation` (`SafeToReview` < `ReviewRecommended` < `HumanReviewRequired` < `BlockMerge`) is compared/raised via integer `Math.Max` on the underlying enum values.

**Rationale**: The enum was already declared in exactly this order for `RiskClassification`'s existing fixed mapping — reusing that ordering for a second, independent floor is a natural fit requiring no new comparison logic.

## 3. Distinguishing this policy from `MandatoryOverride` in the response (FR-004)

**Decision**: A new `RecommendationForcedByGovernancePolicy` boolean, set `true` only when this policy's floor is the reason the *final* recommendation is `HumanReviewRequired` or higher — specifically, `true` when a matching-dimension finding exists AND the pre-floor recommendation was below `HumanReviewRequired` (i.e., this policy actually changed the outcome, not merely "a matching finding exists" regardless of effect — matching spec.md Acceptance Scenario 2 and Edge Case 3, which both require the flag to reflect actual causation, not mere applicability).

**Rationale**: A caller (a human reviewer, or downstream tooling) needs to know *why* a recommendation landed where it did — conflating "this policy applied" with "this policy's own override already accounts for it, independent of whether this floor was even needed" would make the field misleading in the (already-existing) `MandatoryOverride` case, where the answer should be `false` even if a matching dimension also happens to be present.

**Alternatives considered**: A single combined "was this forced by any policy" boolean, folding `MandatoryOverride` and this new mechanism together — rejected; they have different ceilings and different owners (a rule's own decision vs. an operator's cross-cutting policy), and `RecommendationForcedByOverride` is already a shipped, tested field from `005` that must not change meaning.

## 4. Extending `015`'s policy file, not a new file/variable

**Decision**: A third top-level section, `mandatoryReviewDimensions` (an array of dimension strings, using the same wire-format names `EnumMappings.ToApiString` already defines, e.g. `"BUSINESS_CRITICALITY"`), added to the same JSON document `AGENTGUARD_POLICY_FILE_PATH` already points to.

**Rationale**: Same operator, same lifecycle, same startup-time loading as the other two sections — no reason to introduce a second file or variable for what is conceptually the same "how is this AgentGuard deployment configured" artifact.

**Alternatives considered**: A separate `AGENTGUARD_GOVERNANCE_POLICY_PATH` — rejected as unnecessary indirection, same reasoning `015`'s research.md §1 already used for combining the first two sections into one file.

## 5. Unrecognized dimension name: loud failure, matching `015`'s established philosophy

**Decision**: A dimension string in `mandatoryReviewDimensions` that doesn't match any known `RiskDimension` value causes the same startup failure as any other malformed policy-file content.

**Rationale**: A typo'd dimension name (e.g. `"BUSINESS_CRITICALLITY"`) would otherwise silently mean the gate the operator explicitly configured never applies — the exact "false sense of coverage" risk `015-policy-as-code`'s research.md §3 already established as the reason malformed content must fail loudly, not silently.

## 6. No self-tripping-pattern risk

**Decision**: No proactive obscuring needed — this feature has no text-content regex pattern of its own; it's a set-membership check against a fixed enum.
