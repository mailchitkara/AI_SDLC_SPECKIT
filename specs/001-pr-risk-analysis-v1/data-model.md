# Phase 1 Data Model: AgentGuard V1 - PR Risk Analysis

Entities correspond to the spec's Key Entities section, expanded with the concrete fields/types needed to satisfy the Functional Requirements. All types live in `AgentGuard.Core` and are mirrored as request/response DTOs in `AgentGuard.Api` and as TypeScript types in `frontend/src/types/riskAnalysis.ts` (see [contracts/openapi.yaml](./contracts/openapi.yaml)).

## PullRequestChangeSet

The input to one analysis run (FR-001).

| Field | Type | Notes |
|---|---|---|
| `repositoryName` | string, required, non-empty | FR-001, FR-016 (displayed in UI) |
| `prNumber` | integer, required, > 0 | FR-001, FR-016 |
| `prTitle` | string, required, non-empty | FR-001, FR-016 |
| `changedFiles` | `ChangedFile[]`, required, may be empty | Empty list is valid (Edge Case: no file changes → zero score, all checks pass) |

**Validation**: missing/empty `repositoryName` or `prTitle`, `prNumber <= 0`, or a missing `changedFiles` field are rejected with a clear validation error (FR-002) — an *empty* `changedFiles` array is valid input, not an error.

## ChangedFile

One entry in a `PullRequestChangeSet` (research.md §1).

| Field | Type | Notes |
|---|---|---|
| `path` | string, required | Used for file classification (FR-004) and location on findings (FR-008) |
| `changeType` | enum: `ADDED`, `MODIFIED`, `DELETED`, `RENAMED` | Governs which content fields are populated |
| `oldContent` | string, nullable | Present for `MODIFIED`/`DELETED`/`RENAMED`; null for `ADDED` |
| `newContent` | string, nullable | Present for `ADDED`/`MODIFIED`/`RENAMED`; null for `DELETED` |
| `linesAdded` | integer, >= 0 | Feeds the Large Change Size rule (FR-003) |
| `linesDeleted` | integer, >= 0 | Feeds the Large Change Size rule (FR-003) |

## Rule

One of the five fixed V1 rules (FR-003..FR-007). Not part of the request/response payload — a static, in-code catalog inside `AgentGuard.Core/Rules/`.

| Field | Type | Notes |
|---|---|---|
| `id` | string, fixed enum of 5 values | e.g. `LARGE_CHANGE_SIZE`, `MISSING_RELATED_TESTS`, `API_CONTRACT_BREAKING_CHANGE`, `ARCHITECTURE_VIOLATION`, `SECRET_DETECTED` |
| `name` | string | Human-readable, shown in UI and findings (FR-008, FR-011) |
| `defaultSeverity` | `Severity` | `LOW`, `MEDIUM`, `HIGH`, `HIGH`, `BLOCKER` respectively (FR-003..FR-007) |

## Severity (enum)

Fixed 5-value set with associated scoring weight (FR-008, FR-012).

| Value | Weight |
|---|---|
| `INFO` | 0 |
| `LOW` | 10 |
| `MEDIUM` | 20 |
| `HIGH` | 35 |
| `BLOCKER` | 100 |

## Finding

One issue detected by a rule against a `PullRequestChangeSet` (FR-008..FR-010).

| Field | Type | Notes |
|---|---|---|
| `ruleId` | string (Rule.id) | FR-008 |
| `ruleName` | string (Rule.name) | FR-008 |
| `severity` | `Severity` | FR-008, drives weight (FR-012) |
| `explanation` | string, required | Human-readable reason (FR-008) |
| `evidence` | string, required | For `SECRET_DETECTED` findings, MUST already be masked before this object exists (FR-010) — never populated with an unmasked value |
| `location` | string, nullable | File path (+ optional line) when determinable; **omitted** (`null`), not a placeholder string, when not applicable (FR-009) |
| `remediation` | string, required | Suggested fix (FR-008) |

**Validation**: `evidence` for a `SECRET_DETECTED` finding MUST NOT equal or contain the raw matched secret value — enforced by constructing the finding only from the already-masked value (research.md §6), and verified in `AgentGuard.Core.Tests` by asserting the raw fixture secret never appears in the produced `Finding`.

## CheckResult

Pass/fail status of one rule for one analysis run (FR-011).

| Field | Type | Notes |
|---|---|---|
| `ruleId` | string (Rule.id) | |
| `ruleName` | string (Rule.name) | |
| `passed` | boolean | `true` when the rule produced zero findings for this run, `false` otherwise |

## RiskClassification (enum)

Derived from the capped score (FR-015).

| Value | Score band |
|---|---|
| `LOW` | 0–24 |
| `MEDIUM` | 25–49 |
| `HIGH` | 50–74 |
| `CRITICAL` | 75–100 |

## Recommendation (enum)

Derived 1:1 from `RiskClassification` (FR-016).

| Classification | Recommendation |
|---|---|
| `LOW` | `SAFE_TO_REVIEW` |
| `MEDIUM` | `REVIEW_RECOMMENDED` |
| `HIGH` | `HUMAN_REVIEW_REQUIRED` |
| `CRITICAL` | `BLOCK_MERGE` |

## RiskAnalysisResult

The full output of one analysis run — what the API returns and the UI renders in full (FR-013..FR-027).

| Field | Type | Notes |
|---|---|---|
| `repositoryName` | string | Echoed from input (FR-016/UI display) |
| `prNumber` | integer | Echoed from input |
| `prTitle` | string | Echoed from input |
| `score` | integer, 0–100 | Sum of all finding weights, capped at 100 (FR-013) |
| `classification` | `RiskClassification` | Derived from `score` (FR-015) |
| `recommendation` | `Recommendation` | Derived from `classification` (FR-016) |
| `checks` | `CheckResult[]`, exactly 5 entries | One per rule, in fixed rule order (FR-011) |
| `findings` | `Finding[]`, may be empty | All findings from all rules, sorted by severity (BLOCKER→INFO) then `ruleId` for stable output (research.md §7) |

**State/derivation invariants** (no persisted state — these are computed, not stored):

- `score == min(100, sum(weight(f.severity) for f in findings))` (FR-013)
- If any `f in findings` has `severity == BLOCKER`, then `score == 100`, `classification == CRITICAL`, `recommendation == BLOCK_MERGE` (FR-014, FR-017)
- `checks.count == 5` always, one per fixed rule, regardless of how many findings exist (FR-011)
- `findings == []` and `checks.All(c => c.passed)` together imply `classification == LOW` and `recommendation == SAFE_TO_REVIEW`
