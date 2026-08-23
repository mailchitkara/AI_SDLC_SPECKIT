#!/usr/bin/env bash
# Resolves the triggering PR from ambient Actions context (no URL/manual input — US1) and calls
# 003-github-pr-import's analyze-by-reference endpoint. See contracts/analyze-by-reference.md for
# the response-status mapping this implements.
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./lib.sh
source "$SCRIPT_DIR/lib.sh"

: "${API_URL:?api-url input is required}"
: "${GITHUB_REPOSITORY:?}"
: "${GITHUB_EVENT_PATH:?}"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-60}"
CREDENTIAL="${GITHUB_TOKEN_INPUT:-}"

OWNER="${GITHUB_REPOSITORY%%/*}"
REPO="${GITHUB_REPOSITORY##*/}"
PR_NUMBER="$(jq -r '.pull_request.number // .number // empty' "$GITHUB_EVENT_PATH")"

if [ -z "$PR_NUMBER" ]; then
  echo "::error::Could not resolve a pull request number from the triggering event (${GITHUB_EVENT_PATH}). This action must run on a pull_request-related event." >&2
  write_outcome '{"status":"unavailable","unavailable_reason":"unreachable"}'
  exit 0
fi

PR_URL="https://github.com/${OWNER}/${REPO}/pull/${PR_NUMBER}"
REQUEST_BODY="$(jq -n --arg prUrl "$PR_URL" --arg credential "$CREDENTIAL" \
  'if $credential == "" then {prUrl:$prUrl} else {prUrl:$prUrl, credential:$credential} end')"

HTTP_CODE_FILE="$(mktemp)"
RESPONSE_BODY_FILE="$(mktemp)"

set +e
curl -sS --max-time "$TIMEOUT_SECONDS" \
  -o "$RESPONSE_BODY_FILE" -w '%{http_code}' \
  -X POST "${API_URL%/}/api/pr-risk-analysis/from-reference" \
  -H 'Content-Type: application/json' \
  -d "$REQUEST_BODY" > "$HTTP_CODE_FILE"
CURL_EXIT=$?
set -e

if [ "$CURL_EXIT" -ne 0 ]; then
  echo "::warning::AgentGuard analysis call failed or timed out (curl exit $CURL_EXIT)." >&2
  write_outcome '{"status":"unavailable","unavailable_reason":"timed_out"}'
  exit 0
fi

STATUS_CODE="$(cat "$HTTP_CODE_FILE")"

case "$STATUS_CODE" in
  200)
    write_outcome "$(jq -c '{
        status: "completed",
        score: .score,
        classification: .classification,
        recommendation: .recommendation,
        finding_summary: ([.findings[].severity] | group_by(.) | map({severity: .[0], count: length})),
        partially_evaluated_files: (.partiallyEvaluatedFiles // [])
      }' "$RESPONSE_BODY_FILE")"
    ;;
  429)
    echo "::warning::AgentGuard analysis unavailable: GitHub is rate-limiting the request." >&2
    write_outcome '{"status":"unavailable","unavailable_reason":"rate_limited"}'
    ;;
  404 | 400)
    echo "::warning::AgentGuard analysis unavailable (HTTP $STATUS_CODE) — this should not normally occur, since this action constructs the PR reference itself from valid workflow context." >&2
    write_outcome '{"status":"unavailable","unavailable_reason":"unreachable"}'
    ;;
  *)
    echo "::warning::AgentGuard analysis unavailable (unexpected HTTP $STATUS_CODE)." >&2
    write_outcome '{"status":"unavailable","unavailable_reason":"unreachable"}'
    ;;
esac
