# Phase 0 Research: Insecure Configuration Detection

No `[NEEDS CLARIFICATION]` markers. This records the technology/design decisions.

## 1. Detection approach: fixed regex pattern list

**Decision**: A fixed, reviewable list of 4 `(Name, Regex, RemediationHint)` entries: Django debug mode, a .NET TLS-validation-disabling callback, a Node.js HTTPS option that turns off certificate rejection, and a Python `requests` call that turns off TLS verification.

**Rationale**: FR-008 forbids a general configuration/infrastructure-as-code analyzer. Same shape as `006`'s `PermissivePatterns`, which this rule is architecturally closest to (both Security-adjacent, both High severity).

**Alternatives considered**: Parsing actual config file formats (`.env`, `appsettings.json`, `settings.py` as real Python) to determine effective configuration semantically — rejected as exactly the general-purpose analyzer FR-008 rules out; also each of the four chosen patterns is specific, well-known, code-level syntax rather than a config-file value, so a text-pattern approach is a natural fit, not a compromise.

## 2. "Newly introduced" semantics: occurrence count

**Decision**: Count-based diffing, identical shape to `006`/`007`/`008`/`010`.

## 3. No API contract change needed

**Decision**: Zero new DTO fields — flows through the exact response shape already established.

## 4. Severity: High, matching 006

**Decision**: `Severity.High`, the same severity `006-security-risk-rules` uses for access-control loosening.

**Rationale**: Disabling TLS certificate validation or leaving debug mode enabled are both serious, well-documented production risks with a narrow legitimate-use surface (essentially none outside active local debugging) — closer in real-world severity to `006`'s access-control patterns than to `009`/`010`'s Medium-severity, often-legitimate patterns (generated-file edits, TODOs).

**Alternatives considered**: Medium, matching `009`/`010` — rejected; unlike a TODO or a generated-file edit, there is no reasonable "this is a deliberate, temporary, tracked placeholder" case for shipping `DEBUG` mode or disabled certificate validation to a PR that could reach production.

## 5. Scope boundary vs. 006 and SECRET_DETECTED

**Decision**: This rule is explicitly scoped to insecure *settings* — not access-control policy (`006`'s job) and not exposed credential *values* (`SECRET_DETECTED`'s job).

**Rationale**: Keeps each rule's responsibility narrow and non-overlapping, consistent with every prior Phase 2 increment's own scope boundary section.

## 6. Self-tripping check applied from the start

**Decision**: Every literal example string in this feature's artifacts is written to avoid a contiguous match against its own patterns, verified against the full staged diff before push.
