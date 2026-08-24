## User Interface & Experience

AgentGuard MUST provide a web-based user interface for developers and
engineering teams to understand pull-request risk and individual findings.

The UI is a first-class AgentGuard interface alongside the Core library,
API and future CLI.

### UI Principles

The UI MUST:
- use React + TypeScript + Vite
- communicate risk clearly and visually
- prioritise findings and evidence over decorative dashboards
- make every risk score explainable
- clearly distinguish BLOCKER, HIGH, MEDIUM, LOW and INFO findings
- allow a developer to understand why a check failed without reading raw logs
- provide actionable remediation guidance
- remain simple and responsive
- meet reasonable accessibility standards

### V1 Dashboard

The initial UI should support a PR Risk Analysis experience containing:

- Repository name
- Pull request identifier and title
- Overall risk score from 0-100
- Overall risk classification
- Summary of passed and failed checks
- Findings grouped or filterable by severity
- Rule name
- Explanation
- Evidence
- Affected file/location where available
- Suggested remediation
- Overall recommendation such as:
  - SAFE TO REVIEW
  - REVIEW RECOMMENDED
  - HUMAN REVIEW REQUIRED
  - BLOCK MERGE

Example conceptual layout:

AgentGuard
------------------------------------------------

Repository: agentguard-demo
PR #42: Add Customer Preferences API

RISK SCORE
72 / 100
HIGH

Checks
------------------------------------------------
PASS    Build                         Passed
PASS    Tests                         48/48
BLOCK   API Contract                  Breaking change
HIGH    Architecture                  Dependency violation
MEDIUM  Test Coverage                 Missing related tests

Findings
------------------------------------------------

API-001 | BLOCKER
Customer.email removed from API response

Evidence:
contracts/customer.yaml

Recommendation:
Restore the property or explicitly version the API.

### Separation of Concerns

The React frontend MUST NOT implement risk-analysis business rules.

The frontend displays results produced by the backend.

The ASP.NET Core API orchestrates requests and exposes AgentGuard.Core
capabilities through REST endpoints.

AgentGuard.Core remains responsible for deterministic analysis,
findings and risk calculation.

The intended architecture is:

React UI
   |
   | REST
   v
ASP.NET Core API
   |
   v
AgentGuard.Core
   |
   +-- Rules
   +-- Findings
   +-- Risk Engine
   +-- Policy Engine

UI-specific requirements MUST NOT leak into AgentGuard.Core.

### UI Contract & Accessibility

The React UI MUST consume AgentGuard through documented REST API contracts.

Frontend components MUST NOT depend on internal AgentGuard.Core implementation details.

Any displayed risk score MUST include the findings that contributed to the score.

The V1 UI SHOULD support:
- keyboard navigation
- accessible labels
- sufficient contrast
- responsive layouts for common desktop and tablet widths

## Analysis Engine: Deterministic and Contextual Findings

AgentGuard.Core's analysis capability is split into two distinct tracks. This
section specifies the second track, satisfying the requirement (below,
Future UI Direction) that capabilities such as spec-to-code compliance and
AI vs human-generated change analysis MUST NOT be implemented until
separately specified.

### Deterministic Track (existing, unchanged)

Every rule shipped through Phase 1 and Phase 2 of the risk-analysis
expansion — pattern-matching rules, count-based diffing rules, and the
externally-supplied-findings adapter — belongs to this track.

Deterministic findings:
- MUST report `FindingKind.Deterministic` and `Confidence.Certain`
- MUST be reproducible: identical input MUST always produce an identical
  finding
- MAY set `MandatoryOverride`, and MAY alone drive the risk score past the
  CRITICAL threshold to a `BLOCK_MERGE` recommendation

Nothing in this section changes any deterministic rule's existing behavior,
scoring, or guarantees.

### Contextual Track (new, governs Phase 3 and beyond)

A Contextual finding is one produced by inference — semantic, spec-alignment,
or model-based reasoning about a change — rather than a fixed, literal
pattern match. Phase 3 (Contextual/Semantic Risk Analysis) and any later
phase introducing inference-based findings MUST comply with every
constraint below. These constraints are constitutional, not merely a
design preference for Phase 3 — a future phase MAY NOT loosen them without
a further, explicit constitution amendment.

Contextual findings MUST:
- report `FindingKind.Contextual`
- report a `Confidence` other than `Certain` (`High`, `Medium`, or `Low`) —
  a Contextual finding claiming `Certain` confidence misrepresents an
  inference as a fact and MUST be rejected
- be structurally and visually distinguishable from Deterministic findings
  everywhere they are surfaced (API response, UI) — a reviewer MUST always
  be able to tell, without reading the explanation text, whether a finding
  is a fact or an inference
- carry reasoning distinct from evidence: a plain statement of *why* the
  model reached this conclusion, separate from the located evidence itself

Contextual findings MUST NOT:
- set `MandatoryOverride` under any circumstance
- by themselves, or in combination with other Contextual findings only,
  produce a `BLOCK_MERGE` recommendation — only Deterministic findings
  (via score threshold or `MandatoryOverride`) may reach `BLOCK_MERGE`. A
  PR whose only findings are Contextual MUST cap at
  `HUMAN_REVIEW_REQUIRED` regardless of computed score
- silently override or contradict a Deterministic finding — low-confidence
  contextual analysis MUST be presented as uncertainty alongside
  deterministic evidence, never as a replacement for it (this restates,
  for the constitution's own authority, the same principle the Phase 3
  governance guidance already establishes)
- be fabricated when the underlying analysis is unavailable or
  inconclusive — an unavailable determination MUST be represented as
  such, not guessed

Any Phase 3 (or later) implementation plan's Constitution Check gate MUST
verify compliance with every MUST/MUST NOT above before proceeding past
Phase 0.

### Future UI Direction

The architecture may support future capabilities such as:

- repository overview
- PR risk history
- engineering risk trends
- policy configuration
- rule management
- spec-to-code compliance
- AI vs human-generated change analysis
- engineering leadership dashboards

These capabilities MUST NOT be implemented until separately specified.