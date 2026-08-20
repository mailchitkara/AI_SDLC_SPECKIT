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