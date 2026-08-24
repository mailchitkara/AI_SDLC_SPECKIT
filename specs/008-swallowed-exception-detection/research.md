# Phase 0 Research: Newly Swallowed Exception Detection

No `[NEEDS CLARIFICATION]` markers. This records the technology/design decisions.

## 1. Detection approach: fixed regex pattern list

**Decision**: A fixed, reviewable list of `(Name, Regex, RemediationHint)` entries covering 3 patterns: an empty-bodied catch block (C#/JS/TS share identical syntax for this, so one pattern covers both), Python bare-except-with-pass, Go ignored-error-check.

**Rationale**: FR-008 forbids a general control-flow/static-analysis engine. This is the same shape used by every Phase 2 rule so far.

**Alternatives considered**: An AST/CFG-based "is this catch block truly a no-op" analysis — rejected as exactly the general-purpose engine FR-008 rules out.

## 2. "Newly introduced" semantics: occurrence count

**Decision**: Count-based diffing, identical shape to `006`/`007` — count regex matches in `OldContent` vs `NewContent`; flag only a net increase.

**Rationale**: Same rationale as `006`/`007` — these patterns' matched text is fixed syntax, not a unique value per instance.

**Alternatives considered**: None new — this is a settled precedent by now.

## 3. No API contract change needed

**Decision**: Zero new DTO fields — flows through the exact response shape already established.

## 4. Severity: High, not Blocker

**Decision**: `Severity.High`, matching every other Phase 2 rule.

**Rationale**: A swallowed error is a serious reliability signal but not an active, immediate compromise like an exposed secret — its real-world impact depends on context (a genuinely inconsequential error swallowed deliberately vs. one masking a real failure).

## 5. Self-tripping check applied from the start

**Decision**: Every literal example string in this feature's artifacts is written to avoid a contiguous match against its own patterns (empty catch block, bare-except-pass, ignored-Go-error-check), verified against the full staged diff before push — per `007`'s research.md §5 precedent, now applied a third time.
