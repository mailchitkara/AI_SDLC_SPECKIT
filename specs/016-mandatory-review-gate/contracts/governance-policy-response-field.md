# Contract: `recommendationForcedByGovernancePolicy` response field

Adds one boolean field to `RiskAnalysisResultResponse` — the shape returned by both existing analysis endpoints (`POST /api/pr-risk-analysis`, `POST /api/pr-risk-analysis/from-reference`). No request-shape change; no new endpoint.

## Response field

```json
{
  "score": 20,
  "classification": "LOW",
  "recommendation": "HUMAN_REVIEW_REQUIRED",
  "recommendationForcedByOverride": false,
  "recommendationForcedByGovernancePolicy": true,
  "checks": [...],
  "findings": [...]
}
```

| Field | Type | Notes |
|---|---|---|
| `recommendationForcedByGovernancePolicy` | boolean | `true` only when a configured mandatory-review-dimension finding actually raised the recommendation to `HUMAN_REVIEW_REQUIRED` — i.e. the pre-floor recommendation was below that. `false` whenever no such finding exists, or one exists but the recommendation was already at `HUMAN_REVIEW_REQUIRED`/`BLOCK_MERGE` through the existing score/override pipeline (research.md §3). |

## Relationship to the existing `recommendationForcedByOverride` field

The two fields are independent and can both be `false`, either one `true`, or (in principle, though the ceilings make the practical overlap narrow) both `true` is impossible by construction — `recommendationForcedByOverride: true` always implies the pre-floor recommendation was already `BLOCK_MERGE`, which is above this policy's `HUMAN_REVIEW_REQUIRED` ceiling, so `recommendationForcedByGovernancePolicy` is always `false` whenever `recommendationForcedByOverride` is `true` (spec.md Edge Case 3).

## Behavior with no policy configured

When `mandatoryReviewDimensions` is absent or empty in the policy file (or no policy file is configured at all), `recommendationForcedByGovernancePolicy` is always `false` — identical to every response before this feature existed, aside from the field's mere presence with a constant `false` value.

## Backward compatibility

Purely additive — the same compatibility guarantee `005-risk-engine-foundation` established for `recommendationForcedByOverride` and `015-policy-as-code` established for the policy file itself. Any existing consumer ignoring unknown response fields is unaffected.
