# Feature Specification: GitHub PR Import for AgentGuard

**Feature Branch**: `003-github-pr-import`

**Created**: 2026-08-22

**Status**: Draft

**Input**: User description: "GitHub PR import for AgentGuard - formalize pulling a real PR's changed files from GitHub and feeding them into AgentGuard's risk analysis, replacing the current manual/ad hoc approach with a supported capability."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Analyze a Real GitHub PR by URL (Priority: P1)

A developer pastes the URL of an existing pull request from a public GitHub repository into AgentGuard and receives the same risk analysis (score, classification, checks, findings, recommendation) they would get from manually submitted change data — without assembling that data by hand.

**Why this priority**: This closes the exact gap that makes AgentGuard impractical for real day-to-day use today — a developer cannot currently analyze a real PR without manually copying file contents into a request. This is the feature that turns AgentGuard from a demo into a usable tool.

**Independent Test**: Can be fully tested by providing the URL of a real, small public GitHub PR and verifying the returned analysis reflects that PR's actual changed files, matching what manual submission of the same file data would produce.

**Acceptance Scenarios**:

1. **Given** a valid URL to an existing pull request on a public GitHub repository, **When** a developer submits it for analysis, **Then** AgentGuard fetches that PR's changed files and returns a complete risk analysis result equivalent to submitting the same file data manually.
2. **Given** a pull request that has already been merged or closed, **When** a developer submits its URL, **Then** AgentGuard still returns a complete risk analysis using the file states from that PR.
3. **Given** the same PR URL submitted twice with no change to the PR in between, **When** analysis is run both times, **Then** the resulting score, classification, and recommendation are identical both times.

---

### User Story 2 - Understand What Couldn't Be Analyzed (Priority: P2)

A developer reviewing results for a real PR can tell, for any file AgentGuard could not fully evaluate (e.g., a binary asset, or a file too large to retrieve), that it was skipped and why — rather than being left to assume the analysis was complete when it wasn't.

**Why this priority**: Without this, a developer could trust a "SAFE TO REVIEW" result that silently missed a risky change in a file the system simply couldn't read. Making gaps visible preserves the trustworthiness that is the whole point of AgentGuard's score.

**Independent Test**: Can be fully tested by submitting a real PR that includes at least one binary or oversized file, and verifying the analysis result indicates that file was not fully evaluated, while still returning results for the rest of the PR.

**Acceptance Scenarios**:

1. **Given** a PR that includes a binary file (e.g., an image), **When** the analysis completes, **Then** the result indicates that file's content could not be evaluated, and the file's size still contributes to size-based checks.
2. **Given** a PR where every changed file can be fully retrieved, **When** the analysis completes, **Then** no such notice appears.

---

### User Story 3 - Get a Clear Error for an Invalid or Inaccessible PR, With a Path to Recover (Priority: P3)

A developer who submits a malformed PR reference receives a clear, specific error explaining the problem. A developer who submits a syntactically valid PR reference that cannot be retrieved without credentials — which looks identical, at first, to a PR that genuinely does not exist — is offered the chance to supply a credential and retry, rather than being told a definitive but potentially wrong story about why it failed.

**Why this priority**: A confusing or silent failure here is worse than no feature at all, since it could be mistaken for "this PR has no risk" rather than "this PR could not be checked." This is a safety rail on top of the core capability, not the core value itself. Note that GitHub's API intentionally returns the same "not found" response for a PR that doesn't exist and for a private PR the caller can't yet access — so this story treats that outcome as recoverable rather than promising an immediate, always-accurate distinction the source provider itself doesn't give up front.

**Independent Test**: Can be fully tested by submitting an invalid PR URL (expect an invalid-reference error with no retry offered), a URL for a PR that does not exist (expect a not-found/no-access outcome, retry with any credential still fails), and a URL for a real private repository (expect the same not-found/no-access outcome, but retrying with a credential that has access succeeds and returns a full analysis).

**Acceptance Scenarios**:

1. **Given** a URL that is not a validly formatted GitHub pull request URL, **When** a developer submits it, **Then** AgentGuard returns an error identifying the URL as invalid, without returning any risk score, and does not offer a credential retry (no credential could fix a malformed URL).
2. **Given** a syntactically valid URL that AgentGuard cannot retrieve without a credential, and no credential was supplied, **When** a developer submits it, **Then** AgentGuard returns a not-found-or-no-access outcome and offers the developer a way to supply a credential and retry the same PR reference.
3. **Given** a not-found-or-no-access outcome from Scenario 2, **When** the developer retries with a credential that has access to that repository, **Then** AgentGuard completes and returns a full risk analysis, confirming the original outcome was a private-access case rather than a nonexistent PR.
4. **Given** a not-found-or-no-access outcome from Scenario 2, **When** the developer retries with a credential that still lacks access (or no PR exists there at all), **Then** AgentGuard returns the same not-found-or-no-access outcome rather than a misleading different error.

---

### Edge Cases

- What happens when the source hosting provider is rate-limiting requests? System returns a clear, specific error indicating the request could not complete due to rate limiting, distinct from a "not found" or "invalid" error, rather than a partial or incorrect analysis.
- What happens when a valid-looking PR reference cannot be retrieved and no credential was supplied? System treats this as a recoverable not-found-or-no-access outcome and offers a credential retry, since the source provider does not distinguish "doesn't exist" from "exists but requires access" for an unauthenticated request.
- What happens when a retry credential itself lacks access, or the PR truly does not exist? System returns the same not-found-or-no-access outcome again rather than asserting a specific cause it cannot actually confirm.
- What happens when a changed file was renamed with no content changes? The file is evaluated under its new path using its actual before/after content, consistent with how a normal content change is evaluated.
- What happens when a PR has an extremely large number of changed files? Analysis still completes and returns a single deterministic result; files that cannot practically be retrieved are reported as not fully evaluated rather than causing the whole request to fail, consistent with how large manually-submitted PRs are already handled.
- What happens when no credential is supplied for the source hosting provider? Public repositories can still be analyzed, subject to that provider's unauthenticated rate limits; the caller is informed if a request fails specifically due to those limits.
- What happens to a credential the caller does supply? It is used only for the duration of the single request to retrieve PR data and is never stored or logged.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept a reference to a single pull request on a supported source hosting provider (a PR URL, or an equivalent owner/repository/PR-number identifier) as input.
- **FR-002**: System MUST retrieve, for the referenced PR, its title, repository name, PR number, and the full set of changed files (path, change type, and before/after content) needed to run AgentGuard's existing risk analysis rules.
- **FR-003**: System MUST map the source provider's file change status (added, modified, removed, renamed) to AgentGuard's existing change-type classification without altering the meaning of any existing risk rule.
- **FR-004**: System MUST run the retrieved change data through AgentGuard's existing risk analysis and return the same result shape (score, classification, recommendation, checks, findings) as manually submitted change data.
- **FR-005**: System MUST support analyzing PRs from public repositories without requiring the caller to supply a credential.
- **FR-006**: System MUST accept an optional caller-supplied credential to analyze PRs from repositories the caller has access to, or to raise the source provider's request rate limit, and MUST use that credential only for the duration of the single request.
- **FR-007**: System MUST NOT persist, log, or otherwise retain any credential supplied for this purpose beyond the single request in which it is used.
- **FR-008**: System MUST NOT persist or retain fetched file content beyond the single request's analysis, consistent with AgentGuard's existing no-persistence behavior.
- **FR-009**: When a file's content cannot be retrieved (e.g., binary content, or content exceeding a practical size limit), system MUST continue analysis for the remaining PR data and MUST indicate in the result which file(s) were not fully evaluated and why.
- **FR-010**: System MUST return a clear, specific error — distinct from a risk analysis result — when the PR reference is malformed, when the source provider is rate-limiting the request, or (per FR-010a) when the PR cannot be retrieved for reasons that may include it not existing or requiring access.
- **FR-010a**: When a syntactically valid PR reference cannot be retrieved and no credential was supplied, system MUST report a not-found-or-no-access outcome (without asserting which cause it is, since the source provider does not distinguish them for unauthenticated requests) and MUST allow the caller to retry the same PR reference with a credential.
- **FR-011**: System MUST produce identical analysis results for repeated requests against the same, unchanged PR, consistent with AgentGuard's existing determinism guarantee.

### Key Entities

- **PR Reference**: The input identifying which pull request to import — a URL or owner/repository/PR-number triple identifying one PR on one source hosting provider.
- **Retrieved Change File**: One file as fetched from the source provider for the referenced PR — its path, change type, before/after content (when retrievable), and an indicator of whether its content was fully retrieved.
- **Import Error**: A distinct outcome from a risk analysis result, describing why a PR reference could not be turned into an analysis — invalid reference, not-found-or-no-access (retryable with a credential), or rate-limited.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can get a complete risk analysis for a real, existing public GitHub pull request by providing only its URL, with no manual data entry required.
- **SC-002**: For a PR of typical size (under 50 changed files, matching AgentGuard's existing "typical size" definition), a complete analysis is returned in under 15 seconds.
- **SC-003**: 100% of PRs containing at least one file that cannot be fully retrieved still produce a complete analysis of the rest of the PR, with those files clearly indicated rather than silently omitted.
- **SC-004**: 100% of invalid, not-found-or-inaccessible, or rate-limited PR references produce a specific, actionable outcome rather than an analysis result or an unhandled failure; every not-found-or-no-access outcome offers a credential retry, and a retry with a credential that has real access succeeds 100% of the time.
- **SC-005**: Re-analyzing the same unchanged PR produces identical results 100% of the time.

## Assumptions

- GitHub is the only source hosting provider supported in this version; other providers (GitLab, Bitbucket, etc.) are out of scope.
- This feature extends AgentGuard's existing risk analysis capability with a new way to supply input; it does not change any of the five existing risk rules, the scoring model, or the classification/recommendation mapping defined for AgentGuard V1.
- No new data persistence is introduced; each import-and-analyze request remains a single synchronous operation, consistent with AgentGuard's existing "no database" constraint.
- No new user authentication is introduced for using this feature itself; an optional source-provider credential is a per-request pass-through, not an AgentGuard user account.
- A "practical size limit" for individual file content retrieval follows the source provider's own published constraints rather than a new AgentGuard-specific limit.
- Very large PRs (e.g., hundreds of changed files) are handled on a best-effort basis, consistent with how AgentGuard already treats large manually-submitted PRs; there is no hard cap on PR size for this feature to reject outright.
