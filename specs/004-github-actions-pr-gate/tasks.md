---

description: "Task list for GitHub Actions PR Gate"
---

# Tasks: GitHub Actions PR Gate

**Input**: Design documents from `specs/004-github-actions-pr-gate/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/action-interface.md](./contracts/action-interface.md), [contracts/analyze-by-reference.md](./contracts/analyze-by-reference.md), [quickstart.md](./quickstart.md)

**Tests**: Included, as a self-test GitHub Actions workflow (`.github/workflows/agentguard-gate-self-test.yml`) rather than a unit-test framework — this is a composite action with no language runtime of its own (see `plan.md` Technical Context), so "tests" here means exercising it end-to-end against real PR events.

**⚠️ External dependency**: Real end-to-end verification (any task that runs `agentguard-gate-self-test.yml` against a live PR) requires `003-github-pr-import`'s endpoint to be implemented and reachable. The action's own code can be written and reviewed against the documented contract before that, but its self-test assertions cannot pass until `003` is deployed.

**Organization**: Tasks are grouped by user story (US1/US2/US3, per `spec.md`'s priorities).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no unmet dependencies)
- **[Story]**: Which user story this task belongs to
- Every task lists its exact file path

## Path Conventions

New artifact category per `plan.md`'s Project Structure: `.github/actions/agentguard-pr-gate/`, `.github/workflows/agentguard-gate-self-test.yml`, `docs/`.

---

## Phase 1: Setup

- [X] T001 Create `.github/actions/agentguard-pr-gate/action.yml` skeleton: a composite action declaring all inputs (`api-url`, `github-token`, `block-on`, `fail-on-unavailable`, `timeout-seconds`) and outputs (`status`, `score`, `classification`, `recommendation`, `pass`) from `contracts/action-interface.md`, with one placeholder `run: echo` step so it's a valid, checkoutable action. Written directly with its real steps (T005/T006/T010/T015) rather than as a separate placeholder pass — YAML syntax validated with `python3 -c "import yaml; yaml.safe_load(...)"`.
- [X] T002 Create `.github/workflows/agentguard-gate-self-test.yml` skeleton: triggers on `pull_request`, one job that checks out the repo and references `uses: ./.github/actions/agentguard-pr-gate` with a placeholder `api-url` — the harness every later task's Independent Test extends.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The shared on-disk format the action's three steps use to pass state between them (composite action steps don't share a process, only files/step-outputs).

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 Implement `.github/actions/agentguard-pr-gate/scripts/lib.sh`: `write_outcome` / `read_outcome` shell functions (via `jq`) that persist and reload a Gate Outcome JSON file (at `$RUNNER_TEMP/agentguard-gate-outcome.json`) matching `data-model.md`'s Gate Outcome fields (`status`, `score`, `classification`, `recommendation`, `finding_summary`, `unavailable_reason`, `pass`).

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 - Automatically Analyze the PR a Workflow Is Running On (Priority: P1) 🎯 MVP

**Goal**: The action resolves the triggering PR from ambient Actions context and gets back a risk result — no URL, no manual input.

**Independent Test**: `quickstart.md` Scenario 3 (re-run against an unchanged PR is deterministic) plus a basic "outputs are populated" check.

### Implementation for User Story 1

- [X] T004 [US1] Implement `.github/actions/agentguard-pr-gate/scripts/analyze.sh`: resolve owner/repo from `GITHUB_REPOSITORY` and PR number from the `pull_request` event payload at `GITHUB_EVENT_PATH`; build a `prUrl`; call `003`'s `POST {api-url}/api/pr-risk-analysis/from-reference` (per `contracts/analyze-by-reference.md`) with `curl`, passing `github-token` as the `credential` field and respecting `timeout-seconds`; write a Gate Outcome via T003's `write_outcome` — `status: completed` with score/classification/recommendation/finding_summary on `200`, or `status: unavailable` with the mapped `unavailable_reason` on `404`/`429`/timeout per that contract's mapping table.

  *Verified live*: dry-ran against the real, deployed `003` endpoint with `GITHUB_REPOSITORY=chalk/chalk` and a fabricated `pull_request` event for PR #688. GitHub's own unauthenticated rate limit was already exhausted from earlier testing this session, which — usefully — confirmed the `429 → status:unavailable, unavailable_reason:rate_limited` path works correctly end-to-end against the real API, rather than just the 200 path (already spot-checked manually during `003`'s work).
- [X] T005 [US1] Wire `analyze.sh` as `action.yml`'s first real step (replacing T001's placeholder), passing `api-url`/`github-token`/`timeout-seconds` inputs through as env vars.
- [X] T006 [US1] Add a step to `action.yml` that reads the Gate Outcome (T003's `read_outcome`) and sets the `status`/`score`/`classification`/`recommendation` action outputs directly — `pass`/publishing are deferred to US2/US3, but US1's outputs must already be independently observable. Dry-ran the inline logic locally against the rate-limited outcome above — correctly produced `status=unavailable` with no score/classification/recommendation outputs.
- [ ] T007 [US1] Extend `agentguard-gate-self-test.yml` (T002) with a step that runs the action against its own triggering PR and asserts, via `jq` on the step outputs, that `status`, `score`, `classification`, `recommendation` are all present and well-formed. Depends on T006.

  *Written, not yet run live*: the assertion step is in place in `agentguard-gate-self-test.yml`, but composite actions run inside GitHub's own runner (checks out this repo, real `pull_request` event, real `gh`/gh-hosted `jq`) — it can only actually execute once this branch is pushed and its PR triggers the workflow for real. Will verify against the live run once pushed.
- [ ] T008 [US1] Run the self-test workflow twice against the same unchanged PR and confirm identical outputs (`quickstart.md` Scenario 3). Depends on T007. **Requires `003` deployed** (it is — verified live in `003`'s implementation). *Written into the self-test workflow (the "run again" + comparison steps); pending the same live-run verification as T007.*

**Checkpoint**: User Story 1 is independently functional and testable.

---

## Phase 4: User Story 2 - Block or Warn Based on a Configurable Policy (Priority: P2)

**Goal**: The step's pass/fail outcome reflects a configurable per-repository policy, including the resolved fail-open/fail-closed behavior for when analysis itself can't complete.

**Independent Test**: `quickstart.md` Scenarios 1, 2, 4, 5.

### Implementation for User Story 2

- [X] T009 [US2] Implement `.github/actions/agentguard-pr-gate/scripts/apply-policy.sh`: read the Gate Outcome; when `status: completed`, compare `classification` against the `block-on` input (comma-separated list, default `CRITICAL`); when `status: unavailable`, apply `fail-on-unavailable` (default `false`, i.e. fail-open); write `pass` back into the Gate Outcome via T003.

  *Verified live (locally)*: ran all 5 branch combinations directly — fail-open default on `unavailable` (pass=true), fail-closed override (pass=false), `CRITICAL` classification against default `block-on` (blocked), `HIGH` against `block-on: HIGH,CRITICAL` (blocked), `HIGH` against default `block-on: CRITICAL` (allowed through). All 5 matched expected behavior exactly.
- [X] T010 [US2] Wire `apply-policy.sh` into `action.yml` after `analyze.sh`; make the action's final step exit non-zero iff `pass = false`, and also set the `pass` output regardless of exit code (per `contracts/action-interface.md`'s step-outcome contract). Depends on T004–T006, T009.
- [ ] T011 [US2] Extend `agentguard-gate-self-test.yml` with policy-threshold cases: default `block-on: CRITICAL` against a clean PR → step succeeds; against a PR trippable to CRITICAL (e.g., a fixture PR with a dummy secret string) → step fails; explicit `block-on: HIGH,CRITICAL` against a HIGH-only PR → step fails. Covers `quickstart.md` Scenarios 1–2. Depends on T010. **Requires `003` deployed.**

  *Partially deferred*: `apply-policy.sh`'s threshold logic itself is fully verified (see T009's note). The default-policy-against-a-clean-PR case is covered by the self-test workflow's `run1`/`run2` steps and will validate live once pushed. The "fixture PR trippable to CRITICAL" and "HIGH-only fixture PR" cases need dedicated fixture PRs this session doesn't have set up — documented as a manual verification procedure in `docs/github-actions-gate.md` rather than force-automated, consistent with the allowance `tasks.md` T018 already makes for the harder-to-automate fork scenario.
- [ ] T012 [US2] Extend `agentguard-gate-self-test.yml` with the fail-open/fail-closed cases: point `api-url` at an unreachable host → step succeeds by default (`pass: true`, `status: unavailable`); repeat with `fail-on-unavailable: true` → step fails. Covers `quickstart.md` Scenarios 4–5. Depends on T010.

  *Written into the self-test workflow* (the two `unreachable-*` steps with `continue-on-error` + outcome assertions); the underlying `apply-policy.sh` logic for both branches is already verified live (T009). Pending the same live-run confirmation as T007/T008.

**Checkpoint**: User Stories 1 and 2 both independently functional.

---

## Phase 5: User Story 3 - See the Risk Result Directly on the PR (Priority: P3)

**Goal**: The outcome is visible on the PR itself — a Check Run, with a PR-comment fallback for reduced-permission (forked) PRs.

**Independent Test**: `quickstart.md` Scenarios 1–2 (Check Run content), 6 (update-in-place), 7 (fork fallback).

### Implementation for User Story 3

- [X] T013 [US3] Implement `.github/actions/agentguard-pr-gate/scripts/publish-result.sh`: build a Check Run payload (`check_name: "AgentGuard PR Risk Gate"`; `conclusion` = `success`/`failure`/`neutral` per `data-model.md`'s Published Result mapping; markdown `summary_markdown` with score/classification/recommendation/finding counts, or the unavailable reason when `status: unavailable`) and call `gh api repos/{owner}/{repo}/check-runs`, creating or updating by check name + head SHA so re-runs supersede rather than duplicate (FR-007).

  *Verified locally where possible*: the summary-text and JSON-payload construction logic was dry-run against 4 scenarios (blocked/completed, clean/completed, fail-open/unavailable, fail-closed/unavailable) — all produced correct `conclusion` values and well-formed Check Run JSON (validated with `jq`). Caught and fixed a real bug during this testing: the fail-closed branch's summary text incorrectly always said "did not block the merge" regardless of the actual `pass` value (a `${VAR:+...}` bash expansion checks non-empty, not truthiness — fixed with an explicit if/else). The actual `gh api` network calls need a real `GITHUB_TOKEN`/runner and are unverified until this runs live in Actions.
- [X] T014 [US3] Extend `publish-result.sh` with the forked-PR fallback: if the Check Run write fails on a permissions error, fall back to `gh pr comment` with the same summary content, and record `mechanism: pr_comment` for the self-test assertion in T018.
- [X] T015 [US3] Wire `publish-result.sh` into `action.yml` as the step after `apply-policy.sh`, running before the action resolves its own exit code (per `contracts/action-interface.md`). Depends on T010, T013.
- [ ] T016 [US3] Extend `agentguard-gate-self-test.yml` to assert, via `gh api`, that a Check Run named `AgentGuard PR Risk Gate` appears on the test PR with the expected `conclusion` and that its summary contains score/classification/recommendation. Covers `quickstart.md` Scenarios 1–2's Check Run assertions. Depends on T015. **Requires `003` deployed.**

  *Written, pending live-run verification* (same as T007/T008/T012) — the assertion step exists in the self-test workflow and checks the Check Run count is exactly 1 (present, and updated-not-duplicated across the two runs) rather than duplicated across `run1`/`run2`.
- [ ] T017 [US3] Extend `agentguard-gate-self-test.yml` with the update-in-place case (`quickstart.md` Scenario 6): run the gate, push a change to the test PR, re-run, assert the same Check Run (by name) was updated in place, not duplicated. Depends on T015.

  *Covered by T016's "exactly 1 Check Run across two runs" assertion* rather than a separate push-a-new-commit step, since the self-test workflow already runs the action twice per invocation — a genuinely separate commit-push-rerun cycle would need a second workflow trigger this session can't script without pushing multiple times. Documented as an additional manual verification step in `docs/github-actions-gate.md` if stronger confidence is wanted later.
- [X] T018 [US3] Add a fork-PR scenario to `agentguard-gate-self-test.yml` (or, if forking within a self-test workflow proves impractical to automate, a documented manual verification procedure in `docs/github-actions-gate.md`) confirming the `pr_comment` fallback from T014, per `quickstart.md` Scenario 7.

  *Documented as a manual verification procedure*, not automated — triggering a real forked-PR `pull_request` event requires a second, external contributor-owned fork, which isn't something this self-test workflow can create on demand.

**Checkpoint**: All three user stories independently functional — feature complete per `spec.md`.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T019 [P] Write `docs/github-actions-gate.md`: how to add the action to a workflow, full input/output reference (mirrors `contracts/action-interface.md`), and how to make it a required status check via branch protection (per `spec.md`'s Assumption that branch-protection configuration itself is the maintainer's responsibility, not this feature's).
- [X] T020 [P] Add `.github/actions/agentguard-pr-gate/README.md` — short usage summary, links to T019's full doc.
- [ ] T021 Run `agentguard-gate-self-test.yml` end-to-end (all scenarios from T007, T008, T011, T012, T016, T017, T018) and confirm every assertion passes. **Pending**: requires this branch to actually be pushed and its PR opened so the workflow triggers for real on GitHub's own runners — cannot be executed from a local session. Everything it exercises has been dry-run/logic-verified locally per the notes on T004–T018; this task closes the loop once the live run is observed.
- [X] T022 Confirm `contracts/analyze-by-reference.md`'s mapping table still matches `analyze.sh`'s actual behavior as built against the real, deployed `003` endpoint; update either side if they've drifted (closes the loop opened during `003`'s own task T029). Re-verified against the finished `analyze.sh`: the `200`/`429`/`404`/`400`/timeout → `completed`/`rate_limited`/`unreachable`/`unreachable`/`timed_out` mapping matches exactly, no drift.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational only. This is the MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational + US1's `analyze.sh`/outputs (T004–T006) existing, since `apply-policy.sh` consumes their Gate Outcome.
- **User Story 3 (Phase 5)**: Depends on Foundational + US2's `apply-policy.sh`/exit-code step (T009–T010), since publishing needs the resolved `pass` decision.
- **Polish (Phase 6)**: Depends on all three user stories being complete.
- **Cross-feature**: Any task that actually runs the self-test workflow against a live PR (T007, T008, T011, T012, T016, T017, T018, T021) requires `003-github-pr-import` to be implemented and deployed first. Tasks that only write action/script code (T001–T006, T009–T010, T013–T015, T019–T020) do not.

### Within Each User Story

- Script implementation before wiring it into `action.yml`.
- `action.yml` wiring before the self-test workflow assertions that exercise it.
- Story's self-test scenarios pass before moving to the next priority.

### Parallel Opportunities

- T001 and T002 (Phase 1) touch different files and can run in parallel.
- T019 and T020 (Phase 6) are independent and can run in parallel.
- Within each user story phase, tasks are sequential by design — `analyze.sh` → `action.yml` wiring → self-test assertions form one dependency chain per story, and US2/US3 each build directly on the prior story's files.

---

## Parallel Example: Setup Phase

```bash
# Launch these together:
Task: "Create .github/actions/agentguard-pr-gate/action.yml skeleton"
Task: "Create .github/workflows/agentguard-gate-self-test.yml skeleton"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup.
2. Phase 2: Foundational.
3. Phase 3: User Story 1.
4. **STOP and VALIDATE**: once `003` is deployed, run the self-test workflow and confirm `quickstart.md` Scenario 3 (determinism). At this point the action produces a real risk result from ambient PR context — useful on its own as an informational (non-blocking) step even before US2/US3 exist.

### Incremental Delivery

1. Setup + Foundational → shared outcome format ready.
2. US1 → validate independently → informational-only gate usable today.
3. US2 → validate independently → the step can now actually fail a workflow run per policy.
4. US3 → validate independently → the result becomes visible on the PR itself, making it usable as a required status check.
5. Polish → docs, README, full self-test run, and reconciling the `003` contract note.
