#!/usr/bin/env bash
# Shared Gate Outcome persistence (data-model.md) between analyze.sh, apply-policy.sh, and
# publish-result.sh, which each run as a separate process (composite action steps don't share
# shell state) — so state has to round-trip through a temp file instead.
set -uo pipefail

AGENTGUARD_OUTCOME_FILE="${RUNNER_TEMP:-/tmp}/agentguard-gate-outcome.json"

write_outcome() {
  echo "$1" > "$AGENTGUARD_OUTCOME_FILE"
}

outcome_field() {
  jq -r "$1" "$AGENTGUARD_OUTCOME_FILE"
}

update_outcome() {
  local tmp
  tmp=$(mktemp)
  jq "$1" "$AGENTGUARD_OUTCOME_FILE" > "$tmp"
  mv "$tmp" "$AGENTGUARD_OUTCOME_FILE"
}
