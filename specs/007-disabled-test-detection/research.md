# Phase 0 Research: Newly Disabled Test Detection

No `[NEEDS CLARIFICATION]` markers — `/speckit-clarify` found no critical ambiguities for this narrowly-scoped feature. This records the technology/design decisions.

## 1. Detection approach: fixed regex pattern list, mirroring `PermissivePatterns`

**Decision**: A fixed, reviewable list of `(Name, Regex, RemediationHint)` entries, structurally identical to the existing `PermissivePatterns.All` list, covering 5 patterns across the 4 frameworks FR-001 requires: xUnit's skip parameter (×1), a JS/TS test-runner `.skip()` modifier (×1), a JS/TS `x`-prefixed skip function (×1, kept as a distinct entry from `.skip()` for clearer per-pattern evidence, mirroring how `006` split CORS into 3 stack-specific entries rather than one combined pattern), pytest's skip decorator (×1), and Go's early-skip call (×1).

**Rationale**: FR-008 explicitly forbids reimplementing a general test-coverage or test-quality analyzer. `OverlyPermissiveAccessRule`/`PermissivePatterns` is the immediate precedent in this exact codebase for "deterministic, evidence-producing, pattern-based detection of a testing/security concern" — reusing that shape keeps the new rule reviewable in isolation and consistent with the rest of `AgentGuard.Core`.

**Alternatives considered**: A real parser/AST per target test framework to detect a "test is skipped" state semantically rather than textually — rejected as exactly the "general-purpose test-coverage/quality analyzer" FR-008 rules out for this increment; also a much larger, riskier PR than the phase's own stated preference for small increments allows.

## 2. "Newly introduced" semantics: occurrence count, not a value set

**Decision**: For each pattern, count regex matches in `OldContent` and `NewContent` separately; if `newCount > oldCount`, produce one finding for that pattern (evidence includes the count of new occurrences). Same shape as `OverlyPermissiveAccessRule` — not `SecretDetectedRule`'s value-set tracking.

**Rationale**: A skip marker's matched text is almost always identical across every occurrence (the fixed syntax of the framework's skip call/attribute/decorator), exactly like the overly-permissive-access patterns — a value-set comparison would incorrectly treat a second, genuinely new skip marker as "already seen" merely because the first occurrence's identical text existed before. Count-based comparison correctly handles every acceptance scenario in the spec: a brand-new file (old count 0) is flagged; an untouched file is never evaluated (FR-002); a file where a skip marker is removed (count decreases) is not flagged, so re-enabling a test is never mistakenly penalized; a file where the count increases is flagged for the delta.

**Alternatives considered**: Value-set comparison — rejected per above. Position/line-based diffing — rejected as unnecessary complexity; count-based comparison already satisfies every acceptance scenario without needing line-level diff plumbing.

## 3. No API contract change needed

**Decision**: This feature adds zero new fields to any request/response DTO. The new rule's findings flow through the exact `RiskAnalysisResultResponse`/`FindingResponse` shape `005-risk-engine-foundation` already established.

**Rationale**: This is the second real test of the `005` foundation (after `006`), and it holds again: no `contracts/` directory is needed for this feature at all.

**Alternatives considered**: None — confirming the foundation still holds, not a decision with real alternatives.

## 4. Severity: High, not Blocker

**Decision**: `Severity.High` (weight 35), the same severity already used by `OverlyPermissiveAccessRule`, `ArchitectureViolationRule`, and `ApiContractBreakingChangeRule`.

**Rationale**: A newly-disabled test is a serious signal worth a human's attention — it directly weakens the safety net the test suite exists to provide — but unlike an exposed credential (an active, immediate compromise the instant it's merged), a skip marker's severity genuinely depends on context AgentGuard can't fully assess from a diff alone (e.g., a flaky test skipped with a tracked follow-up issue is a reasonable, low-risk engineering call, vs. a test silently disabled to force a build to pass). Reserving `Blocker` exclusively for `SecretDetectedRule`'s "credential is now live" certainty keeps the score-100/always-CRITICAL invariant meaningful rather than diluting it.

**Alternatives considered**: `Blocker` — rejected per above; would make every newly-skipped test unconditionally block-merge with no room for legitimate cases, which the spec's own edge cases (a documented, justified skip is still flagged, but shouldn't be treated as an automatic hard stop) argue against.

## 5. Self-tripping check is now a standing precaution, not an afterthought

**Decision**: Every literal example string this feature's own artifacts (spec, research, data-model, quickstart, test fixtures) would otherwise contain — e.g. an xUnit skip parameter, a Jest skip modifier call, a pytest skip decorator, a Go test's early-skip call — is written either descriptively (no literal contiguous match) or, where a test genuinely needs the exact runtime string, via compile-time-constant string concatenation, before this feature's PR is ever pushed.

**Rationale**: This exact self-reference failure mode has now happened three times in this session across two different rules (`SECRET_DETECTED` on PRs #10 and #14; `OVERLY_PERMISSIVE_ACCESS_CONTROL` on PR #15, caught proactively before push). It is not a one-off — it is a structural property of shipping any new pattern-matching rule alongside the examples/tests that exercise it, and will recur for every future rule unless checked for deliberately each time.

**Alternatives considered**: Leaving it to CI to catch reactively (the pattern used the first two times) — rejected; the fix is now well-understood and cheap to apply proactively, and doing so avoids wasting a CI cycle and a fixup commit on an entirely predictable failure.
