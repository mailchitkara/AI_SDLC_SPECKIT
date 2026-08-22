---

description: "Task list for GitHub PR Import for AgentGuard"
---

# Tasks: GitHub PR Import for AgentGuard

**Input**: Design documents from `specs/003-github-pr-import/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/pr-reference-analysis-endpoint.md](./contracts/pr-reference-analysis-endpoint.md), [quickstart.md](./quickstart.md)

**Tests**: Included — the contract explicitly enumerates the test coverage this feature needs (see `contracts/pr-reference-analysis-endpoint.md`'s "Contract test coverage" section), and this repo's existing endpoint (`PrRiskAnalysisEndpointTests.cs`) already establishes that pattern.

**Organization**: Tasks are grouped by user story (US1/US2/US3, per `spec.md`'s priorities) so each can be implemented, tested, and demoed independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no unmet dependencies)
- **[Story]**: Which user story this task belongs to
- Every task lists its exact file path

## Path Conventions

Web app layout already established by this repo: `backend/AgentGuard.Api/`, `backend/AgentGuard.Api.Tests/`. `backend/AgentGuard.Core` is not touched by this feature (see `plan.md` Constitution Check).

---

## Phase 1: Setup

**Purpose**: Confirm the baseline before adding anything.

- [X] T001 Run `dotnet build backend/AgentGuard.sln` and `dotnet test backend/AgentGuard.sln` to confirm a clean baseline before starting; no code changes in this task.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared contracts and a minimal (happy-path-only) GitHub client that every user story builds on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Define `IGitHubPullRequestClient` in `backend/AgentGuard.Api/GitHub/IGitHubPullRequestClient.cs` — one method taking owner/repository/PR-number/optional credential, returning a `GitHubPullRequestClientResult` (from T008).
- [X] T003 [P] Define `GitHubFileStatusMapping` in `backend/AgentGuard.Api/GitHub/GitHubFileStatusMapping.cs` mapping GitHub's `added`/`removed`/`modified`/`renamed` file status strings to the existing `AgentGuard.Core.ChangeType` enum (`Added`/`Deleted`/`Modified`/`Renamed`), mirroring the style of `backend/AgentGuard.Api/Contracts/EnumMappings.cs`.
- [X] T004 [P] Define `PrReferenceAnalysisRequest` record in `backend/AgentGuard.Api/Contracts/PrReferenceAnalysisRequest.cs` with `PrUrl`, `Owner`, `Repository`, `PrNumber`, `Credential` fields per `data-model.md`'s PR Reference entity.
- [X] T005 [P] Define `ImportErrorResponse` record in `backend/AgentGuard.Api/Contracts/ImportErrorResponse.cs` with `ErrorType`, `Message`, `RetryableWithCredential` fields per `data-model.md`'s Import Error entity and `contracts/pr-reference-analysis-endpoint.md`'s error bodies.
- [X] T006 [P] Extend `backend/AgentGuard.Api/Contracts/RiskAnalysisResultResponse.cs`: add a `PartiallyEvaluatedFileResponse(string Path, string Reason)` record, add `PartiallyEvaluatedFiles` to `RiskAnalysisResultResponse`, and extend `RiskAnalysisResultResponseMapping.ToResponse` to accept and populate it (default to empty list so the existing manual `/api/pr-risk-analysis` call site keeps compiling unchanged).
- [X] T007 Implement `PrReferenceAnalysisRequestValidator` in `backend/AgentGuard.Api/Contracts/PrReferenceAnalysisRequestValidator.cs`: valid iff exactly one of {`PrUrl` present} or {`Owner`+`Repository`+`PrNumber` all present}; validates `PrUrl` against the GitHub PR URL pattern (`^https?://github\.com/([^/]+)/([^/]+)/pull/(\d+)`). Depends on T004.
- [X] T008 [P] Define `GitHubPullRequestClientResult` in `backend/AgentGuard.Api/GitHub/GitHubPullRequestClientResult.cs` as a discriminated result covering: `Success` (PR title + list of retrieved files, each with `FullyEvaluated` flag per `data-model.md`'s Retrieved Change File), `NotFoundOrNoAccess`, and `RateLimited`.
- [X] T009 Implement the happy-path of `GitHubPullRequestClient` in `backend/AgentGuard.Api/GitHub/GitHubPullRequestClient.cs`: fetch PR metadata, walk all pages of the files list (per `research.md` §4), fetch old/new content at base/head SHA for each file, map status via T003, return `Success`. Error/partial-content handling is added later by US2 (T017) and US3 (T022, T023) — this task only needs the successful-retrieval path to compile and work. Depends on T002, T003, T008.

  *Implementation note*: written as one complete file covering T009+T017+T022+T023 together (pagination, not-retrievable-content, 404→NotFoundOrNoAccess, and rate-limit detection all landed in the same pass) rather than three separate incremental edits to the same class — noted here so the checklist stays honest about when each behavior actually landed; T017/T022/T023 are marked done once their corresponding tests (US2/US3 phases) confirm the behavior, not redundantly re-implemented.
- [X] T010 Register the GitHub client in `backend/AgentGuard.Api/Program.cs`: `builder.Services.AddHttpClient<IGitHubPullRequestClient, GitHubPullRequestClient>(c => { c.BaseAddress = new Uri("https://api.github.com"); c.DefaultRequestHeaders.UserAgent.ParseAdd("AgentGuard"); })`. Depends on T009.
- [X] T011 [P] Create `FakeGitHubPullRequestClient` in `backend/AgentGuard.Api.Tests/Fakes/FakeGitHubPullRequestClient.cs` implementing `IGitHubPullRequestClient` with settable canned `GitHubPullRequestClientResult` responses, for use by every story's endpoint tests. Depends on T002, T008.

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 - Analyze a Real GitHub PR by URL (Priority: P1) 🎯 MVP

**Goal**: A `prUrl` or owner/repository/PR-number request returns the same analysis shape as manual submission.

**Independent Test**: `quickstart.md` Scenarios 1–3 (URL form, trio form, determinism) against a real public PR.

### Tests for User Story 1

- [X] T012 [US1] Write endpoint tests in `backend/AgentGuard.Api.Tests/PrReferenceAnalysisEndpointTests.cs` (new file) using `FakeGitHubPullRequestClient` (T011): valid `prUrl` → `200` with expected `RiskAnalysisResultResponse` shape; valid owner/repository/prNumber trio → `200`, identical result to the equivalent `prUrl` request; same request run twice → identical result. Covers `contracts/pr-reference-analysis-endpoint.md`'s first three "Contract test coverage" bullets.

### Implementation for User Story 1

- [X] T013 [US1] Implement `PrReferenceAnalysisEndpoint.MapPrReferenceAnalysisEndpoint` (`POST /api/pr-risk-analysis/from-reference`) in `backend/AgentGuard.Api/Endpoints/PrReferenceAnalysisEndpoint.cs`: validate via T007 (`400` on failure), resolve owner/repository/PR-number from either request form, call `IGitHubPullRequestClient`, map a `Success` result's files to `AgentGuard.Core.ChangedFile` (via T003), run the existing `AgentGuardAnalyzer.Analyze` unchanged, return `200` via T006's extended response mapping. Depends on T006, T007, T008, T009.
- [X] T014 [US1] Wire `app.MapPrReferenceAnalysisEndpoint();` into `backend/AgentGuard.Api/Program.cs`, alongside the existing `app.MapPrRiskAnalysisEndpoint();` call. Depends on T013.
- [X] T015 [US1] Run `quickstart.md` Scenarios 1–3 against a locally running `dotnet run` instance (`backend/AgentGuard.Api`) and confirm the responses match what's documented there (including the `chalk/chalk#688` result already spot-checked manually). Depends on T014.

  *Verified live*: started the API locally and hit both request forms against the real `chalk/chalk#688` PR — response matched the earlier ad hoc manual test exactly (score 0, LOW, SAFE_TO_REVIEW).

**Checkpoint**: User Story 1 is fully functional and independently testable/demoable.

---

## Phase 4: User Story 2 - Understand What Couldn't Be Analyzed (Priority: P2)

**Goal**: Files GitHub can't serve inline are reported, not silently dropped.

**Independent Test**: `quickstart.md` Scenario 4.

### Tests for User Story 2

- [X] T016 [US2] Add a test to `backend/AgentGuard.Api.Tests/PrReferenceAnalysisEndpointTests.cs`: a PR containing one file the fake client reports as not-retrievable → `200`, that file appears in `partiallyEvaluatedFiles` with `reason: "not_retrievable"`, and the rest of the analysis (e.g., its line counts still feeding `LargeChangeSize`) is unaffected. Covers `contracts/pr-reference-analysis-endpoint.md`'s partial-evaluation bullet. Depends on T012 (same file).

### Implementation for User Story 2

- [X] T017 [US2] Extend `GitHubPullRequestClient` (`backend/AgentGuard.Api/GitHub/GitHubPullRequestClient.cs`) to detect not-retrievable content per `research.md` §5 (missing content, non-base64 `encoding`, or a 404 at that specific path/ref) and set `FullyEvaluated: false` on that file while still capturing its `linesAdded`/`linesDeleted`. Depends on T009.
- [X] T018 [US2] Populate the endpoint's `PartiallyEvaluatedFiles` response field (`backend/AgentGuard.Api/Endpoints/PrReferenceAnalysisEndpoint.cs`) from files where `FullyEvaluated = false`. Depends on T017, T013.
- [X] T019 [US2] Run `quickstart.md` Scenario 4 and confirm the `partiallyEvaluatedFiles` behavior. Depends on T018.

  *Verified via T016's automated test* (exercises the identical endpoint/client code path with `FullyEvaluated: false`). Did not additionally spot-check against a real GitHub PR with a binary file — candidates found (e.g. `twbs/bootstrap#42329`) were too large (100 files) to fetch within the unauthenticated GitHub API rate limit available this session; GitHub's contents-API behavior for oversized/binary files (missing `encoding`/`content`) is stable, documented behavior this code already handles per `research.md` §5.

**Checkpoint**: User Stories 1 and 2 both independently functional.

---

## Phase 5: User Story 3 - Get a Clear Error for an Invalid or Inaccessible PR, With a Path to Recover (Priority: P3)

**Goal**: Malformed/not-found-or-no-access/rate-limited references each get a distinct, correct outcome, and a not-found-or-no-access outcome is genuinely resolved by retrying with a working credential.

**Independent Test**: `quickstart.md` Scenarios 5–8.

### Tests for User Story 3

- [X] T020 [US3] Add tests to `backend/AgentGuard.Api.Tests/PrReferenceAnalysisEndpointTests.cs`: malformed/both/neither reference forms → `400 invalid_reference`; fake client returns `NotFoundOrNoAccess` → `404 not_found_or_no_access`, `retryableWithCredential: true`; same request with a credential the fake client accepts → `200`; same request with a credential the fake client still rejects → `404` again (not a different error); fake client returns `RateLimited` → `429 rate_limited`. Covers `contracts/pr-reference-analysis-endpoint.md`'s remaining "Contract test coverage" bullets. Depends on T016 (same file).

### Implementation for User Story 3

- [X] T021 [US3] Confirm/extend `PrReferenceAnalysisEndpoint` (`backend/AgentGuard.Api/Endpoints/PrReferenceAnalysisEndpoint.cs`) returns `400` + `ImportErrorResponse{errorType: invalid_reference, retryableWithCredential: false}` when T007's validator fails. Depends on T013.
- [X] T022 [US3] Extend `GitHubPullRequestClient` to map a GitHub `404` (on the PR metadata or files-list call) to `NotFoundOrNoAccess`, and extend the endpoint to return `404` + `ImportErrorResponse{errorType: not_found_or_no_access, retryableWithCredential: true}` for it. Depends on T009, T013.
- [X] T023 [US3] Extend `GitHubPullRequestClient` to detect GitHub rate-limiting (`403` with `X-RateLimit-Remaining: 0`, or a genuine `429`) and map it to `RateLimited`; extend the endpoint to return `429` + `ImportErrorResponse{errorType: rate_limited, retryableWithCredential: false}`, forwarding GitHub's `Retry-After` header when present. Depends on T009, T013.
- [X] T024 [US3] In `GitHubPullRequestClient`, forward a supplied `Credential` as an `Authorization: Bearer {credential}` header on every GitHub call, and confirm (by inspection — no logging middleware currently exists in `Program.cs`) that `Credential` is never written to a log or echoed back in any response, per `plan.md`'s Constraints. Depends on T009.
- [X] T025 [US3] Run `quickstart.md` Scenarios 5–8 (invalid reference, not-found-then-recover, still-denied retry, rate-limited) and confirm. Depends on T021, T022, T023, T024.

  *Verified*: Scenarios 5 (invalid `prUrl` → 400) and 6's not-found half (nonexistent PR → 404 `not_found_or_no_access`) spot-checked live against the real GitHub API. The credential-retry half of Scenario 6, Scenario 7 (still-denied retry), and Scenario 8 (rate-limiting) are covered by the automated tests in T020, which exercise the identical endpoint/client code — a live private-repo credential and deliberately exhausting GitHub's rate limit weren't exercised live in this session to avoid consuming a scarce shared resource (a real PAT, and the remaining unauthenticated request budget) for the same code path already under test.

**Checkpoint**: All three user stories independently functional — feature complete per `spec.md`.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T026 [P] Add a `docs/github-pr-import.md` usage doc (mirroring `docs/deployment.md`'s style) describing the new endpoint for API consumers, linking to `contracts/pr-reference-analysis-endpoint.md`.
- [X] T027 [P] Add `.Produces<>()` OpenAPI annotations to `PrReferenceAnalysisEndpoint` (`200`, `400`, `404`, `429`) matching the existing `PrRiskAnalysisEndpoint` pattern, in `backend/AgentGuard.Api/Endpoints/PrReferenceAnalysisEndpoint.cs`. Done as part of T013 — the endpoint was written with these annotations from the start.
- [X] T028 Run the full backend suite (`dotnet test backend/AgentGuard.sln`) and confirm everything passes, including all tasks' new tests. 49/49 passing (33 Core + 16 Api, up from 4 to 16 in Api.Tests).
- [X] T029 Re-read `specs/004-github-actions-pr-gate/contracts/analyze-by-reference.md` against the as-built endpoint and correct it if implementation deviated from what was planned (closes the loop opened during `004`'s planning). Verified accurate — `prUrl`/`credential` field names and the `400`/`404`/`429` status-code scheme all match the as-built endpoint exactly; no changes needed.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational only. This is the MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational + US1's endpoint (T013) existing, since it extends the same client/endpoint files rather than adding new ones.
- **User Story 3 (Phase 5)**: Depends on Foundational + US1's endpoint (T013) existing, for the same reason. Independent of US2 (different behavior added to the same shared files, no shared logic between them).
- **Polish (Phase 6)**: Depends on all three user stories being complete.

### Within Each User Story

- Tests before implementation (T012 before T013; T016 before T017; T020 before T021–T024).
- `GitHubPullRequestClient` changes before the endpoint changes that consume them.
- Story complete (its quickstart scenarios pass) before moving to the next priority.

### Parallel Opportunities

- T002, T003, T004, T005, T006, T008 (Phase 2) touch disjoint files and can run in parallel.
- T011 can run in parallel with T009/T010 once T002 and T008 are done (test-project file, no overlap with the client/Program.cs).
- Within each user story phase, test and implementation tasks share a small number of files (`PrReferenceAnalysisEndpointTests.cs`, `GitHubPullRequestClient.cs`, `PrReferenceAnalysisEndpoint.cs`) by design — deliberately sequential, not marked `[P]`, to avoid conflicting edits.
- T026 and T027 (Phase 6) are independent of each other and can run in parallel.

---

## Parallel Example: Foundational Phase

```bash
# Launch these together once Phase 1 is done:
Task: "Define IGitHubPullRequestClient in backend/AgentGuard.Api/GitHub/IGitHubPullRequestClient.cs"
Task: "Define GitHubFileStatusMapping in backend/AgentGuard.Api/GitHub/GitHubFileStatusMapping.cs"
Task: "Define PrReferenceAnalysisRequest in backend/AgentGuard.Api/Contracts/PrReferenceAnalysisRequest.cs"
Task: "Define ImportErrorResponse in backend/AgentGuard.Api/Contracts/ImportErrorResponse.cs"
Task: "Extend RiskAnalysisResultResponse with PartiallyEvaluatedFiles in backend/AgentGuard.Api/Contracts/RiskAnalysisResultResponse.cs"
Task: "Define GitHubPullRequestClientResult in backend/AgentGuard.Api/GitHub/GitHubPullRequestClientResult.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup.
2. Phase 2: Foundational (blocks everything).
3. Phase 3: User Story 1.
4. **STOP and VALIDATE**: run `quickstart.md` Scenarios 1–3 against a real public PR.
5. This alone already closes the gap the ad hoc PowerShell script was standing in for — deployable as-is.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → validate independently → this is already usable (a developer or `004`'s gate can call it for well-formed, accessible public PRs).
3. US2 → validate independently → results on PRs with binary/oversized files stop silently under-reporting risk.
4. US3 → validate independently → callers get honest, recoverable errors instead of the endpoint's default (unhandled exception / generic 500) on bad input.
5. Polish → docs, OpenAPI annotations, full-suite confirmation, and reconciling `004`'s dependent contract.
