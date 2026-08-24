# Phase 0 Research: Hand-Edited Generated File Detection

No `[NEEDS CLARIFICATION]` markers. This records the technology/design decisions.

## 1. Detection approach: fixed extension list + fixed content-marker regex

**Decision**: Two independent, fixed signals: a regex over `file.Path` matching a short list of recognized generated-file extensions, and a regex over `file.NewContent` matching a short list of recognized auto-generated header phrases.

**Rationale**: FR-010 forbids a general build-artifact/codegen-manifest analyzer. A fixed, reviewable list is the same philosophy every prior Phase 2 rule uses, just applied to two different targets (path vs. content) instead of one.

**Alternatives considered**: Consulting `.gitattributes` `linguist-generated` markers or build-tool manifests to determine generated status authoritatively — rejected as out of scope (assumptions.md notes this rule only looks at the PR's own diff, not repository-wide configuration), and as the kind of broader analyzer FR-010 explicitly rules out for this increment.

## 2. Evaluation shape: "content changed at all," not "occurrence count increased"

**Decision**: Unlike `006`/`007`/`008`, this rule does not count regex matches within content. It checks: is this file `ChangeType.Modified`, does old content differ from new content, and does the file match a recognized generated-file signal (by path or by content). If all true, one finding per matched signal.

**Rationale**: The risk signal here isn't "a specific sub-pattern newly appeared" — it's "this generated file's content changed in this PR at all." A count-based diff of, say, the auto-generated marker's own occurrence count would be meaningless (the marker itself doesn't change when someone hand-edits the generated body beneath it).

**Alternatives considered**: Diffing to detect which *lines* changed within the file and only flagging non-trivial changes — rejected as unnecessary complexity; per the spec's edge cases, this rule deliberately does not attempt to judge whether a given edit is trivial or significant, matching the same "surface it, let the reviewer judge" posture every other AgentGuard rule already has.

## 3. Why `ChangeType.Modified` only (not `Added`)

**Decision**: A newly-added file that happens to match a generated-file signal produces no finding — only a genuine edit to a pre-existing generated file does (FR-003).

**Rationale**: Adding a freshly generated file (e.g. checking in codegen output for the first time, or a generator producing a new file) is normal, expected activity — the risk this rule targets is specifically *hand-editing already-generated output*, which requires there to have been a prior version to edit.

**Side effect worth recording**: because this rule only evaluates `Modified` files, it structurally cannot fire on any file this feature's own PR newly adds (every spec/doc/source/test file this increment creates is `ChangeType.Added` against `main`), regardless of that file's content. Only the handful of *existing* files this PR modifies need the standard self-tripping-pattern check.

## 4. Severity: Medium, not High

**Decision**: `Severity.Medium` (weight 20), the same severity already used by `MissingRelatedTestsRule`.

**Rationale**: Unlike the Phase 2 rules shipped so far (all High), a hand-edit to a generated file is sometimes a reasonable, intentional stopgap (e.g. a one-off fix before the generator itself is updated) rather than a near-certain mistake. Medium reflects "worth a second look" rather than "serious problem," while still surfacing every occurrence for human judgment per the spec's edge cases.

**Alternatives considered**: High, matching the rest of Phase 2 so far — rejected as over-weighting a pattern with a meaningfully higher legitimate-use rate than, say, a swallowed exception or a disabled test.

## 5. Self-tripping check

**Decision**: Per §3 above, the newly-added spec/doc/test files are structurally immune (they're `Added`, not `Modified`). The three *existing* files this PR touches (`RuleCatalog.cs`, `AgentGuardAnalyzer.cs`, the endpoint test file) are checked against both signals before push, same standing discipline as `006`–`008`.
