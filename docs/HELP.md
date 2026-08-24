# AgentGuard Help Guide

AgentGuard is a deterministic pull-request risk-analysis engine, purpose-built to catch the
specific mistakes AI coding agents make — not a general linter, not a reimplementation of SAST or
dependency-scanning tools, and (today) not itself an AI. Every finding below is a literal,
reproducible pattern match or a direct computation over a PR's own diff; identical input always
produces identical output.

- **Live API**: <https://agentguard-api-ifb3.onrender.com>
- **Live UI**: <https://agentguard-frontend-grar.onrender.com>
- **Source**: this repository, `backend/AgentGuard.Core` (analysis engine) and
  `backend/AgentGuard.Api` (REST layer)

---

## 1. What we've built

AgentGuard analyzes a pull request's changed files and produces one unified result: a 0–100 risk
score, a classification (`LOW`/`MEDIUM`/`HIGH`/`CRITICAL`), a recommendation
(`SAFE_TO_REVIEW`/`REVIEW_RECOMMENDED`/`HUMAN_REVIEW_REQUIRED`/`BLOCK_MERGE`), and a list of
findings — each with a stable rule ID, severity, risk dimension, evidence, and remediation
guidance.

The architecture keeps three layers strictly separate:

```text
React UI  →  ASP.NET Core API  →  AgentGuard.Core
                                      ├── Rules          (14 deterministic checks)
                                      ├── Findings        (the shared result model)
                                      ├── Risk Engine      (score → classification → recommendation)
                                      └── Policy Engine    (operator-configurable overrides)
```

`AgentGuard.Core` never does I/O, never calls an external service, and never depends on anything
non-deterministic — the same PR diff, analyzed twice, always produces byte-for-byte the same
result. That guarantee is a constitutional requirement (`.specify/memory/constitution.md`), not
just a convention.

**Current state**: 14 rules across 9 risk dimensions, plus 2 operator-configurable governance
policies. 159 backend tests, all passing, all live in production.

---

## 2. What the engine checks — and why it matters for agentic code

Every rule below exists because it's a mistake AI coding agents make in a *recognizable,
recurring* way — not a hypothetical. Each one was scoped narrowly on purpose: a fixed, reviewable
set of patterns rather than an open-ended "does this look risky" judgment call, so a human
reviewing a finding can verify it in seconds rather than trusting a black box.

### Baseline checks (present since v1)

| Rule | What it catches | Why it matters for agentic code |
|---|---|---|
| **Large Change Size** | A PR whose total diff crosses a line/file-count threshold | Agents can produce large, sweeping changes in a single pass; size alone correlates with how carefully a human can actually review it |
| **Missing Related Tests** | Source files changed with no corresponding test file touched | An agent instructed to "implement the feature" will often do exactly that and nothing more, unless testing is explicitly part of the ask |
| **API Contract Breaking Change** | A change that breaks an existing API contract | An agent editing one side of an integration doesn't always trace every consumer before reshaping it |
| **Architecture / Dependency Violation** | A newly-added import crossing a configured forbidden boundary | An agent taking the shortest path to "make it compile" can cross architecture boundaries a human would have caught by convention |
| **Potential Secret Detected** | A credential-shaped literal newly added to a file | Agents iterating against a live service sometimes hardcode a real key mid-debugging and forget to remove it before finishing |

### Security & reliability (Phase 2)

| Rule | What it catches | Why it matters for agentic code |
|---|---|---|
| **Overly Permissive Access Control** | Newly-loosened CORS, disabled authorization checks, or a wildcard allowed-hosts setting | Facing a CORS or auth error, an agent's fastest fix is often to open the policy up entirely rather than scope it correctly — a fast fix that also happens to be the wrong one |
| **Newly Disabled Test** | A test newly marked skipped/ignored across several common test frameworks | Facing a failing test, an agent under pressure to "make the build pass" will sometimes disable the test rather than fix the underlying issue |
| **Newly Swallowed Exception** | A newly-added empty error handler that catches a failure and does nothing with it | An agent unblocking itself from an error will sometimes catch it and move on, silently discarding a signal a real user would eventually hit |
| **Hand-Edited Generated File** | An existing edit to a file identified as codegen output (by extension or an in-file header comment) | An agent may not recognize a file is generated and will patch it directly — the fix quietly disappears the next time the file regenerates |
| **Newly Introduced TODO or Stub** | A new incompleteness marker or a not-implemented stub left in newly-added code | An agent can stub out the hard part of a task and report the task as done, leaving acknowledged-incomplete work behind |
| **Insecure Configuration** | Debug mode left enabled, or TLS/certificate validation newly disabled, across several common stacks | Facing a certificate or HTTPS error in a local/dev context, an agent's fastest fix is often to turn validation off entirely — safe locally, dangerous if it ships |
| **Vulnerable Dependency** | A dependency an external scanner already flagged, surfaced through AgentGuard's unified result | Agents adding or upgrading a dependency rarely check it against a vulnerability database unless a scanner is already wired into the workflow — this doesn't replace that scanner, it makes its output impossible to miss |

### Contextual risk intelligence (Phase 4)

| Rule | What it catches | Why it matters for agentic code |
|---|---|---|
| **Business-Critical Path Touched** | A change landing in a path an operator has marked as business-critical (e.g. payments, authentication) | A diff can look small and safe by every other metric while still touching a high-stakes area — this restores context none of the content-based rules can see on their own |
| **Large New File Introduced** | A substantial brand-new file with no prior review or production history | Agents can generate a large amount of code in one new file in a single pass — code with zero track record, a well-documented higher-defect-risk category |

### Enterprise governance (Phase 5)

These aren't rules that produce findings — they're policies that change how findings translate
into an outcome, and they exist specifically so a low score from the rules above can never be the
last word on its own.

| Policy | What it does | Why it matters for agentic code |
|---|---|---|
| **Policy-as-code** | Lets an operator configure architecture boundaries and business-critical paths without forking AgentGuard | Every rule above is only as good as its configuration — this is what makes the two configurable rules actually usable in a real deployment |
| **Mandatory review gate** | Guarantees at least a human review for any PR touching an operator-chosen risk dimension, regardless of computed score | An agent's own implicit self-assessment ("the tests pass, I'm done") is exactly the kind of signal a low aggregate score can accidentally reward — this makes sure a human still looks at what the organization has decided always needs one |

---

## 3. Future plans (V2)

### Phase 3 — Contextual/Semantic Analysis (on hold)

Blocked on an LLM provider and credential decision — nothing to build until that's chosen. Once
unblocked, this phase adds genuinely semantic checks a pattern match can't do: does the change
actually match what the PR description/spec says it does, does it show signs of scope creep beyond
the stated task, does it exhibit patterns associated with model hallucination (invented APIs,
fabricated library behavior).

This is governed by a constitutional amendment already merged
(`.specify/memory/constitution.md`, "Analysis Engine: Deterministic and Contextual Findings"),
written specifically so this phase can never weaken the deterministic guarantee everything above
relies on:

- Every finding from this phase is separately tagged (`Contextual`, never `Deterministic`) and
  always carries a confidence level below certain — it is presented as an inference, never as a
  fact.
- It can never, by itself, force a mandatory block — the strongest outcome a Contextual finding can
  reach on its own is a mandatory human look, the same ceiling the Phase 5 governance policy above
  already uses.
- It can never silently override or contradict a Deterministic finding.
- It must represent unavailable or inconclusive analysis as exactly that, not guess.

### Remaining Phase 4 — deeper repository context

The areas not yet built (file-churn hotspots, true file-age novelty, blast-radius/dependency-impact
analysis, reviewer recommendations) all need a real GitHub commit-history integration beyond a
single PR's diff — a bigger, more deliberate architectural piece than any rule shipped so far, and
scoped as its own future increment rather than folded into what exists today.

### Remaining Phase 5 — governance at scale

Risk delta between analysis runs, a persisted audit trail, reporting/dashboards, and per-repository
or per-organization rule profiles all need persisted state — a database this service has
deliberately avoided everywhere so far, since every rule and policy today is a pure, stateless
computation. Adding storage is a real architectural decision, not an incremental rule addition.

---

## 4. How to use AgentGuard

There are three ways to get an analysis: the web UI, the REST API directly, or the `.NET` library
embedded in your own code. A fourth — the GitHub Actions gate — wraps the API for CI/CD use and is
documented separately in [`docs/github-actions-gate.md`](./github-actions-gate.md).

### 4.1 Web UI

Open <https://agentguard-frontend-grar.onrender.com>. Two tabs:

- **Paste JSON** — paste a change-set payload directly (the same shape the API accepts below).
- **GitHub PR URL** — paste a real GitHub pull request URL; AgentGuard fetches the diff itself.

Results render as a risk score, a pass/fail check list (one row per rule), and a findings list with
severity, dimension, evidence, and remediation guidance for each hit.

### 4.2 REST API directly

Base URL: `https://agentguard-api-ifb3.onrender.com` (or `http://localhost:5080` running locally).

**Endpoint A — analyze a change set you already have:**

```bash
curl -s -X POST https://agentguard-api-ifb3.onrender.com/api/pr-risk-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "repositoryName": "my-org/my-repo",
    "prNumber": 42,
    "prTitle": "Add pricing tier",
    "changedFiles": [
      {
        "path": "src/PricingService.cs",
        "changeType": "MODIFIED",
        "oldContent": "...",
        "newContent": "...",
        "linesAdded": 12,
        "linesDeleted": 3
      }
    ]
  }'
```

`changeType` is one of `ADDED`, `MODIFIED`, `DELETED`, `RENAMED`. `oldContent`/`newContent` are the
full file text before/after (omit `oldContent` for a newly-added file).

**Endpoint B — analyze a real GitHub PR by reference** (AgentGuard fetches the diff for you):

```bash
curl -s -X POST https://agentguard-api-ifb3.onrender.com/api/pr-risk-analysis/from-reference \
  -H "Content-Type: application/json" \
  -d '{"prUrl": "https://github.com/my-org/my-repo/pull/42"}'
```

For a private repository, add a `"credential"` field with a token that has read access. Both
endpoints accept the same optional fields:

- `"thresholds"`: `{ "lowMax": 24, "mediumMax": 49, "highMax": 74 }` — override the default
  score-to-classification bands for this request only.
- `"vulnerableDependencies"`: `[{ "packageName": "...", "version": "...", "severity": "HIGH",
  "advisoryId": "...", "advisoryUrl": "..." }]` — feed in results from a dependency scanner you
  already ran (`npm audit`, `dotnet list package --vulnerable`, `pip-audit`); AgentGuard turns each
  into a finding in the unified result rather than reimplementing the scan itself.

Both endpoints return the same shape:

```json
{
  "score": 45,
  "classification": "MEDIUM",
  "recommendation": "REVIEW_RECOMMENDED",
  "recommendationForcedByOverride": false,
  "recommendationForcedByGovernancePolicy": false,
  "checks": [{ "ruleId": "LARGE_CHANGE_SIZE", "ruleName": "Large Change Size", "passed": true }],
  "findings": [
    {
      "ruleId": "MISSING_RELATED_TESTS",
      "severity": "MEDIUM",
      "dimension": "TESTING",
      "confidence": "CERTAIN",
      "kind": "DETERMINISTIC",
      "evidence": "...",
      "location": "src/PricingService.cs",
      "remediation": "..."
    }
  ]
}
```

### 4.3 Configuring the two governance policies (operator-level)

`ARCHITECTURE_VIOLATION` and `BUSINESS_CRITICAL_PATH_TOUCHED`, and the mandatory-review-gate
policy, are **not** request fields — they're set once, at deployment time, by whoever operates the
AgentGuard service (not by each caller), via one JSON file:

```json
{
  "forbiddenDependencies": [{ "from": "src/Ui/", "to": "MyApp.Data.*" }],
  "businessCriticalPaths": [{ "pathPattern": "payments/*", "label": "Payment Processing" }],
  "mandatoryReviewDimensions": ["BUSINESS_CRITICALITY"]
}
```

Point the service at it with the `AGENTGUARD_POLICY_FILE_PATH` environment variable before startup.
Unset or missing file → every section defaults to empty, identical to not having this feature at
all. A malformed file fails startup loudly on purpose, rather than silently running with a policy
the operator thinks is active but isn't.

### 4.4 From your own code (no HTTP, no deployment)

`AgentGuard.Core` is a plain .NET library with no dependency on the API or the web host — reference
it directly if you're building your own tool in .NET and want the analysis embedded rather than
calling out over HTTP:

```csharp
using AgentGuard.Core;
using AgentGuard.Core.PolicyEngine;

var analyzer = new AgentGuardAnalyzer(
    forbiddenDependencyConfig: ForbiddenDependencyConfig.Empty,
    businessCriticalPathConfig: BusinessCriticalPathConfig.Empty);

var changeSet = new PullRequestChangeSet(
    RepositoryName: "my-org/my-repo",
    PrNumber: 42,
    PrTitle: "Add pricing tier",
    ChangedFiles:
    [
        new ChangedFile(
            Path: "src/PricingService.cs",
            ChangeType: ChangeType.Modified,
            OldContent: "...",
            NewContent: "...",
            LinesAdded: 12,
            LinesDeleted: 3)
    ]);

var result = analyzer.Analyze(changeSet);

Console.WriteLine($"{result.Score} / {result.Classification} / {result.Recommendation}");
foreach (var finding in result.Findings)
{
    Console.WriteLine($"{finding.RuleId}: {finding.Evidence}");
}
```

This is exactly what `AgentGuard.Api`'s two endpoints already do internally — there's no hidden
behavior the API adds on top of the library.

---

## Where to look next

- [`docs/deployment.md`](./deployment.md) — how the two Render services are deployed and how to
  redeploy.
- [`docs/github-actions-gate.md`](./github-actions-gate.md) — wiring AgentGuard into your own
  repository's CI to gate merges automatically.
- [`docs/github-pr-import.md`](./github-pr-import.md) — more detail on the from-reference GitHub
  import flow.
- `specs/` — the full spec-kit history for every increment referenced above (spec, plan, research,
  and task breakdown for each).
- `.specify/memory/constitution.md` — the architectural guarantees every rule and policy in this
  document is bound by.
