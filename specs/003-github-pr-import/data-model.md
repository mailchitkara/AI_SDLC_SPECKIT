# Phase 1 Data Model: GitHub PR Import for AgentGuard

New types added to `AgentGuard.Api` (namespaces per `plan.md`'s Project Structure). Existing `AgentGuard.Core` types (`PullRequestChangeSet`, `ChangedFile`, `RiskAnalysisResult`, `Finding`, etc.) are reused unchanged — this feature only adds a new way to construct a `PullRequestChangeSet`, plus new response fields describing the import step itself.

## PR Reference (request-side)

Maps to spec's **PR Reference** key entity. Backing type: `PrReferenceAnalysisRequest` (`AgentGuard.Api.Contracts`).

| Field | Type | Required | Notes |
|---|---|---|---|
| `PrUrl` | `string?` | one-of, with `Owner`/`Repository`/`PrNumber` | e.g. `https://github.com/{owner}/{repo}/pull/{number}`. Parsed via the same regex shape validated ad hoc during manual testing (`^https?://github\.com/([^/]+)/([^/]+)/pull/(\d+)`). |
| `Owner` | `string?` | one-of | Used only when `PrUrl` is absent. |
| `Repository` | `string?` | one-of | Used only when `PrUrl` is absent. |
| `PrNumber` | `int?` | one-of | Used only when `PrUrl` is absent. |
| `Credential` | `string?` | no | Per FR-006/FR-007 — forwarded to GitHub as `Authorization: Bearer {Credential}` when present, held only for the duration of the request, never logged (see `plan.md` Constraints) or included in any response. |

Validation (FR-001, FR-010): exactly one of {`PrUrl` present} or {`Owner`+`Repository`+`PrNumber` all present} — anything else (both, neither, partial trio, malformed `PrUrl`) is a `400` `invalid_reference` error before any GitHub call is made.

## Retrieved Change File

Maps to spec's **Retrieved Change File** key entity. Internal to `GitHubPullRequestClient` — assembled into existing `AgentGuard.Core.ChangedFile` records before reaching `AgentGuardAnalyzer`, so `AgentGuard.Core` never sees a GitHub-specific type.

| Field | Type | Notes |
|---|---|---|
| `Path` | `string` | Current path (post-rename, if renamed). |
| `GitHubStatus` | `"added" \| "removed" \| "modified" \| "renamed"` | Mapped to `AgentGuard.Core.ChangeType` via `GitHubFileStatusMapping` (see `research.md` — reuses existing enum, no new values). |
| `OldContent` | `string?` | `null` when not retrievable (FR-009) or the file was added. |
| `NewContent` | `string?` | `null` when not retrievable (FR-009) or the file was removed. |
| `FullyEvaluated` | `bool` | `false` when either applicable content field could not be retrieved (per `research.md` §5) — drives the response's `PartiallyEvaluatedFiles` list; does not block analysis (FR-009). |
| `LinesAdded` / `LinesDeleted` | `int` | From GitHub's `additions`/`deletions` on the files-list response — used regardless of `FullyEvaluated`, since size-based rules (`LargeChangeSize`) don't need file content. |

## Risk Analysis Result (response-side, extended)

Existing `RiskAnalysisResultResponse` (`AgentGuard.Api.Contracts`) gains one new field for this feature's responses (present, possibly empty, only on this endpoint — the manually-submitted `/api/pr-risk-analysis` endpoint continues to omit it or always returns it empty, implementer's choice, since manual submissions have no retrieval step to fail):

| Field | Type | Notes |
|---|---|---|
| `PartiallyEvaluatedFiles` | `List<PartiallyEvaluatedFile>` | Empty when every file was fully retrieved (spec User Story 2, Acceptance Scenario 2). |

`PartiallyEvaluatedFile`:

| Field | Type | Notes |
|---|---|---|
| `Path` | `string` | |
| `Reason` | `"not_retrievable"` | Single reason value for V1 — see `research.md` §5 on why a more specific cause isn't needed. |

## Import Error (response-side, error cases)

Maps to spec's **Import Error** key entity. Backing type: `ImportErrorResponse` (`AgentGuard.Api.Contracts`), returned with the HTTP status codes decided in `research.md` §3.

| Field | Type | Notes |
|---|---|---|
| `ErrorType` | `"invalid_reference" \| "not_found_or_no_access" \| "rate_limited"` | Machine-readable; matches the spec's **Import Error** entity description exactly. |
| `Message` | `string` | Human-readable explanation; for `not_found_or_no_access`, explicitly mentions that supplying a credential and retrying may resolve it (FR-010a). |
| `RetryableWithCredential` | `bool` | `true` only for `not_found_or_no_access`; `false` for `invalid_reference` (no credential fixes a malformed URL) and `rate_limited` (retrying immediately with a credential doesn't bypass an in-progress rate limit, though a credential does raise the caller's *subsequent* limit). |

## State / lifecycle note

None of the above is persisted. `PrReferenceAnalysisRequest` → `IGitHubPullRequestClient` calls → (list of `ChangedFile` + `PartiallyEvaluatedFile`) → `AgentGuardAnalyzer.Analyze(...)` (unchanged) → `RiskAnalysisResultResponse` (extended) all live and die within one HTTP request, consistent with FR-007/FR-008.
