# Phase 0 Research: Overly Permissive Access Control Detection

No `[NEEDS CLARIFICATION]` markers — `/speckit-clarify` found no critical ambiguities for this narrowly-scoped feature. This records the technology/design decisions.

## 1. Detection approach: fixed regex pattern list, mirroring `SecretPatterns`

**Decision**: A fixed, reviewable list of `(Name, Regex, RemediationHint)` entries, structurally identical to the existing `SecretPatterns.All` list, covering 5 patterns across the 3 categories FR-001 requires (wildcard CORS ×3 stack variants, disabled authorization ×1, wildcard allowed-hosts ×1).

**Rationale**: FR-008 explicitly forbids reimplementing a general SAST engine. `SecretDetectedRule`/`SecretPatterns` is the proven precedent in this exact codebase for "deterministic, evidence-producing, pattern-based detection of a security concern" — reusing that shape (rather than inventing a new detection architecture) keeps the new rule reviewable in isolation and consistent with the rest of `AgentGuard.Core`.

**Alternatives considered**: A real parser per target language (Roslyn for C#, an AST library for JS/Python) to detect these constructs semantically rather than textually — rejected as exactly the "general-purpose static analysis engine" FR-008 rules out for this increment; also a much larger, riskier PR than the phase's own stated preference for small increments allows.

## 2. "Newly introduced" semantics: occurrence count, not a value set

**Decision**: For each pattern, count regex matches in `OldContent` and `NewContent` separately; if `newCount > oldCount`, produce one finding for that pattern (evidence includes the count of new occurrences). This differs from `SecretDetectedRule`, which tracks a `HashSet` of distinct matched *values*.

**Rationale**: A secret's matched text is effectively unique per instance (a real credential value), so value-set comparison correctly identifies "this exact secret is new." An access-control pattern's matched text is almost always identical across every occurrence (e.g., the fixed syntax of the ASP.NET Core no-restriction CORS call) regardless of how many times or where it's newly added — a value-set comparison would incorrectly treat a second, genuinely new occurrence as "already seen" merely because the first occurrence's identical text existed before. Count-based comparison correctly handles: a brand-new file (old count 0) — flagged; an untouched file (not in `ChangedFiles` at all, so never evaluated) — correctly never flagged, satisfying FR-002; a modified file where the count is unchanged (e.g., a pattern moved from one line to another without a net increase) — correctly not flagged; a modified file where the count increased — correctly flagged for the delta.

**Alternatives considered**: Value-set comparison (matching `SecretDetectedRule`) — rejected per above, would silently miss genuinely new occurrences of an already-present pattern. Position/line-based diffing (only flag a match at a genuinely new line number) — rejected as unnecessary complexity; count-based comparison already satisfies every acceptance scenario in the spec without needing line-level diff plumbing.

## 3. No API contract change needed

**Decision**: This feature adds zero new fields to any request/response DTO. The new rule's findings flow through the exact `RiskAnalysisResultResponse`/`FindingResponse` shape `005-risk-engine-foundation` already established (rule id, dimension, confidence, kind, severity, evidence, location, remediation — all already generic, already present).

**Rationale**: `005`'s entire purpose was building a foundation new rules could slot into without further contract changes — this is the first real test of that foundation, and it holds: no `contracts/` directory is needed for this feature at all.

**Alternatives considered**: None — this is simply confirming the foundation phase did its job, not a decision with real alternatives.

## 4. Severity: High, not Blocker

**Decision**: `Severity.High` (weight 35), the same severity already used by `ArchitectureViolationRule` and `ApiContractBreakingChangeRule`.

**Rationale**: An overly permissive access-control change is a serious regression worth a human's attention, but — unlike an exposed credential, which is an active, immediate compromise the instant it's merged — it is a *policy* weakening whose actual exploitability depends on context AgentGuard can't fully assess from a diff alone (e.g., a wildcard CORS policy on a genuinely public, read-only API endpoint may be intentional and low-risk). Reserving `Blocker` exclusively for `SecretDetectedRule`'s "credential is now live" certainty keeps the score-100/always-CRITICAL invariant meaningful rather than diluting it.

**Alternatives considered**: `Blocker` — rejected per above; would make every wildcard-CORS PR unconditionally block-merge with no room for legitimate cases, which the spec's own edge cases (e.g., pattern found only in a comment/test fixture — a known, accepted false-positive surface for pattern-based rules) argue against being that strict this early.
