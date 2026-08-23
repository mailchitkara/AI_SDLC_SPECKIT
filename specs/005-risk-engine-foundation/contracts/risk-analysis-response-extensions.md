# Contract: Extensions to the Existing Risk Analysis Endpoints

This feature adds no new endpoint. It extends the request/response shapes of the two that already exist:
- `POST /api/pr-risk-analysis` (manual submission, from `001-pr-risk-analysis-v1`)
- `POST /api/pr-risk-analysis/from-reference` (GitHub PR import, from `003-github-pr-import`)

Both gain the identical additions below. Everything not listed here is unchanged.

## Request addition (both endpoints)

An optional `thresholds` field, alongside the existing fields each endpoint already accepts:

```json
{
  "...": "... existing fields unchanged ...",
  "thresholds": { "lowMax": 24, "mediumMax": 49, "highMax": 74 }
}
```

Omitting `thresholds` entirely is valid and uses V1's fixed bands (`lowMax: 24, mediumMax: 49, highMax: 74`) — existing callers need no changes.

**Validation** (`400 Bad Request`, same error-response shape each endpoint already uses for its own validation failures):
- All three fields must be present if `thresholds` is present at all (no partial configuration).
- Must satisfy `0 <= lowMax < mediumMax < highMax < 100`.

## Response addition (both endpoints, `200 OK` body)

```json
{
  "...": "... existing fields unchanged ...",
  "recommendationForcedByOverride": false,
  "findings": [
    {
      "...": "... existing finding fields unchanged ...",
      "dimension": "SECURITY",
      "confidence": "CERTAIN",
      "kind": "DETERMINISTIC",
      "mandatoryOverride": false
    }
  ]
}
```

`dimension` is one of: `SECURITY | TESTING | COMPATIBILITY | ARCHITECTURE | CHANGE_MANAGEMENT | DEPENDENCIES | RELIABILITY | CONFIGURATION`.
`confidence` is one of: `CERTAIN | HIGH | MEDIUM | LOW`.
`kind` is one of: `DETERMINISTIC | CONTEXTUAL` (this phase's five rules always produce `DETERMINISTIC`).

For the five existing rules, `confidence` is always `CERTAIN`, `kind` is always `DETERMINISTIC`, and `mandatoryOverride` is always `false` (research.md §3) — `recommendationForcedByOverride` is therefore always `false` for any result that only contains findings from the five existing rules, regardless of `thresholds`.

## Compatibility guarantee (FR-013)

For identical change data, with no `thresholds` supplied: `score`, `classification`, `recommendation`, and every existing field's value are byte-for-byte identical to what the endpoint returned before this feature — the additions above are pure extensions, never a change to existing field values.

## Contract test coverage (for `tasks.md` to enumerate)

- Existing request with no `thresholds` field → `200`, `recommendationForcedByOverride: false`, every finding carries a dimension/confidence/kind consistent with the table above, and `score`/`classification`/`recommendation` match what the same input produced before this feature (regression check against a recorded pre-feature fixture).
- Request with a valid `thresholds` object → classification follows the supplied bands, not the defaults.
- Request with an invalid `thresholds` object (partial, out-of-order, out-of-range) → `400`.
- A change set that trips `SecretDetected` → `recommendationForcedByOverride: false` (it already reaches `BLOCK_MERGE` via score, not override — research.md §3), and that finding's `mandatoryOverride` is `false`.
- (Core-level, not endpoint-level) A synthetic finding with `MandatoryOverride: true` at a severity that would not otherwise reach Critical → `recommendation: BLOCK_MERGE`, `recommendationForcedByOverride: true`, regardless of the configured or default thresholds.
