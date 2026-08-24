# Contract: `vulnerableDependencies` request field

Adds one optional field to both existing analysis endpoints' request bodies — `POST /api/pr-risk-analysis` (`PullRequestChangeSetRequest`) and `POST /api/pr-risk-analysis/from-reference` (`PrReferenceAnalysisRequest`). No new endpoint, no response-shape change.

## Field

```json
"vulnerableDependencies": [
  {
    "packageName": "left-pad",
    "version": "1.3.0",
    "severity": "HIGH",
    "advisoryId": "GHSA-xxxx-xxxx-xxxx",
    "advisoryUrl": "https://github.com/advisories/GHSA-xxxx-xxxx-xxxx"
  }
]
```

| Field | Required | Notes |
|---|---|---|
| `packageName` | Yes | Non-empty string. |
| `version` | Yes | Non-empty string. |
| `severity` | Yes | One of `LOW`, `MODERATE`, `HIGH`, `CRITICAL` (case-sensitive, matching the API's existing enum-string convention). Any other value is a validation error. |
| `advisoryId` | No | Free-form identifier (e.g. a GHSA or CVE id). Included in finding evidence when present. |
| `advisoryUrl` | No | Free-form URL. Included in finding evidence when present. |

## Behavior

- Omitting the field entirely, or supplying `[]`, is valid and produces zero `VULNERABLE_DEPENDENCY_DETECTED` findings — behaviorally identical to before this feature existed (spec.md Acceptance Scenario 3).
- Each well-formed entry produces exactly one finding — no deduplication across entries with the same `packageName`/`version` (spec.md Edge Cases).
- A malformed entry (missing `packageName`/`version`, or an unrecognized `severity`) fails request validation for the *whole* request with a 400, the same way an invalid `changedFiles[i].changeType` already does — it does not silently drop just that one entry.

## Response

No response-shape change. `VULNERABLE_DEPENDENCY_DETECTED` findings appear in the existing `findings` array using the exact same `FindingResponse` shape every other rule already produces; the rule appears in the existing `checks` array like every other rule.

## Backward compatibility

Purely additive. Any existing request that doesn't set this field behaves exactly as it did before this feature — this is the same compatibility guarantee `005-risk-engine-foundation` established for `thresholds`.
