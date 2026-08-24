# Phase 0 Research: Vulnerable Dependency Adapter

No `[NEEDS CLARIFICATION]` markers. This records the technology/design decisions.

## 1. Adapter shape: caller-supplied list, not a tool integration

**Decision**: AgentGuard accepts an optional list of already-identified vulnerable dependencies as request data. It does not shell out to, or embed, any external scanner itself.

**Rationale**: The governance doc explicitly instructs against reimplementing dependency-vulnerability scanning, and AgentGuard's `PullRequestChangeSet` (a diff of changed file content) has no dependency-tree information to scan even if it wanted to — there is nothing in a PR diff that reliably tells you a package's full transitive dependency graph. A caller (typically a CI step that already ran `dotnet list package --vulnerable`, `npm audit`, or `pip-audit`) is the only party that actually has this data.

**Alternatives considered**: AgentGuard directly invoking a scanner against the repository — rejected outright; AgentGuard has no repository checkout, no package manager, and no network access to vulnerability databases in its current architecture, and building that is squarely the "rewriting a dependency-scanning engine" the governance doc rules out.

## 2. Where the new field lives: a sibling parameter to `Analyze`, not a `PullRequestChangeSet` field

**Decision**: `AgentGuardAnalyzer.Analyze` gains a third optional parameter, `IReadOnlyList<VulnerableDependency>? vulnerableDependencies = null` — exactly the same shape as `005`'s `thresholds` parameter, not a new field on the `PullRequestChangeSet` record.

**Rationale**: `PullRequestChangeSet` is constructed positionally in every existing rule's test file (`new PullRequestChangeSet("agentguard-demo", 1, "test", files)`, dozens of call sites across `006`–`011`'s test suites). Adding a field there — even an optional trailing one — is unnecessary risk for zero benefit, since vulnerability data is conceptually orthogonal to "what changed in this diff," exactly like threshold configuration was. Passing it as a sibling parameter, matching `005`'s precedent exactly, touches zero existing call sites.

**Alternatives considered**: A field on `PullRequestChangeSet` — rejected per above. A completely separate endpoint — rejected; `005`'s `Thresholds` precedent already established that request-level, analysis-scoped optional data belongs alongside the existing endpoints, not as a new one.

## 3. Severity mapping: external 4-level scale → AgentGuard's 5-level scale, capped below Blocker

**Decision**: `Low → Low`, `Moderate → Medium`, `High → High`, `Critical → High` (not `Blocker`).

**Rationale**: The four external levels (`low`/`moderate`/`high`/`critical`) match the vocabulary both npm audit and GitHub Security Advisories already use, so callers translating real tool output need no further mapping of their own. Capping `Critical` at `High` preserves `006-security-risk-rules`'s established invariant that `Blocker` (and its guaranteed score-100/CRITICAL/BLOCK_MERGE outcome) is reserved exclusively for `SECRET_DETECTED`'s "a credential is now live" certainty — diluting that by also awarding it to a dependency vulnerability (whose real exploitability, unlike a live leaked credential, still depends on whether the vulnerable code path is actually reachable) would weaken a signal this session has deliberately kept narrow twice already.

**Alternatives considered**: Mapping `Critical → Blocker` — rejected per above. A direct 1:1 five-level external vocabulary — rejected as forcing callers to invent a fifth level with no natural external-tool equivalent, when four cleanly covers the real-world tools this adapter is meant to receive data from.

## 4. Confidence and Kind: Certain / Deterministic, same as every other rule

**Decision**: Every finding from this rule reports `Confidence.Certain` and `FindingKind.Deterministic`.

**Rationale**: The underlying detection was already deterministic — performed by whatever external tool the caller ran — this rule only translates an already-certain result into AgentGuard's shape. It introduces no new uncertainty.

## 5. Validation: reject malformed entries, matching the existing pattern for changed files

**Decision**: A missing package name/version, or an unrecognized severity string, causes a 400 validation error for the whole request — the same behavior an invalid `changeType` on a changed file already has.

**Rationale**: Consistency with the existing validator (`PullRequestChangeSetValidator`/`ChangedFileRequest`'s own per-item validation) rather than inventing a different failure mode (e.g. silently dropping a malformed entry) for this new field.
