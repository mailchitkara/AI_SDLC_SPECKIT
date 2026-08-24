# Phase 0 Research: Large New File Detection

No `[NEEDS CLARIFICATION]` markers. This records the technology/design decisions.

## 1. Novelty proxy: "new in this PR," not "new in the repository's full history"

**Decision**: This rule treats `ChangeType.Added` as the novelty signal — a file this PR itself introduces.

**Rationale**: The true novelty signal (how long ago a file was actually first created, regardless of which PR analysis is running against) requires querying GitHub's commit-history API for the file, which the plain paste-JSON endpoint (`/api/pr-risk-analysis`) has no repository context to do at all, and which the from-reference endpoint would need a new, rate-limit-aware integration for. "New in this PR" is a real, valid, immediately-available subset of the true signal — every file that's genuinely brand-new to the repository is also `ChangeType.Added` in the PR that introduces it — so this increment loses no correctness, only the additional (and harder) case of "an existing file that happens to be recent."

**Alternatives considered**: Querying git/GitHub history for true file age — deferred to a later increment once that GitHub API integration is worth taking on as its own scoped piece of work (matching this session's established "prefer smaller PRs" discipline).

## 2. Threshold: 200 added lines in a single new file

**Decision**: A newly-added file with `LinesAdded >= 200` qualifies.

**Rationale**: `LargeChangeSizeRule`'s existing 500-line threshold is a *PR-wide* total across every changed file — a lower per-file threshold is warranted here because a single large new file concentrates all of its "no track record" risk in one place, unlike a PR-wide total that might be spread thinly across many small, individually-unremarkable changes. 200 lines is large enough to exclude routine new small files (a new DTO, a new short config, a new small utility) while still catching genuinely substantial new modules.

**Alternatives considered**: Reusing `LargeChangeSizeRule`'s 500-line threshold directly — rejected as too high for a per-file (rather than PR-wide) measure, which would make this rule redundant with `LargeChangeSizeRule` in practice rather than catching a distinct case (e.g. one 250-line new file within an otherwise small, ordinary PR that `LargeChangeSizeRule`'s PR-wide total would never flag).

## 3. Dimension: reuse ChangeManagement, no new dimension

**Decision**: `RiskDimension.ChangeManagement`, the same dimension `LargeChangeSize`, `GeneratedFileModified`, and `TodoStub` already use.

**Rationale**: This is fundamentally about the nature of the change itself (introducing a large amount of unproven code), the same conceptual bucket those three rules already occupy — unlike `013`'s `BusinessCriticality` addition, there's no gap here an existing dimension fails to capture.

**Alternatives considered**: A new dimension (e.g. `Novelty` or `CodeMaturity`) — rejected for this increment as unnecessary; `ChangeManagement` already fits without stretching its meaning.

## 4. Severity: Medium

**Decision**: `Severity.Medium`, matching `009`/`010`'s calibration.

**Rationale**: A large new file isn't inherently broken — it's simply less proven than established code. Medium reflects "worth a second look," the same reasoning already applied to generated-file edits and TODOs.

## 5. No self-tripping-pattern risk

**Decision**: No proactive obscuring needed — this rule compares a numeric line count against a constant, not a text/regex pattern, so there's no way for this feature's own docs or test fixtures to "match" it in the way earlier rules' literal example text could.
