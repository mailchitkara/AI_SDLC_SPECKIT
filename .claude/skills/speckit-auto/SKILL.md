---
name: "speckit-auto"
description: "Run the full spec-kit pipeline (specify -> plan -> tasks -> implement) from one feature description, then ship it via this repo's standard branch/commit/push/PR/merge flow -- one prompt instead of four."
argument-hint: "Describe the feature you want built end-to-end"
compatibility: "Requires spec-kit project structure with .specify/ directory (same as speckit-specify/plan/tasks/implement)"
metadata:
  author: "repo-custom"
user-invocable: true
disable-model-invocation: false
---

## User Input

```text
$ARGUMENTS
```

If empty, ask the user what feature to build before doing anything else -- do not guess a feature description.

## What this skill is

A thin orchestrator over the four existing spec-kit skills (`speckit-specify`, `speckit-plan`, `speckit-tasks`, `speckit-implement`), plus this repo's established shipping flow. It does not reimplement any of their logic -- it invokes each one in turn, in the same way a user would type each `/speckit-*` command manually, and carries the feature through to a merged PR.

Only exists because running the four commands separately, then shipping by hand, is the same sequence every time. If any of the four underlying skills change, this orchestrator's behavior changes with them automatically, since it delegates rather than duplicates.

## Outline

Work through these phases in order. Do not skip a phase or jump ahead "for efficiency" -- each one depends on the previous one's actual output (the feature directory, the plan, the task list), not on your memory of similar past features.

### Phase 1: Specify

Invoke `speckit-specify` with `$ARGUMENTS` as its input, exactly as if the user had typed `/speckit-specify $ARGUMENTS`.

- If it comes back with `[NEEDS CLARIFICATION]` questions, **stop and surface them to the user verbatim**. Do not guess answers on their behalf -- this is the one place in the pipeline where the user's intent genuinely branches. Resume Phase 2 only after they answer.
- If it completes clean, note the feature directory it created (e.g. `specs/005-something/`) and continue.

### Phase 2: Plan

Invoke `speckit-plan` (no arguments -- it operates on whatever `.specify/feature.json` currently points to, which Phase 1 just set).

- If a Constitution Check gate fails with unjustified violations, stop and ask the user how they want to resolve it rather than forcing a Complexity Tracking justification yourself.

### Phase 3: Tasks

Invoke `speckit-tasks` (no arguments, same feature-directory pickup as Phase 2).

- If a checklist gate reports unchecked items, follow that skill's own prompt behavior (ask the user whether to proceed) rather than silently overriding it.

### Phase 4: Implement

Invoke `speckit-implement` (no arguments).

- Let it run to completion against the generated `tasks.md`. Do not hand-wave over failing tasks -- if implementation genuinely cannot proceed (a missing external dependency, an unresolved design gap), stop and report that clearly rather than marking tasks done that aren't.

### Phase 5: Ship it

This repo's standing convention (established across `003-github-pr-import`, `004-github-actions-pr-gate`, and the frontend features) is that a feature isn't done until it's merged on `main`. Once Phase 4 completes with working, tested code:

1. If not already on a fresh branch for this work, create one off an up-to-date `main` (`git checkout main && git pull && git checkout -b <name>`), matching the pattern of prior branches in this repo (short, kebab-case, descriptive of the feature -- not necessarily identical to the spec's feature-directory name).
2. Stage and commit the implementation with a message explaining *why*, not just *what* -- same bar as every other commit in this repo's history.
3. Push, open a PR against `main` (via the GitHub API using the credential already available through git's credential helper, the same mechanism used earlier in this session -- see recent PRs #9-#12 for the pattern), and **wait for CI checks to actually complete** before merging. Never merge on a hunch that checks will pass.
4. If checks fail, diagnose and fix on the same branch (push again, re-check) rather than merging anyway or abandoning the branch.
5. Once checks are green, merge, delete the branch, and sync local `main`.

### Phase 6: Report

Summarize what shipped: the spec's core capability in one sentence, the PR number and merge status, and anything the user should know before using it (new env vars, new secrets needed, manual setup steps) -- the same level of detail given at the end of each feature shipped earlier in this session, not a bare "done."

## When to deviate from full automation

Bias toward running straight through per this session's established `/loop`-style working pattern -- but stop and ask the user (not the sub-skills' own internal prompts) when:

- A step's clarifying questions materially change scope, security posture, or user experience (Phase 1's `NEEDS CLARIFICATION` markers are the main case).
- CI fails for a reason that isn't a quick, obvious fix (flag it rather than guessing repeatedly).
- Anything would touch production data, delete a resource, or otherwise cross the "ask first" bar described in this project's own operating norms -- spec-kit features are code changes, but if a generated task ever asks for something destructive, treat that as a stop, not a task to complete.
