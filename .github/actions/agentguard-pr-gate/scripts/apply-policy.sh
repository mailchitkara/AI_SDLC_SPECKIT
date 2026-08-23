#!/usr/bin/env bash
# Derives pass/fail from the Gate Policy (data-model.md) and the Gate Outcome analyze.sh wrote.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./lib.sh
source "$SCRIPT_DIR/lib.sh"

BLOCK_ON="${BLOCK_ON:-CRITICAL}"
FAIL_ON_UNAVAILABLE="${FAIL_ON_UNAVAILABLE:-false}"

STATUS="$(outcome_field '.status')"
PASS=true

if [ "$STATUS" = "completed" ]; then
  CLASSIFICATION="$(outcome_field '.classification')"
  IFS=',' read -ra LEVELS <<< "$BLOCK_ON"
  for raw_level in "${LEVELS[@]}"; do
    level="$(echo "$raw_level" | xargs)"
    if [ "$level" = "$CLASSIFICATION" ]; then
      PASS=false
    fi
  done
else
  if [ "$FAIL_ON_UNAVAILABLE" = "true" ]; then
    PASS=false
  fi
fi

update_outcome ".pass = $PASS"
echo "pass=$PASS" >> "$GITHUB_OUTPUT"
