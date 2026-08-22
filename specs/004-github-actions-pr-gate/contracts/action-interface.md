# Contract: `agentguard-pr-gate` Action Interface

This is the public interface a workflow author codes against — `.github/actions/agentguard-pr-gate/action.yml`. Implementation detail behind these inputs/outputs is free to change without breaking consumers.

## Inputs

| Input | Required | Default | Maps to Data Model |
|---|---|---|---|
| `api-url` | yes | — | `Gate Policy.api-url` |
| `github-token` | no | `${{ github.token }}` | `Gate Policy.github-token` |
| `block-on` | no | `CRITICAL` | `Gate Policy.block-on` (comma-separated list, e.g. `HIGH,CRITICAL`) |
| `fail-on-unavailable` | no | `false` | `Gate Policy.fail-on-unavailable` |
| `timeout-seconds` | no | `60` | `Gate Policy.timeout-seconds` |

## Outputs

| Output | Present when | Description |
|---|---|---|
| `status` | always | `completed` or `unavailable` — mirrors `Gate Outcome.status`. |
| `score` | `status = completed` | 0–100. |
| `classification` | `status = completed` | `LOW \| MEDIUM \| HIGH \| CRITICAL`. |
| `recommendation` | `status = completed` | `SAFE_TO_REVIEW \| REVIEW_RECOMMENDED \| HUMAN_REVIEW_REQUIRED \| BLOCK_MERGE`. |
| `pass` | always | `true`/`false` — the Gate Policy decision (`Gate Outcome.pass`), so a downstream step can branch on it independent of the step's own exit code. |

## Step outcome contract

- The composite action's final step exits non-zero (failing the workflow step) **iff** `Gate Outcome.pass = false`. A caller relying on step failure to gate a required check needs no extra logic; a caller wanting to branch instead can read the `pass` output and ignore step exit code by adding `continue-on-error: true` to the step.
- The action always attempts to publish a Check Run (or fallback comment) before resolving its own exit code — a failed publish does not suppress the pass/fail decision, but is recorded in the step's own log output (not this contract's concern, since FR-006/US3 concern what appears *on the PR*, not the workflow log).

## Example usage

```yaml
- uses: ./.github/actions/agentguard-pr-gate
  with:
    api-url: https://agentguard-api-ifb3.onrender.com
    block-on: HIGH,CRITICAL
```
