# Feature Specification: AgentGuard V1 - PR Risk Analysis

**Feature Branch**: `Feature/AgentGuard`

**Created**: 2026-08-20

**Status**: Draft

**Input**: User description: "Define AgentGuard V1. Goal: Provide a simple PR Risk Analysis experience for developers. V1 must support: Analyse one pull request worth of change data; produce a deterministic overall risk score 0-100; produce an overall risk classification; return individual findings (rule id, rule name, severity, explanation, evidence, affected file/location where available, suggested remediation). Initial deterministic rules: large change size; business logic changed without related test changes; potential API contract breaking change; architecture/dependency violation; potential secret detected in changed content. V1 UI: React + TypeScript + Vite, one PR Risk Analysis screen showing repository name, PR number/title, overall risk score, classification, passed/failed checks, findings grouped/filterable by severity, overall recommendation (SAFE TO REVIEW / REVIEW RECOMMENDED / HUMAN REVIEW REQUIRED / BLOCK MERGE). Backend: ASP.NET Core .NET 8 REST API using AgentGuard.Core, no database, no authentication, no LLM, no Docker, no cloud dependency required for core analysis."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Overall PR Risk Summary (Priority: P1)

A developer submits the change data for a single pull request and immediately sees a top-level summary: the repository, PR number and title, an overall risk score (0-100), a risk classification, and a clear recommendation on how to proceed with the review.

**Why this priority**: This is the core value of AgentGuard V1 — a developer must be able to glance at one screen and know whether a PR needs careful human attention, without reading raw logs or diffs first. Without this, there is no product.

**Independent Test**: Can be fully tested by submitting one PR's change data and verifying the screen displays repository name, PR number/title, a numeric score, a classification, and one of the four defined recommendations — independent of whether any individual findings are inspected.

**Acceptance Scenarios**:

1. **Given** a PR with no rule violations, **When** the developer requests analysis, **Then** the system shows a low risk score, a low risk classification, and a "SAFE TO REVIEW" recommendation.
2. **Given** a PR that trips the secret-detection rule, **When** the developer requests analysis, **Then** the system shows an overall risk score of 100, a "CRITICAL" classification, and a "BLOCK MERGE" recommendation regardless of other findings.
3. **Given** the same PR change data submitted twice, **When** analysis is run both times, **Then** the resulting score, classification, and recommendation are identical both times.

---

### User Story 2 - Review Individual Findings by Severity (Priority: P2)

A developer inspects the specific issues that contributed to the risk score, grouped or filtered by severity, so they can understand exactly what was flagged, why, and where, without reading raw logs.

**Why this priority**: The score alone tells a developer *that* something is risky but not *what* or *why*. Findings with evidence and remediation are what make the score explainable and actionable, directly fulfilling the project's core purpose.

**Independent Test**: Can be fully tested by submitting PR change data that trips at least two of the five rules, then verifying each finding displays rule id, rule name, severity, explanation, evidence, affected file/location (when available), and suggested remediation, and that the list can be filtered or grouped by severity.

**Acceptance Scenarios**:

1. **Given** a PR that trips multiple rules, **When** the developer views the findings list, **Then** each finding shows its rule id, rule name, severity, explanation, evidence, remediation, and file/location when one applies.
2. **Given** a PR with findings of different severities, **When** the developer filters by a specific severity, **Then** only findings of that severity are shown.
3. **Given** a finding that applies to the PR as a whole rather than a specific file (e.g., an overall contract change), **When** the developer views that finding, **Then** the affected file/location is omitted rather than showing inaccurate data.

---

### User Story 3 - Review Passed/Failed Checks Summary (Priority: P3)

A developer sees, at a glance, which of the five deterministic checks passed and which failed for the PR, before drilling into individual findings.

**Why this priority**: A pass/fail summary lets a developer quickly confirm overall PR health and decide whether to drill into findings at all. It's valuable but secondary to the score/recommendation (P1) and the detailed findings (P2).

**Independent Test**: Can be fully tested by submitting PR change data and verifying the screen lists all five checks, each marked as passed or failed, matching whether that rule produced any findings.

**Acceptance Scenarios**:

1. **Given** a PR that trips exactly two of the five rules, **When** the developer views the checks summary, **Then** those two checks are marked failed and the remaining three are marked passed.
2. **Given** a PR that trips no rules, **When** the developer views the checks summary, **Then** all five checks are marked passed.

---

### Edge Cases

- What happens when the submitted PR change data contains no file changes? System returns a zero risk score, all checks passed, no findings, a "LOW" classification, and a "SAFE TO REVIEW" recommendation.
- What happens when the submitted change data is malformed or missing required fields (e.g., no repository name)? System returns a clear error indicating what is missing rather than an incomplete or misleading analysis.
- How does the system handle a PR where multiple rules flag the same file? Each rule produces its own independent finding; findings are never merged or deduplicated across rules.
- How does the system handle a very large PR (e.g., thousands of changed files)? Analysis still completes and returns a single deterministic result; the size itself is expected to trigger the large-change-size finding.
- What happens when a PR has one or more BLOCKER-severity findings? Because BLOCKER carries a weight of 100 and the overall score is capped at 100, the score is always exactly 100, the classification is always CRITICAL, and the recommendation is always "BLOCK MERGE" — regardless of any other findings present.
- What happens when a PR has exactly 500 changed lines and exactly 20 changed files? The large-change-size rule does not trigger, since it requires strictly more than 500 changed lines or strictly more than 20 changed files.
- What happens when a PR changes only test files, or changes files that are neither recognized source/business-logic files nor recognized test files (e.g., documentation)? The missing-tests rule does not trigger.
- What happens when no forbidden dependency relationships are configured? The architecture-violation rule never triggers, since V1 relies entirely on configured relationships rather than inferred analysis.
- What happens when a secret is detected? The evidence shown for that finding, in the API response, the UI, and any logs, is masked so the complete secret value is never exposed in any of those surfaces.

## Requirements *(mandatory)*

### Functional Requirements

**Input & rule evaluation**

- **FR-001**: System MUST accept, as input, the change data for exactly one pull request: repository name, PR number, PR title, and the set of changed files/content.
- **FR-002**: System MUST evaluate the input against all five initial deterministic rules defined below, and MUST return a clear error, instead of a partial or misleading analysis, when required input fields are missing or malformed.

**Rule definitions**

- **FR-003 (Large Change Size)**: System MUST produce a finding of severity LOW when a PR's change data has more than 500 total changed lines OR more than 20 changed files.
- **FR-004 (Missing Related Tests)**: System MUST classify each changed file as a recognized source/business-logic file, a recognized test file, or neither, and MUST produce a finding of severity MEDIUM when at least one recognized source/business-logic file changes and no recognized test file changes in the same PR. This detection MUST remain intentionally simple for V1 (file classification only — no semantic analysis of what a test actually covers).
- **FR-005 (API Contract Breaking Change)**: System MUST produce a finding of severity HIGH when changed content shows any of the following, and MUST NOT flag any other kind of API change as breaking in V1: an endpoint removed; an HTTP method removed from an endpoint; a response property removed; or a previously-optional request property changed to required.
- **FR-006 (Architecture/Dependency Violation)**: System MUST produce a finding of severity HIGH when a changed file introduces a dependency that matches a configured forbidden dependency relationship. The V1 mechanism MUST rely solely on configured forbidden relationships (a simple allow/deny style list) rather than inferred or graph-based architectural analysis.
- **FR-007 (Potential Secret Detected)**: System MUST produce a finding of severity BLOCKER when changed content matches a recognized secret pattern.

**Evidence & findings**

- **FR-008**: For every rule that detects an issue, system MUST produce a finding containing: rule id, rule name, severity, explanation, evidence, affected file/location (when determinable), and suggested remediation.
- **FR-009**: When a finding has no determinable single file/location, system MUST omit the location field rather than report an inaccurate one.
- **FR-010**: For secret-detection findings, system MUST mask the detected secret so that the complete secret value is never exposed in the finding's evidence, in any API response, in the UI, or in any log output.
- **FR-011**: System MUST report, for each of the five rules, whether that check passed (no finding produced) or failed (at least one finding produced) for the analyzed PR.

**Deterministic scoring**

- **FR-012**: System MUST assign a numeric weight to each finding based on its severity, using the fixed table: INFO = 0, LOW = 10, MEDIUM = 20, HIGH = 35, BLOCKER = 100.
- **FR-013**: System MUST compute the overall risk score as the sum of the weights of all findings produced for the PR, capped at a maximum of 100. Identical input MUST always produce the identical score.
- **FR-014**: Because a BLOCKER finding carries weight 100 and the overall score is capped at 100, any PR with at least one BLOCKER-severity finding MUST receive an overall score of exactly 100.

**Classification & recommendation**

- **FR-015**: System MUST derive the overall risk classification from the capped overall score using these fixed bands: 0–24 = LOW; 25–49 = MEDIUM; 50–74 = HIGH; 75–100 = CRITICAL.
- **FR-016**: System MUST derive exactly one overall recommendation from the risk classification using this fixed mapping: LOW → SAFE TO REVIEW; MEDIUM → REVIEW RECOMMENDED; HIGH → HUMAN REVIEW REQUIRED; CRITICAL → BLOCK MERGE.
- **FR-017**: Per FR-014 through FR-016, any PR with at least one BLOCKER-severity finding MUST always resolve to CRITICAL classification and a BLOCK MERGE recommendation.

**Execution & platform constraints**

- **FR-018**: System MUST perform analysis synchronously within a single request/response cycle, without persisting results beyond that response.
- **FR-019**: System MUST NOT require user authentication to submit or view a PR risk analysis.
- **FR-020**: System MUST NOT use a large language model or any external AI service to compute rules, scores, or findings; analysis logic MUST be deterministic and rule-based.
- **FR-021**: System MUST be able to run and produce a complete analysis without Docker and without any cloud service dependency.

**UI display**

- **FR-022**: UI MUST display, for the analyzed PR, the repository name, PR number, and PR title.
- **FR-023**: UI MUST display the overall risk score and risk classification.
- **FR-024**: UI MUST display the summary of passed and failed checks for all five rules.
- **FR-025**: UI MUST display all findings and allow the user to filter or group findings by severity.
- **FR-026**: UI MUST display the overall recommendation.
- **FR-027**: UI MUST NOT compute or alter risk scores, classifications, findings, or recommendations; it MUST only display results produced by the backend analysis.

### Key Entities

- **Pull Request Change Set**: The input to an analysis — repository name, PR number, PR title, and the collection of changed files/content for that one PR.
- **Risk Analysis Result**: The output of one analysis run — overall risk score (0–100), risk classification (LOW/MEDIUM/HIGH/CRITICAL), overall recommendation, the passed/failed status of each of the five checks, and the list of findings.
- **Rule**: One of the five fixed deterministic checks — has an id, name, and default severity (which determines its scoring weight), and inspects the change set for the specific condition defined in FR-003 through FR-007.
- **Finding**: An individual issue detected by a rule — rule id, rule name, severity, (masked, for secrets) evidence, explanation, optional affected file/location, and suggested remediation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can determine whether a PR is safe to review, review-recommended, requires human review, or must be blocked within 10 seconds of viewing the analysis screen, without reading raw logs.
- **SC-002**: Identical PR change data submitted for analysis multiple times produces the identical score, classification, and recommendation 100% of the time.
- **SC-003**: 100% of findings shown to the user include a rule name, explanation, evidence, and suggested remediation, so no finding requires external investigation to understand.
- **SC-004**: A developer can narrow the findings list to a single severity level in one interaction (e.g., one click/selection).
- **SC-005**: For a PR of typical size (under 50 changed files), a complete risk analysis is returned and rendered in under 5 seconds.
- **SC-006**: Any PR containing a detected secret is classified with a "BLOCK MERGE" recommendation 100% of the time, independent of all other findings.
- **SC-007**: 0% of secret-detection findings expose the complete secret value in evidence, API responses, UI output, or logs — 100% of such findings show a masked value.

## Assumptions

- Change data is supplied by the caller as structured input (file paths, diff/content, metadata); how that data is originally sourced (e.g., pulled from a Git hosting provider) is outside this specification's scope.
- V1 analyzes exactly one PR per request; batch analysis or repository-wide history is out of scope.
- No user roles or permissions distinctions exist in V1; any caller may submit and view an analysis.
- Analysis results are not persisted between requests; refreshing or re-submitting requires resubmitting the PR change data.
- Users access the UI from a modern desktop or tablet browser; offline use is not required.
- The exact file-name/path conventions used to recognize "source/business-logic files" versus "test files" for the missing-tests rule (FR-004) are configuration decided during planning, not fixed by this specification.
- The source/format of the configured forbidden dependency relationships used by the architecture-violation rule (FR-006) is decided during planning; V1 assumes a simple explicit list rather than full static dependency-graph analysis.
- The exact secret-masking format (e.g., showing only the first/last few characters) is decided during planning; this specification only requires that the complete secret is never exposed (FR-010).
- Future roadmap capabilities (PR risk history, policy configuration, rule management, AI vs. human-generated change analysis, engineering dashboards) are explicitly out of scope for V1.
