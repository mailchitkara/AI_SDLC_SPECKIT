# Phase 0 Research: Newly Introduced TODO/Stub Detection

No `[NEEDS CLARIFICATION]` markers. This records the technology/design decisions.

## 1. Detection approach: fixed regex pattern list

**Decision**: A fixed, reviewable list of 3 `(Name, Regex, RemediationHint)` entries: TODO/FIXME/HACK comment marker (covers both `//` and `#` comment styles in one pattern), C# `NotImplementedException` stub, Python `NotImplementedError` stub.

**Rationale**: FR-008 forbids a general code-completeness/static-analysis engine. Same shape as every prior count-based Phase 2 rule.

**Alternatives considered**: A per-language AST check for "this function body is literally empty or a stub" — rejected as exactly the general-purpose engine FR-008 rules out; also unnecessary, since the marker/stub-exception surface already catches the common, explicit cases without needing to infer intent from empty bodies (which would have much higher false-positive risk — an empty body can be entirely legitimate, e.g. an interface default).

## 2. "Newly introduced" semantics: occurrence count

**Decision**: Count-based diffing, identical shape to `006`/`007`/`008`.

**Rationale**: Same rationale as the prior three rules — these patterns' matched text is fixed syntax, not a unique value per instance.

## 3. Word-boundary matching to avoid false positives on longer identifiers

**Decision**: The comment-marker pattern requires a word boundary immediately after `TODO`/`FIXME`/`HACK`, so it does not match those letters as a substring of a longer word (e.g. "Hackathon," "TODOClient").

**Rationale**: Directly addresses a real, easy-to-hit false-positive class; a plain substring match would flag unrelated code far too often to be useful.

**Alternatives considered**: None — this is a correctness requirement, not a design trade-off with a real alternative.

## 4. No API contract change needed

**Decision**: Zero new DTO fields — flows through the exact response shape already established.

## 5. Severity: Medium, not High

**Decision**: `Severity.Medium` (weight 20), the same severity already used by `MissingRelatedTestsRule` and `009`'s `GeneratedFileModifiedRule`.

**Rationale**: A TODO or stub is very often a deliberate, reasonable placeholder within a larger incremental change (e.g. a scaffolded method a follow-up PR will fill in) rather than a near-certain mistake — Medium reflects "worth a second look," not "serious problem," matching the same calibration reasoning `009` used.

**Alternatives considered**: High, matching most of Phase 2 so far — rejected as over-weighting a pattern with a meaningfully higher legitimate-use rate than, say, a swallowed exception or a disabled test.

## 6. Self-tripping check applied from the start

**Decision**: Every literal example string in this feature's artifacts is written to avoid a contiguous match against its own patterns, verified against the full staged diff before push.
