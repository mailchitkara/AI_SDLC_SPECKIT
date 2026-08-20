# Phase 0 Research: AgentGuard V1 - PR Risk Analysis

No `NEEDS CLARIFICATION` markers remain in the Technical Context — the spec and constitution together were specific enough to make direct decisions rather than open research questions. This document records those decisions for traceability.

## 1. Change data shape consumed by AgentGuard.Core

**Decision**: The `PullRequestChangeSet` input contains, per changed file: `path`, `changeType` (`ADDED` / `MODIFIED` / `DELETED` / `RENAMED`), and both `oldContent` and `newContent` (nullable depending on change type), plus line-add/line-delete counts. All five rules operate purely on this structure — no rule fetches anything over the network or from a Git host.

**Rationale**: The spec's Assumptions explicitly place "how change data is sourced" out of scope, but every rule (large-change-size, missing-tests, API-breaking-change, architecture-violation, secret-detection) needs before/after content and file classification to be deterministic and testable in isolation. Giving Core a self-contained input keeps it framework-agnostic and keeps analysis a pure function of its input (required for FR-013's determinism guarantee).

**Alternatives considered**: Having Core call out to a Git provider itself — rejected, it would violate "no cloud dependency required for core analysis" (FR-021) and break unit-testability of `AgentGuard.Core.Tests`.

## 2. Large Change Size rule (FR-003)

**Decision**: Sum `linesAdded + linesDeleted` across all changed files and count changed files; finding fires when total lines > 500 OR file count > 20. Both thresholds are read from the input's precomputed counts, not recomputed from diffing content.

**Rationale**: Directly matches FR-003 and its boundary edge case (exactly 500/20 does not trigger — `>`, not `>=`).

**Alternatives considered**: None — thresholds are fixed by the spec, not a research question.

## 3. Missing Related Tests rule (FR-004)

**Decision**: Classify each changed file via path/name pattern into `Source`, `Test`, or `Other`. Default V1 patterns: a file is `Test` if its path contains a `test`/`tests`/`__tests__`/`spec` segment, or its filename matches `*.test.*`, `*.spec.*`, `*Test.cs`, `*Tests.cs`; a file is `Source` if it has a recognized source extension (`.cs`, `.ts`, `.tsx`, `.js`, `.jsx`, `.py`, `.java`, `.go`, etc.) and is not classified `Test`; everything else (docs, config, markdown, JSON, etc.) is `Other`. Finding fires when at least one `Source` file changed and zero `Test` files changed in the same PR. These patterns are supplied as configuration with the above as the default, per the spec's Assumption that exact conventions are a planning-time decision.

**Rationale**: Keeps detection "intentionally simple" (FR-004) — a file classification pass with no semantic understanding of coverage. Configurable defaults let a consuming team override without touching Core.

**Alternatives considered**: Parsing test framework output/coverage reports — rejected as far beyond V1's "intentionally simple" mandate and would reintroduce tooling/CI dependencies.

## 4. API Contract Breaking Change rule (FR-005)

**Decision**: The rule only inspects changed files recognized as API contract files (OpenAPI/Swagger JSON or YAML, matched by filename/extension convention, configurable with a sensible default). For each such file with both `oldContent` and `newContent` (i.e., `MODIFIED`), Core parses old and new as OpenAPI documents and diffs them for exactly the four conditions in FR-005: endpoint removed, HTTP method removed from a remaining endpoint, response property removed, or a previously-optional request property becoming required. No other diff (e.g., new endpoints, added properties, description changes) produces a finding. If a PR changes no recognized contract file, the check simply passes with no finding — V1 does not attempt to infer contract changes from arbitrary source code.

**Rationale**: Bounds detection to a structurally diffable, deterministic artifact instead of attempting semantic analysis of arbitrary handler code, matching the "MUST NOT flag any other kind of API change" boundary in FR-005.

**Alternatives considered**: Static analysis of controller/route source code to infer contract changes — rejected as non-deterministic across languages/frameworks and far outside V1's simplicity mandate.

## 5. Architecture / Dependency Violation rule (FR-006)

**Decision**: `PolicyEngine` loads a static list of forbidden dependency relationships (`{ from: <path/namespace pattern>, to: <path/namespace pattern> }`) from a configuration file bundled with `AgentGuard.Core` (no database, no external service — satisfies FR-021). For each changed file, Core inspects its added import/using/require statements (a simple text-level scan, not a full compiler-level dependency graph) and flags any that match a configured forbidden `{from, to}` pair. V1 ships with an empty default list; a consuming team supplies their own relationships via configuration.

**Rationale**: Matches FR-006's explicit requirement for "a simple allow/deny style list" rather than graph-based analysis, and reuses the constitution's `PolicyEngine` component for exactly this configuration-holding role without building any policy-authoring UI (which remains out of scope per the constitution's Future UI Direction).

**Alternatives considered**: Full static dependency-graph construction (e.g., via Roslyn compilation analysis) — rejected as disproportionate to V1 scope and a source of non-determinism across partial/uncompilable PR diffs.

## 6. Potential Secret Detected rule (FR-007) and evidence masking (FR-010)

**Decision**: Maintain a fixed set of regex patterns for common secret shapes (cloud provider access keys, generic high-entropy `api_key`/`token`/`secret` assignments, private-key PEM headers). On a match, the finding's `evidence` field stores only a masked form (e.g., first 4 and last 4 characters, rest replaced with `*`) — the unmasked match is never placed in any field, log statement, or exception message; masking happens at the point of finding construction, not as a later redaction step, so an unmasked value never exists in an object that could be serialized or logged.

**Rationale**: Directly satisfies FR-007 (detection) and FR-010/SC-007 (masking must hold across evidence, API, UI, and logs) — masking at construction time is the only way to guarantee it can never leak through a code path that forgets to redact.

**Alternatives considered**: Redacting at the API serialization boundary only — rejected, because it would leave an unmasked secret in memory/logs during Core processing, violating FR-010's "never... in evidence... or logs."

## 7. Deterministic scoring, classification, recommendation (FR-012..FR-017)

**Decision**: `RiskEngine` is a pure function `(IReadOnlyList<Finding>) -> RiskAnalysisResult` with no I/O, no randomness, no time dependency, and stable ordering (findings sorted by severity then rule id for reproducible output). Weight table and score/classification/recommendation bands are implemented exactly as specified in FR-012, FR-015, FR-016.

**Rationale**: Purity is what makes FR-013's "identical input MUST always produce the identical score" testable with plain unit tests (`AgentGuard.Core.Tests`), and stable ordering makes UI/API responses byte-for-byte reproducible for the same input, supporting SC-002.

**Alternatives considered**: None — the formula is fully specified by the spec; no design choice remained open.

## 8. Testing stack

**Decision**: xUnit + FluentAssertions for `AgentGuard.Core.Tests` (one test class per rule, plus a `RiskEngine` test class covering the weight/cap/classification/recommendation matrix including the BLOCKER→100→CRITICAL→BLOCK MERGE edge case). `Microsoft.AspNetCore.Mvc.Testing` for `AgentGuard.Api.Tests` (end-to-end through the real endpoint, no mocking of Core). Vitest + React Testing Library for the frontend, covering rendering of the summary, checks, and filterable findings list.

**Rationale**: All are the standard, first-party-supported choices for .NET 8 and Vite/React respectively; no unusual tooling is warranted for a feature this scoped.

**Alternatives considered**: NUnit/MSTest for backend — no material advantage for this project; xUnit is the .NET ecosystem default and already implied by "no unusual dependencies" constraint.
