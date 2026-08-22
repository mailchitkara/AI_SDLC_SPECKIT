# Phase 0 Research: GitHub PR Import for AgentGuard

No `[NEEDS CLARIFICATION]` markers remain in the Technical Context. This records the technology-choice research behind it.

## 1. Raw `HttpClient` vs. a GitHub SDK (Octokit.NET)

**Decision**: A small typed `HttpClient` (`GitHubPullRequestClient`) calling GitHub's REST API directly with `System.Text.Json`, no third-party SDK.

**Rationale**: This feature needs exactly three GitHub endpoints (PR metadata, PR files list, file contents at a ref) — a small, stable surface. Octokit.NET is a large, actively-versioned dependency whose full typed surface (issues, releases, actions, webhooks, etc.) is irrelevant here; pulling it in for three calls adds a dependency-update burden disproportionate to the value. A typed `HttpClient` also matches AgentGuard's existing minimalism (no ORM, no DI-heavy frameworks beyond what ASP.NET Core ships with) and keeps the GitHub-specific JSON shapes fully under this project's own control, which matters since FR-009's "not fully evaluated" and FR-010a's "not-found-or-no-access" outcomes need precise handling of GitHub's actual response shapes (missing `encoding` field, 404 vs. 403 semantics) that a general-purpose SDK would otherwise abstract away.

**Alternatives considered**: Octokit.NET — rejected per above. `gh` CLI shelled out from the API process — rejected outright as inappropriate for a web service (process-spawn overhead, no benefit over a direct HTTP call, and out of place next to the existing pure-C# stack).

## 2. Testability: abstracting GitHub access

**Decision**: `IGitHubPullRequestClient` interface, with `GitHubPullRequestClient` as the real `HttpClient`-based implementation registered via `AddHttpClient<IGitHubPullRequestClient, GitHubPullRequestClient>()`. Endpoint tests use a fake implementation.

**Rationale**: The existing `AgentGuard.Api.Tests` project already tests `PrRiskAnalysisEndpoint` without any external dependency; the new endpoint should be testable the same way; hitting the real GitHub API in unit tests would make CI flaky (network dependency, rate limits shared across every CI run) and slow. This directly enables testing FR-009 (partial evaluation) and FR-010/FR-010a (error outcomes) deterministically with fixture responses, including cases (like the ambiguous private-repo 404) that would be awkward to reproduce against the real API on demand.

**Alternatives considered**: Recording/replaying real HTTP responses (e.g., VCR-style cassette testing) — more faithful to real GitHub responses, but adds a new test-tooling dependency for marginal benefit over hand-written fixtures, given the response shapes this feature actually depends on are small and well-documented. May be worth revisiting if GitHub's response shape proves to have more edge cases than expected during implementation.

## 3. How results and errors are represented on the wire

**Decision**: Reuse the existing `200 OK` + `RiskAnalysisResultResponse` shape for a completed analysis (extended with a `partiallyEvaluatedFiles` field), and use distinct HTTP status codes for the three error categories: `400 Bad Request` (malformed reference — FR-010), `404 Not Found` (not-found-or-no-access — FR-010a), `429 Too Many Requests` (rate-limited — FR-010). Each error response carries a small typed body (`ImportErrorResponse`) with a machine-readable `errorType` and a human-readable `message`.

**Rationale**: This lets a caller (including `004-github-actions-pr-gate`'s `analyze.sh`) branch on HTTP status alone without parsing a response-envelope discriminator field, matching REST conventions and the pattern already established by the existing endpoint's `400` + `ValidationErrorResponse` for validation errors. Using `404` for the ambiguous not-found-or-no-access case is a deliberate, honest choice: it mirrors GitHub's own behavior for the same ambiguity (see spec's User Story 3 rationale) rather than inventing a false certainty AgentGuard doesn't have.

**Alternatives considered**: A single `200 OK` envelope with an `"outcome": "completed" | "error"` discriminator for every case — considered because it's what `004`'s forward-declared contract initially assumed, but rejected in favor of proper status codes, which are more idiomatic for this codebase's existing pattern and let HTTP-level tooling (retries, logging, monitoring) work without inspecting bodies. `004`'s forward contract is reconciled to this decision (see that feature's `contracts/analyze-by-reference.md`, updated alongside this plan).

## 4. Fetching a PR's full file list (pagination)

**Decision**: Follow GitHub's `Link` response header to walk every page of `GET /repos/{owner}/{repo}/pulls/{number}/files` (100 files per page, GitHub's max), with no artificial cap on page count — bounded only by the endpoint's overall request timeout.

**Rationale**: The spec explicitly requires large PRs to be handled "on a best-effort basis... no hard cap... to reject outright" (Assumptions) and "analysis still completes... files that cannot practically be retrieved are reported as not fully evaluated rather than causing the whole request to fail" (Edge Cases) — an artificial page-count cap would silently drop files from analysis in a way indistinguishable from those two documented, intentional cases, which would be misleading. Time, not page count, is the natural bound already required by FR-010 (rate-limiting) and the overall request lifecycle.

**Alternatives considered**: Capping at a fixed number of pages (e.g., 10) for safety — rejected as arbitrary and against the spec's explicit best-effort stance; a slow response is already handled by the caller's own timeout (e.g., `004`'s `timeout-seconds`), which is a more honest bound than a silently-applied file-count ceiling.

## 5. Detecting "file content could not be retrieved" (FR-009)

**Decision**: A file is marked not-fully-evaluated when the GitHub Contents API response for it is missing (404 at that ref — e.g., a submodule reference), lacks a `base64` `encoding` (GitHub omits inline content for files over 1MB, requiring the separate Git Blobs API, which this feature does not call in V1), or when decoding otherwise fails.

**Rationale**: These are exactly the cases GitHub's Contents API itself cannot serve inline content for; treating them uniformly as "not fully evaluated" (rather than trying to special-case binary detection by extension, which is unreliable) matches FR-009's actual requirement — the reason doesn't need to be more specific than "content wasn't retrievable," since the remediation (the file still counts toward size-based checks, per the parent User Story 2) is the same regardless of cause.

**Alternatives considered**: Calling the separate Git Blobs API for files over 1MB to fetch large file content anyway — rejected for V1 as unnecessary complexity; the spec's Assumptions already state size limits follow "the source provider's own published constraints rather than a new AgentGuard-specific limit," and secret-scanning/contract-diff rules are unlikely to need multi-megabyte file bodies in practice.
