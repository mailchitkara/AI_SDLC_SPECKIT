# Phase 0 Research: Policy-as-Code Configuration Loading

No `[NEEDS CLARIFICATION]` markers. This records the technology/design decisions.

## 1. One combined JSON file, not two separate ones

**Decision**: A single JSON document with two top-level, independently-optional sections (`forbiddenDependencies`, `businessCriticalPaths`), controlled by one environment variable.

**Rationale**: Both configs share the same lifecycle (loaded once at startup, operator-controlled, currently both unreachable) — a single file and a single environment variable is simpler for an operator to manage than two of each, and matches how a real "policy" is naturally one artifact an operator would want to review and version-control together, not two unrelated files.

**Alternatives considered**: Two separate files/variables (one per config) — rejected as unnecessary indirection for two configs with identical lifecycles and no reason to ever be loaded independently.

## 2. Location: environment variable, not a fixed path

**Decision**: An environment variable (e.g. `AGENTGUARD_POLICY_FILE_PATH`) supplies the file's path; unset means no policy file.

**Rationale**: Matches this deployment's existing environment-driven configuration pattern exactly (`FRONTEND_ORIGIN`, `PORT`) rather than inventing a new configuration mechanism (e.g. an `appsettings.json` section, which would require a code change to even add the section key before an operator could use it — an environment variable requires zero code change to set differently per deployment).

**Alternatives considered**: A fixed, conventional path (e.g. always read `/config/policy.json` if present) — rejected; an environment variable is strictly more flexible (still supports a fixed path, by convention, if an operator chooses one) and matches Render's own deployment model (`render.yaml`'s `envVars` section) more naturally than a baked-in path assumption.

## 3. Failure mode: missing file is normal, malformed file is fatal

**Decision**: A missing file (or unset variable) → empty configs, no error. A present-but-malformed file → startup fails with a clear error.

**Rationale**: These are semantically different situations. "No policy file yet" is the default, expected state for any deployment that hasn't opted in — silently proceeding is correct. "A policy file exists but is broken" is an operator mistake with real consequences (silently proceeding would mean the operator believes their forbidden-dependency/critical-path rules are active when they are not, a false sense of coverage) — failing loudly at startup, before the service ever serves a request, is safer than a silent, ongoing gap that might not be noticed for a long time.

**Alternatives considered**: Always fail-open (log a warning, proceed with empty configs even on a malformed file) — rejected per the false-sense-of-coverage risk above; matches this session's own established preference (from the GitHub Actions gate action) for `fail-on-unavailable` to be an explicit choice, not always fail-open by default, when the consequence of a silent failure is meaningful.

## 4. JSON shape mirrors the existing C# record shapes exactly

**Decision**:

```json
{
  "forbiddenDependencies": [
    { "from": "src/Ui/", "to": "MyApp.Data.*" }
  ],
  "businessCriticalPaths": [
    { "pathPattern": "payments/*", "label": "Payment Processing" }
  ]
}
```

`forbiddenDependencies[].from`/`.to` map directly to `ForbiddenDependency(string From, string To)`; `businessCriticalPaths[].pathPattern`/`.label` map directly to `BusinessCriticalPath(string PathPattern, string Label)`. Both arrays are optional — an absent key is treated the same as an empty array (spec.md edge cases).

**Rationale**: No new pattern-matching semantics, no new field names to remember beyond what the existing records already use — an operator reading `ForbiddenDependency`'s or `BusinessCriticalPath`'s existing doc comments already knows this file's shape.

## 5. Loader lives in AgentGuard.Api, not AgentGuard.Core

**Decision**: `PolicyFileLoader` is an `AgentGuard.Api` type, using `System.Text.Json` directly, producing `AgentGuard.Core.PolicyEngine` types as output.

**Rationale**: JSON deserialization is a wire-format concern, and the constitution already draws this exact line for API request DTOs (`VulnerableDependencyRequest` lives in Api, maps to `AgentGuard.Core.Dependencies.VulnerableDependency`). A local policy file is the same kind of external-representation-to-domain-object translation, just read from disk instead of an HTTP request body.
