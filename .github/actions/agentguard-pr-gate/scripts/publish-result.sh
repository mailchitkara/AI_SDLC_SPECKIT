#!/usr/bin/env bash
# Publishes the Gate Outcome as a GitHub Check Run (data-model.md: Published Result), updating
# any prior run for the same PR/SHA in place (FR-007) rather than creating a duplicate. Falls
# back to a PR comment when the ambient token can't write a Check Run (forked-PR edge case).
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=./lib.sh
source "$SCRIPT_DIR/lib.sh"

: "${GITHUB_TOKEN_INPUT:?github-token input is required}"
: "${GITHUB_REPOSITORY:?}"
: "${GITHUB_EVENT_PATH:?}"
export GH_TOKEN="$GITHUB_TOKEN_INPUT"

OWNER="${GITHUB_REPOSITORY%%/*}"
REPO="${GITHUB_REPOSITORY##*/}"
HEAD_SHA="$(jq -r '.pull_request.head.sha // empty' "$GITHUB_EVENT_PATH")"
PR_NUMBER="$(jq -r '.pull_request.number // .number // empty' "$GITHUB_EVENT_PATH")"
CHECK_NAME="AgentGuard PR Risk Gate"

if [ -z "$HEAD_SHA" ]; then
  echo "::warning::No head SHA available (not a pull_request event) — skipping result publication." >&2
  update_outcome '.mechanism = "none"'
  exit 0
fi

STATUS="$(outcome_field '.status')"
PASS="$(outcome_field '.pass')"

if [ "$STATUS" = "completed" ]; then
  SCORE="$(outcome_field '.score')"
  CLASSIFICATION="$(outcome_field '.classification')"
  RECOMMENDATION="$(outcome_field '.recommendation')"
  FINDINGS_LINE="$(outcome_field '[.finding_summary[] | "\(.severity): \(.count)"] | join(", ")')"
  PARTIAL_COUNT="$(outcome_field '.partially_evaluated_files | length')"
  if [ "$PASS" = "true" ]; then CONCLUSION="success"; else CONCLUSION="failure"; fi
  SUMMARY="**Score:** ${SCORE}/100
**Classification:** ${CLASSIFICATION}
**Recommendation:** ${RECOMMENDATION}
**Findings by severity:** ${FINDINGS_LINE:-none}"
  if [ "$PARTIAL_COUNT" != "0" ]; then
    SUMMARY="${SUMMARY}
**Note:** ${PARTIAL_COUNT} file(s) could not be fully evaluated (binary or oversized content)."
  fi
else
  REASON="$(outcome_field '.unavailable_reason')"
  if [ "$PASS" = "true" ]; then
    CONCLUSION="neutral"
    SUMMARY="AgentGuard could not complete analysis for this PR (reason: ${REASON}). This did not block the merge, per the configured Gate Policy."
  else
    CONCLUSION="failure"
    SUMMARY="AgentGuard could not complete analysis for this PR (reason: ${REASON}). This blocked the merge, per the configured Gate Policy's fail-on-unavailable setting."
  fi
fi

EXISTING_ID="$(gh api "repos/${OWNER}/${REPO}/commits/${HEAD_SHA}/check-runs" \
  --jq ".check_runs[] | select(.name == \"${CHECK_NAME}\") | .id" 2>/dev/null | head -n1 || true)"

PAYLOAD="$(jq -n \
  --arg name "$CHECK_NAME" \
  --arg sha "$HEAD_SHA" \
  --arg conclusion "$CONCLUSION" \
  --arg summary "$SUMMARY" \
  '{name:$name, head_sha:$sha, status:"completed", conclusion:$conclusion, output:{title:$name, summary:$summary}}')"

set +e
if [ -n "$EXISTING_ID" ]; then
  echo "$PAYLOAD" | gh api --method PATCH "repos/${OWNER}/${REPO}/check-runs/${EXISTING_ID}" --input - > /dev/null 2>/tmp/agentguard-publish.err
else
  echo "$PAYLOAD" | gh api --method POST "repos/${OWNER}/${REPO}/check-runs" --input - > /dev/null 2>/tmp/agentguard-publish.err
fi
PUBLISH_EXIT=$?
set -e

if [ "$PUBLISH_EXIT" -ne 0 ]; then
  echo "::warning::Could not publish a Check Run (likely a forked-PR permissions restriction; see $(cat /tmp/agentguard-publish.err 2>/dev/null)) — falling back to a PR comment." >&2
  if [ -n "$PR_NUMBER" ] && gh pr comment "$PR_NUMBER" --repo "${OWNER}/${REPO}" --body "**${CHECK_NAME}**

${SUMMARY}" 2>/tmp/agentguard-comment.err; then
    update_outcome '.mechanism = "pr_comment"'
  else
    echo "::warning::PR comment fallback also failed ($(cat /tmp/agentguard-comment.err 2>/dev/null)); continuing so the workflow's own pass/fail outcome is still reported." >&2
    update_outcome '.mechanism = "none"'
  fi
else
  update_outcome '.mechanism = "check_run"'
fi
