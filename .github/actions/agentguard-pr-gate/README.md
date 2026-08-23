# AgentGuard PR Risk Gate

Analyzes the pull request a workflow is running on via [AgentGuard](https://github.com/mailchitkara/AI_SDLC_SPECKIT), gates it on a configurable risk policy, and publishes the result as a Check Run on the PR.

```yaml
- uses: actions/checkout@v4
- uses: ./.github/actions/agentguard-pr-gate
  with:
    api-url: https://agentguard-api-ifb3.onrender.com
```

Full usage guide, inputs/outputs reference, and branch-protection setup: [docs/github-actions-gate.md](../../../docs/github-actions-gate.md).
